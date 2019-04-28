using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Net;
using System.Threading.Tasks;
using System.Diagnostics;
using BakaTsuki.LNReader_Android.ImageRetriever.Database;
using BakaTsuki.LNReader_Android.ImageRetriever.Types;

namespace BakaTsuki.LNReader_Android.ImageRetriever.Core
{
    /// <summary>
    /// Contains the main actions that the CLI app has.
    /// </summary>
    public class App
    {
        #region Properties

        /// <summary>
        /// SQLite 3 db file or System.Data.SQLite file
        /// </summary>
        public string DbFile { get; set; } = "Backup_pages.db";

        /// <summary>
        /// List of images to download
        /// </summary>
        public List<DbImage> DbImages { get; set; } = new List<DbImage>();

        #endregion

        /// <summary>
        /// Displays the initial messages to be displayed to the user.
        /// </summary>
        public void Init()
        {
            Console.Title = "BakaTsuki.LNReader-Android.ImageRetriever by HFrederick";
            Console.WriteLine("BakaTsuki.LNReader-Android.ImageRetriever by HFrederick");
            Console.WriteLine();
            Console.WriteLine("* * * * * * * * * * * *");
            Console.WriteLine();
            Console.WriteLine("A CLI application used to restore images " +
                "from the baka-tsuki android reader. Uploaded on github for others.\n\n" +
                "Use this if you've accidentally lost/deleted " +
                "your downloaded images from your device.");
            Console.WriteLine();
            Console.Write("Instructions:\n" +
                "1. Export the database from the LNReader app in your device " +
                "(Settings -> Storage -> Create Novel Database Backup).\n" +

                "2. Copy the Backup_pages.db file into the same folder as this " +
                $"executable (");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write($"{Environment.CurrentDirectory}).\n");
            Console.ForegroundColor = ConsoleColor.Gray;

            Console.Write("3. Run the application and input the values as needed.\n" +

                "4. Wait for the app to finish downloading your images. " +
                "The app will create a beep sound after finishing.\n");

            Console.Write("5. The downloaded files will be saved inside the images folder at ");

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write($"{Environment.CurrentDirectory}\\images\\ ");
            Console.ForegroundColor = ConsoleColor.Gray;

            Console.Write("then copy the projects folder " +
                "into the Image Save Location specified in " +
                "your device's LNReader android app.\n");

            Console.Write("Optionally: You can also relink the image's file path in the database, " +
                "so you can have all the images stored in one location.");

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("WARNING: Depending on the amount of images in " +
                "the database file, it may take a while to download the images.");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine();
            Console.WriteLine("* * * * * * * * * * * *");
        }

        /// <summary>
        /// Set up of db file and checking workspace files.
        /// </summary>
        public async Task SetUp()
        {
            string dbFile;
            do
            {
                // ask for database filename and see if it's valid
                Console.Write("Enter the name of the exported database file " +
                "(it must be placed on the same place as this .exe file, " +
                "leave as empty if the exported db filename is Backup_pages.db): ");
                
                dbFile = Console.ReadLine();
                DbFile = string.IsNullOrWhiteSpace(dbFile) ? DbFile : dbFile;

                // check if database schema contains the images table and expected columns
                // see DbLayout.cs for how it layouts a database schema
                // see Database.cs line 39 for expected column names and data-types
                Console.Write($"Checking if {DbFile} is a valid database... ");
                if (!await new Database.Database(DbFile).IsDatabaseValid())
                    continue;
                
            } while (!File.Exists(DbFile) || !DbFile.EndsWith(".db"));

            Console.WriteLine("Ok.");
            var database = new Database.Database(DbFile);
            // setup workspace files. Adds an 'images' folder, and a 'log.txt' text file
            // Grabs all images from the db for preparation with other methods
            Console.Write("Checking workspace files... ");
            Directory.CreateDirectory("images");
            
            using (var sw = File.AppendText("log.txt"))
            {
                await sw.WriteLineAsync($"Log started at: {DateTime.Now.ToString()}" + sw.NewLine +
                    "----------------------------------------------------------------------" + sw.NewLine);
            }
            
            DbImages = await database.GetDbImagesFromDb();
            Console.WriteLine($"Workspace files has been created. You have {DbImages.Count} images.\n");
        }

