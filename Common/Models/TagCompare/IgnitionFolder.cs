using System.Data.SQLite;

namespace Common.Models.TagCompare
{
    public class IgnitionFolder
    {
        public IgnitionFolder(SQLiteDataReader reader)
        {
            Name = (string)reader[0];
            Path = (string)reader[1];
        }

        public override string ToString()
        {
            return Path + "/" + Name;
        }

        public IgnitionFolder()
        {
        }

        protected bool Equals(IgnitionFolder other)
        {
            return string.Equals(Name, other.Name) && string.Equals(Path, other.Path);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((IgnitionFolder) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Name != null ? Name.GetHashCode() : 0) * 397) ^ (Path != null ? Path.GetHashCode() : 0);
            }
        }

        public string Name { get; set; }
        public string Path { get; set; }
    }
}