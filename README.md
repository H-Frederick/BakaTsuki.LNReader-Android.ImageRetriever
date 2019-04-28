# Image Retriever


Image retriever CLI app in C# to restore images from the [Baka-Tsuki LNReader-Android App](https://github.com/calvinaquino/LNReader-Android) if the image folder was accidentally deleted. Originally written for private use, but refactored for public use. Use this if you have to restore thousands upon thousands of images in your device downloaded by the BakaTsuki LNReader.

### Requirements

###### For end users:

* .NET Framework 4.7.2 or above

###### For developers:

* Same requirements as above
* .NET Framework 4.7.2 SDK or above
* [NuGet Package System.Data.SQLite.Core](https://www.nuget.org/packages/System.Data.SQLite.Core/)

### Instructions

1. Download the binaries from release tab and place it in your PC. Alternatively, you can pull the repo and build the project from Visual Studio.
2. Export the database from the LNReader app in your device (Settings -> Storage -> Create Novel Database Backup) and place it in the same place as the executable BakaTsuki.LNReader-Android.ImageRetriever.exe
3. Run the .exe and follow the prompts as needed.
4. After downloading the images, Image Retriever will prompt if you want to relocate the images' file paths to be located under the same location. Choosing to do so will relocate all the recorded image file paths in the database to be under the new location you'll input.
5. All the downloaded files will be placed inside a folder named 'images', it will be created by Image Retriever in the same location it's run at. Copy everything that is created in there and paste it in your device.

### Notes

* Place the extracted app in a location where there is Read/Write permission.
* The extracted app will always create a folder called 'images' before doing its operations.
* Image Retriever **will** overwrite the database file that you pass in. So keep a copy nearby.
* **WARNING** Depending upon the amount of images in the database file, it may take a while to download the images.

### TODO:

* Better filepath validation that is acceptable for the android OS. See [here](https://github.com/H-Frederick/BakaTsuki.LNReader-Android.ImageRetriever/blob/2146339b04136956c008b4a5da39391503207419/Core/App.cs#L357) for details.
* Refactor the app to allow command line arguments for automated process.
* Change build target to .NET Core 3.0 and set build as a self-contained app to allow deployment without relying on having .NET Framework installed on a user's machine.