        /// <summary>
        /// Downloads all images from the DbFile property into the images folder.<para />
        /// This method also logs download errors into a log.txt file
        /// </summary>
        /// <returns></returns>
        public async Task DownloadImages()
        {
            // prepare webclient for downloading images
            WebClient webClient = new WebClient();
            webClient.Headers["User-Agent"] = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) " +
                "Chrome/73.0.3683.103 Safari/537.36";
            webClient.CachePolicy = new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.BypassCache);

            // Start diagnostics and initialize image counters for failed and successful image downloads
            Console.WriteLine($"Downloading all {DbImages.Count} images...");
            Stopwatch watch = new Stopwatch();
            int downloadedImages = 0, failedImages = 0, alreadyDownloadedImages = 0;
            
            // loop through all the image entries in the database
            foreach (var image in DbImages)
            {
                // start timer for first image
                watch.Start();
                // if the file already exists, log it and skip the current image item
                if (File.Exists(@$"images{image.Name}"))
                {
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.Write(@$"images{image.Name} ");
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Console.Write("is already downloaded. Check log.txt");
                    Console.WriteLine();

                    using (StreamWriter writer = File.AppendText("log.txt"))
                    {
                        await writer.WriteLineAsync($"This image was already downloaded. Please check if " +
                            $"the image is corrupted and if it is, delete it and run the app again{writer.NewLine}" +
                            $"Location: {Path.GetDirectoryName(Path.GetFullPath($"images{image.Name}"))}{writer.NewLine}" +
                            $"File: {Path.GetFileName(image.Name)}{writer.NewLine}" +
                            $"Absolute location of file: {Path.GetFullPath($"images{image.Name}")}{writer.NewLine}" +
                            $"Additional data about this image: {writer.NewLine}" +
                            $"Image.ID: {image.ID}{writer.NewLine}" +
                            $"Image.Name: {image.Name}{writer.NewLine}" +
                            $"Image.FilePath: {image.FilePath}{writer.NewLine}" +
                            $"Image.Url: {image.Url}{writer.NewLine}" +
                            $"Image.Referer: {image.Referer}{writer.NewLine}" +
                            $"Image.LastUpdate: {image.LastUpdate}{writer.NewLine}" +
                            $"Image.LastCheck: {image.LastCheck}{writer.NewLine}" +
                            $"Image.IsBigImage: {image.IsBigImage}{writer.NewLine}" +
                            $"Image.Parent: {image.Parent}{writer.NewLine + writer.NewLine}" +
                            $"----------------------------------------------------------------------{writer.NewLine + writer.NewLine}");
                    }

                    alreadyDownloadedImages++;
                    watch.Reset();
                    continue;
                }

                // display status message, create the directory for the image
                Console.Write("Fetching ");
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.Write($"{image.Url}");
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.Write("... ");
                Directory.CreateDirectory(Path.GetDirectoryName($"images{image.Name}"));

                // download the image, while watching out for errors
                try
                {
                    await webClient.DownloadFileTaskAsync(image.Url, @$"images{image.Name}");
                }
                // alert the user about the exception and log the errors
                // if WebException was thrown, it still creates a 0 byte image file (empty/broken image) in the disk
                // the catch blocks should delete the file
                catch (WebException WebEx)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write("WebException occured. The image requested wasn't found, " +
                        "or the internet has been disconnected. See the log file at ");
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.Write(@$"{Environment.CurrentDirectory}log.txt ");
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write("for details.");
                    Console.ForegroundColor = ConsoleColor.Gray;

                    using (StreamWriter writer = File.AppendText("log.txt"))
                    {
                        await writer.WriteLineAsync($"WebException occured during download of images.{writer.NewLine}" +
                                    $"Message: {WebEx.Message}{writer.NewLine}" +
                                    $"Stack Trace: {WebEx.StackTrace}{writer.NewLine}" +
                                    $"Response: {WebEx.Response}{writer.NewLine}" +
                                    $"Status: {WebEx.Status}{writer.NewLine}" +
                                    $"Source: {WebEx.Source}{writer.NewLine}" +
                                    $"Target Site: {WebEx.TargetSite}{writer.NewLine}" +
                                    $"Inner Exception: {WebEx.InnerException}{writer.NewLine + writer.NewLine}" +
                                    $"Additional data about this image: {writer.NewLine}" +
                                    $"Image.ID: {image.ID}{writer.NewLine}" +
                                    $"Image.Name: {image.Name}{writer.NewLine}" +
                                    $"Image.FilePath: {image.FilePath}{writer.NewLine}" +
                                    $"Image.Url: {image.Url}{writer.NewLine}" +
                                    $"Image.Referer: {image.Referer}{writer.NewLine}" +
                                    $"Image.LastUpdate: {image.LastUpdate}{writer.NewLine}" +
                                    $"Image.LastCheck: {image.LastCheck}{writer.NewLine}" +
                                    $"Image.IsBigImage: {image.IsBigImage}{writer.NewLine}" +
                                    $"Image.Parent: {image.Parent}{writer.NewLine + writer.NewLine}" +
                                    $"----------------------------------------------------------------------{writer.NewLine + writer.NewLine}");
                    }

                    if (File.Exists($"images{image.Name}"))
                        File.Delete($"images{image.Name}");

                    failedImages++;
                    watch.Reset();
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write("An exception occured. Something else unexpected happened. " +
                        "See the log file at ");
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.Write(@$"{Environment.CurrentDirectory}log.txt ");
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write("for details.");
                    Console.ForegroundColor = ConsoleColor.Gray;

                    using (StreamWriter writer = File.AppendText("log.txt"))
                    {
                        await writer.WriteLineAsync($"Exception occured during download of images.{writer.NewLine}" +
                                    $"Message: {ex.Message}{writer.NewLine}" +
                                    $"Stack Trace: {ex.StackTrace}{writer.NewLine}" +
                                    $"Source: {ex.Source}{writer.NewLine}" +
                                    $"Target Site: {ex.TargetSite}{writer.NewLine}" +
                                    $"Inner Exception: {ex.InnerException}{writer.NewLine + writer.NewLine}" +
                                    $"Additional data about this image: {writer.NewLine}" +
                                    $"Image.ID: {image.ID}{writer.NewLine}" +
                                    $"Image.Name: {image.Name}{writer.NewLine}" +
                                    $"Image.FilePath: {image.FilePath}{writer.NewLine}" +
                                    $"Image.Url: {image.Url}{writer.NewLine}" +
                                    $"Image.Referer: {image.Referer}{writer.NewLine}" +
                                    $"Image.LastUpdate: {image.LastUpdate}{writer.NewLine}" +
                                    $"Image.LastCheck: {image.LastCheck}{writer.NewLine}" +
                                    $"Image.IsBigImage: {image.IsBigImage}{writer.NewLine}" +
                                    $"Image.Parent: {image.Parent}{writer.NewLine + writer.NewLine}" +
                                    $"----------------------------------------------------------------------{writer.NewLine + writer.NewLine}");
                    }

                    if (File.Exists($"images{image.Name}"))
                        File.Delete($"images{image.Name}");

                    failedImages++;
                    watch.Reset();
                }

