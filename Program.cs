using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading.Tasks;
using BakaTsuki.LNReader_Android.ImageRetriever.Core;

namespace BakaTsuki.LNReader_Android.ImageRetriever
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var app = new App();
            app.Init();
            await app.SetUp();
            await app.DownloadImages();
            await app.RelinkImagePaths();

            Console.WriteLine("Done! Press any key to exit");
            Console.Read();
        }
    }
}
