namespace Common.Models.Models
{
    public class Endpoint
    {
        public string Name { get; set; }
        public string RemoteEndpoint { get; set; }
        public int? ForwardFrequencyTime { get; set; }
        public string ForwardFrequencyUnits { get; set; }
    }
}