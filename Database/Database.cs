using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading.Tasks;
using System.IO;
using System.Data.SQLite;

using BakaTsuki.LNReader_Android.ImageRetriever.Types;

namespace BakaTsuki.LNReader_Android.ImageRetriever.Database
{
    /// <summary>
    /// Class containing implementations for database operations.<para />
    /// The methods will operate on the database passed in the ctor.
    /// </summary>
    public class Database
    {
        private string ConnectionString { get; set; }
        
        /// <summary>
        /// Initialize the database class
        /// </summary>
        /// <param name="dbLocation">
        /// Location of the database file.<para />
        /// Can be relative path, or absolute.<para /> 
        /// Throws an exception if the *.db file doesn't exist or isn't a database from the android app.<para />
        /// </param>
        /// <exception cref="ArgumentNullException" />
        public Database(string dbLocation)
        {
            if (dbLocation is null)
                throw new ArgumentNullException();
            ConnectionString = $@"Data Source={dbLocation};Version=3;";
        }

        /// <summary>
        /// A read-only database layout containing the table schema for the images table.
        /// </summary>
        private readonly List<DbLayout> ImagesDbLayout = new List<DbLayout>()
        {
            new DbLayout() { ColumnName = "_id", DataType="INTEGER" },
            new DbLayout() { ColumnName = "name", DataType="text" },
            new DbLayout() { ColumnName = "filepath", DataType="text" },
            new DbLayout() { ColumnName = "url", DataType="text" },
            new DbLayout() { ColumnName = "referer", DataType="text" },
            new DbLayout() { ColumnName = "last_update", DataType="integer" },
            new DbLayout() { ColumnName = "last_check", DataType="integer" },
            new DbLayout() { ColumnName = "is_big_image", DataType="boolean" },
            new DbLayout() { ColumnName = "parent", DataType="text" }
        };

        /// <summary>
        /// Checks if the database file contains the same schema that the LNReader android app uses.
        /// </summary>
        /// <returns></returns>
        public async Task<bool> IsDatabaseValid()
        {
            var dblayout = new List<DbLayout>();
            bool isValid = false;
            try
            {
                using (var conn = new SQLiteConnection(ConnectionString))
                {
                    await conn.OpenAsync();
                    using (var transaction = conn.BeginTransaction())
                    {
                        using (var cmd = new SQLiteCommand(conn))
                        {
                            cmd.CommandText = "PRAGMA table_info('images')";

                            using (var reader = await cmd.ExecuteReaderAsync())
                            {
                                while (await reader.ReadAsync())
                                {
                                    dblayout.Add(new DbLayout()
                                    {
                                        ColumnName = reader.GetString(1),
                                        DataType = reader.GetString(2)
                                    });
                                }

                                if (dblayout.SequenceEqual(ImagesDbLayout))
                                    isValid = true;
                            }
                        }
                        transaction.Commit();
                    }

                    conn.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + "\n\n" + ex.StackTrace, ex.Source);
            }

            return isValid;
        }

        /// <summary>
        /// Gets all items from the images table in the LNReader database.
        /// </summary>
        /// <returns>List of image items</returns>
        public async Task<List<DbImage>> GetDbImagesFromDb()
        {
            var list = new List<DbImage>();

            try
            {
                using (var conn = new SQLiteConnection(ConnectionString))
                {
                    await conn.OpenAsync();
                    using (var transaction = conn.BeginTransaction())
                    {
                        using (var cmd = new SQLiteCommand(conn))
                        {
                            cmd.CommandText = 
                                "SELECT _id, name, filepath, url, referer, last_update, last_check, is_big_image, parent FROM images";
                            // SELECT * FROM blahtable WHERE col='@val1'
                            // cmd.Parameters.AddWithValue("@val1", someobject);
                            // execute command

                            using (var reader = await cmd.ExecuteReaderAsync())
                            {
                                while (await reader.ReadAsync())
                                {
                                    DbImage dbImage = new DbImage();
                                    dbImage.ID = reader.GetInt64(0);
                                    dbImage.Name = reader.GetString(1);
                                    dbImage.FilePath = reader.GetString(2);
                                    dbImage.Url = reader.GetString(3);
                                    /* The referer column in the database have DbNull values
                                     * Since the sqlite engine's GetString() throws 
                                     * exceptions when reading DbNull values, 
                                     * GetValue().ToString() returns an empty
                                     * string upon a DbNull
                                     */
                                    dbImage.Referer = reader.GetValue(4).ToString() ?? "";
                                    dbImage.LastUpdate = reader.GetInt64(5);
                                    dbImage.LastCheck = reader.GetInt64(6);
                                    dbImage.IsBigImage = reader.GetBoolean(7);
                                    dbImage.Parent = reader.GetString(8);
                                    list.Add(dbImage);
                                    
                                }
                            }
                        }
                        transaction.Commit();
                    }

                    conn.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + "\n\n" + ex.StackTrace, ex.Source);
            }
            
            return list;
        }

        /// <summary>
        /// Use this method to update all the image file path in the db.
        /// WARNING: It changes the part before the /projects/... in the file path for ALL THE IMAGES.
        /// Use this only if you want the images to be gathered under the same project folder<para />
        /// 
        /// 
        /// TL:DR; It allows you to move the files downloaded by the LNReader app in a different location,
        /// if the LNReader app stored the downloaded images at /storage/emulated/0/Android/data/com.erakk.lnreader/files/images/ before,
        /// this method will allow you to change that into a new specified location in the db.<para />
        /// 
        /// e.g.
        /// From: /storage/emulated/0/Android/data/com.erakk.lnreader/files/images/project/images/0/00/TWGOK_02_017.jpg<para />
        /// To: /storage/4C36-6A34/Android/data/com.erakk.lnreader/files/images/project/images/0/00/TWGOK_02_017.jpg<para />
        /// From: /storage/emulated/0/Android/data/com.erakk.lnreader/files/images/project/images/0/00/TWGOK_02_017.jpg<para />
        /// To: /storage/emulated/0/LNReaderFiles/images/project/images/0/00/TWGOK_02_017.jpg<para />
        /// </summary>
        /// <param name="newpath">The new path where the user wants to store the /project/.. folder at</param>
        /// <returns></returns>
        public async Task<int> UpdateAllImageFilePath(string newpath, List<DbImage> dbImage)
        {
            int changedRows = 0;
            try
            {
                using (var conn = new SQLiteConnection(ConnectionString))
                {
                    await conn.OpenAsync();
                    using (SQLiteTransaction transaction = conn.BeginTransaction())
                    {
                        using (var cmd = new SQLiteCommand(conn))
                        {
                            foreach (var image in dbImage)
                            {
                                cmd.CommandText = "UPDATE images SET filepath=@newpath WHERE _id=@id";
                                
                                cmd.Parameters.AddWithValue("@newpath", newpath + image.Name);
                                cmd.Parameters.AddWithValue("@id", image.ID);

                                changedRows = await cmd.ExecuteNonQueryAsync() + changedRows;
                            }
                        }
                        transaction.Commit();
                    }

                    conn.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + "\n\n" + ex.StackTrace, ex.Source);
            }

            return changedRows;
        }
    }
}
