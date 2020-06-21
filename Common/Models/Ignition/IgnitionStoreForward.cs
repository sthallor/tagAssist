using System.Collections.Generic;

namespace Common.Models.Ignition
{
    public class IgnitionStoreForward
    {
        public int totalDropped { get; set; }
        public double aggregateThroughput { get; set; }
        public bool isEdge { get; set; }
        public List<Store> stores { get; set; }
        public int storeCount { get; set; }
        public int totalQuarantined { get; set; }
    }
}