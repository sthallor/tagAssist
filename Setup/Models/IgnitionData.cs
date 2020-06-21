using System;
using System.Data;
using System.Linq;
using Opc.Ua;

namespace Setup.Models
{
    public class IgnitionData
    {
        public decimal SqlTagId { get; set; }
        public string Path { get; set; }
        public string Name { get; set; }
        public string DataType { get; set; }
        public string OpcItemPath { get; set; }
        public string OpcServer { get; set; }
        public string Root { get; set; }
        public string HistoricalScanclass { get; set; }
        public string HistoryProvider { get; set; }

        protected bool Equals(IgnitionData other)
        {
            return string.Equals(Name, other.Name) && string.Equals(Path, other.Path);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((IgnitionData)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Name != null ? Name.GetHashCode() : 0) * 397) ^ (Path != null ? Path.GetHashCode() : 0);
            }
        }

        public IgnitionData(IDataRecord reader)
        {
            SqlTagId = (decimal)reader[0];
            Path = (string)reader[1];
            Name = (string)reader[2];
            DataType = (string)reader[3];
            OpcItemPath = (string)reader[4];
            OpcServer = (string)reader[5];
            HistoricalScanclass = (string)reader[6];
            HistoryProvider = (string)reader[7];
        }


        public IgnitionData()
        {
        }

        private string GetDataType(uint dataType)
        {
            switch (dataType)
            {
                case 1:
                    return "Boolean";
                case 2:
                    return "Int1";
                case 4:
                    return "Int2";
                case 6:
                    return "Int4";
                case 8:
                    return "Int8";
                case 10:
                    return "Float4";
                case 11:
                    return "Float8";
                case 12:
                    return "String";
                default:
                    Console.WriteLine(dataType);
                    throw new NotImplementedException();
            }
        }
        public string GetPath()
        {
            var substring = OpcItemPath.Substring(OpcItemPath.LastIndexOf(']') + 1);
            var replace = substring.Replace(Name, "");
            var trimEnd = replace.TrimEnd('/');
            if (OpcServer == "IgnitionACE")
                return "s1500/" + trimEnd;
            return trimEnd;
        }
        public string GetRoot()
        {
            var s = OpcItemPath.Split(']').Last();
            if (!s.Contains("\\") && !s.Contains("/"))
                return OpcServer == "RigHistorian" ? "\\" : "/";
            return s.Substring(0, OpcServer == "RigHistorian" ? s.IndexOf('\\') : s.IndexOf('/'));
        }
    }
}