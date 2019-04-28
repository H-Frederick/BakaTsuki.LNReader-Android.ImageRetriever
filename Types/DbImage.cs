using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BakaTsuki.LNReader_Android.ImageRetriever.Types
{
    /// <summary>
    /// A database entity type containing the columns from the images table in the LNReader database.
    /// </summary>
    public class DbImage
    {
        /// <summary>
        /// _id
        /// </summary>
        public long ID { get; set; }

        /// <summary>
        /// name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// filepath
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// url
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// referer
        /// </summary>
        public string Referer { get; set; }

        /// <summary>
        /// last_update
        /// </summary>
        public long LastUpdate { get; set; }

        /// <summary>
        /// last_check
        /// </summary>
        public long LastCheck { get; set; }

        /// <summary>
        /// is_big_image
        /// </summary>
        public bool IsBigImage { get; set; }

        /// <summary>
        /// parent
        /// </summary>
        public string Parent { get; set; }
    }
}
