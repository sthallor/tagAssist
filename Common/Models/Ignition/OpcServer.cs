using System.Collections.Generic;

namespace Common.Models.Ignition
{
    public class OpcServer
    {
        public string EgnHost { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Status { get; set; }
        public string RigNumber { get; set; }
        public List<string> Actions { get; set; }
    }
}