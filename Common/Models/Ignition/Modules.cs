using System.Collections.Generic;

namespace Common.Models.Ignition
{
    public class Modules
    {
        public string Name { get; set; }
        public string Version { get; set; }
        public string Description { get; set; }
        public string License { get; set; }
        public string State { get; set; }
        public List<string> Actions { get; set; }
    }
}