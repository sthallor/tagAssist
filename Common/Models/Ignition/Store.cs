namespace Common.Models.Ignition
{
    public class Store
    {
        public bool isAvailable { get; set; }
        public double storeThroughput { get; set; }
        public string storeName { get; set; }
        public int quarantined { get; set; }
        public double? forwardThroughput { get; set; }
    }
}