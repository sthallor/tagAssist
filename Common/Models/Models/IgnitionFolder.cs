using System;
using System.Data.SQLite;

namespace Common.Models.Models
{
    public class IgnitionFolder: IEquatable<IgnitionFolder>
    {
        public IgnitionFolder(SQLiteDataReader reader)
        {
            Name = (string)reader[0];
            Path = (string)reader[1];
        }

        public bool Equals(IgnitionFolder other)
        {
            return Name.ToLower() == other.Name.ToLower() && Path.ToLower() == other.Path.ToLower();
        }

        public override string ToString()
        {
            return Path + "/" + Name;
        }
        public IgnitionFolder()
        {
        }

        public string Name { get; set; }
        public string Path { get; set; }
    }
}