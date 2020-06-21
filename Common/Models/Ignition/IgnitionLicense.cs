using System.Collections.Generic;

namespace Common.Models.Ignition
{
    public class IgnitionLicense
    {
        public Primary primary { get; set; }
        public List<Secondary> secondary { get; set; }
    }
}