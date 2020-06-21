using System.Collections.Generic;

namespace Common.Models.Ignition
{
    public class QuarantineDetail
    {
        public List<Store> stores { get; set; }
        public string name { get; set; }
        public List<QuarantinedItem> quarantinedItems { get; set; }
        public int quarantined { get; set; }
    }
}