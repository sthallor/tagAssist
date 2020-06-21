using System.Collections.Generic;

namespace Common.Models.Models
{
    public class HistoryProvider
    {
        public override string ToString()
        {
            return $"RootFolder: {RootFolder}  Provider: {Name}  ScanClass: {ScanClass}";
        }

        public string Name { get; set; }
        public string ScanClass { get; set; }
        public string RootFolder { get; set; }
        public List<string> TagList { get; set; }
        public List<string> FolderList { get; set; }
        public List<IgnitionData> IgnitionTags { get; set; }
    }
}