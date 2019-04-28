using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BakaTsuki.LNReader_Android.ImageRetriever.Types
{
    /// <summary>
    /// Contains properties to match the LNReader's database schema.
    /// </summary>
    public class DbLayout : IEquatable<DbLayout>
    {
        public string ColumnName { get; set; }
        public string DataType { get; set; }

        public bool Equals(DbLayout other)
        {
            if (other is null)
                return false;

            return this.ColumnName == other.ColumnName && this.DataType == other.DataType;
        }

        public override bool Equals(object obj) => Equals(obj as DbLayout);
        public override int GetHashCode() => (ColumnName, DataType).GetHashCode();
    }
}
