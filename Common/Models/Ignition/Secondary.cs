using System.Collections.Generic;

namespace Common.Models.Ignition
{
    public class Secondary
    {
        public string key { get; set; }
        public List<Module> modules { get; set; }
    }
}