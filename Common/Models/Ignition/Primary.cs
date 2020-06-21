using System.Collections.Generic;

namespace Common.Models.Ignition
{
    public class Primary
    {
        public string key { get; set; }
        public Platform platform { get; set; }
        public List<Module> modules { get; set; }
    }
}
