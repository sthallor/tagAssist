using System.Collections.Generic;

namespace Setup.Models
{
    public class HistoryProvider
    {
        public string Name { get; set; }
        public string ScanClass { get; set; }
        public string RootFolder { get; set; }
        public List<string> TagList { get; set; }
        public List<IgnitionData> IgnitionTags { get; set; }
    }
}