                // stop the timer and display that the image has finished downloading along with the elapsed time
                watch.Stop();
                Console.WriteLine("Done. " + Convert.ToInt32(watch.Elapsed.TotalMilliseconds) + " ms");
                watch.Reset();
                downloadedImages++;
            }

            // Finish up by displaying image download stats and playing a beep sound.
            Console.WriteLine();
            Console.WriteLine($"Total images in database: {DbImages.Count}\n" +
                $"Downloaded images: {downloadedImages}\n" +
                $"Images that failed to download: {failedImages}\n" +
                $"Images already on disk: {alreadyDownloadedImages}\n" +
                $"Total images stored in disk: {downloadedImages + alreadyDownloadedImages}\n" +
                $"Check log.txt for detailed errors. " +
                $"For images that were already on disk, double-check to see if they aren't corrupted " +
                $"(the log.txt file includes the location of said image), if said image was already download and has " +
                $"errors, delete the image and run this app again\n");
            Console.Beep();
        }

        /// <summary>
        /// Relinks image file paths in the database
        /// </summary>
        /// <returns></returns>
        public async Task RelinkImagePaths()
        {
            // ask for relinking image paths
            string answer;
            do
            {
                Console.Write("Relink the image paths in the database to be under the same folder location (Yes/No)? ");
                answer = Console.ReadLine();
            } while (!(answer.Equals("Yes", StringComparison.OrdinalIgnoreCase) || answer.Equals("No", StringComparison.OrdinalIgnoreCase)));

            if (answer.Equals("No", StringComparison.OrdinalIgnoreCase))
                return;

            // Prepare DbImages field
            var database = new Database.Database(DbFile);
            if (DbImages.Count == 0 || DbImages is null)
                DbImages = await database.GetDbImagesFromDb();

            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("WARNING: ");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write("This procedure would change every image's filepath where the android app reads the image from.\n\n" +
                "For example, if the image's previous filepath was located at ");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("/storage/emulated/0/Android/data/com.erakk.lnreader/files/images/..\n");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write("And you specify it to be at ");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("/storage/sdcard/LNReader/images/..\n");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine("Then you must put the downloaded content from the images folder at that new specified location. \n" +
                "Because this procedure would tell the app to locate the images at the specified new filepath onwards. \n" +
                "It will relocate all the image's filepath into that new location via the db, to be precise.\n");

            Console.WriteLine("This app will take care of appending the image's relative path to your new specified path. \n\nE.g.");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("/storage/emulated/0/Android/data/com.erakk.lnreader/files/images");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write(" + ");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("/project/images/b/bc/This_title_is_too_long%21_v1_Cover.png");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write(" = ");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("/storage/emulated/0/Android/data/com.erakk.lnreader/files/images/project/images/b/bc/This_title_is_too_long%21_v1_Cover.png\n");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine("When inputting the new location, you only need to type in the path to place the " +
                "/project/.. folder at in your phone. The path must start from the root of your phone");
            Console.WriteLine("Examples of expected filepath: ");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("/storage/emulated/0/Android/data/com.erakk.lnreader/files/images");
            Console.WriteLine("/storage/emulated/0/LNReaderFiles/images\n");
            Console.ForegroundColor = ConsoleColor.Gray;

            // TODO: Better validation for filepath
            // Goal for file path validation:
            // It has to be a filepath that works for the android file-system
            // starts with a '/' to indicate root of android device
            // ends with a non-'/' char, because the image addresses that will be attached after
            // the save file path already has a '/' at their beginning
            string newLocation;
            do
            {
                Console.Write("What would be the new location for all the images? ");
                newLocation = Console.ReadLine();
            } while (!(newLocation.StartsWith("/") && !newLocation.EndsWith("/")));

            // Start diagnostics and update the db with the new location
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            int x = await database.UpdateAllImageFilePath(newLocation, DbImages);
            stopwatch.Stop();
            Console.WriteLine($"Changed {x} file paths");
            Console.WriteLine($"{stopwatch.ElapsedMilliseconds} ms has elapsed for re-linking image file paths.");
        }
    }
}
