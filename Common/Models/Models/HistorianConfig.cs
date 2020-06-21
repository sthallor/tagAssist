using System.Collections.Generic;

namespace Common.Models.Models
{
    public class HistorianConfig
    {
        public string Rig { get; set; }
        public HistEnvironment HistEnvironment { get; set; }
        public List<OpcUaServerConfig> OpcUaServers { get; set; }
    }
}