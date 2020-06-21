using System.ComponentModel.DataAnnotations;

namespace Common.Models.Igor
{
    public class RigIgorServiceVersion
    {
        [Key]
        public string Rig { get; set; }
        public string Server { get; set; }
        public decimal Version { get; set; }
    }
}