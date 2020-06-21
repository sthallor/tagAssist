using System.ComponentModel.DataAnnotations;

namespace Common.Models.Igor
{
    public class RealTimeRigStateVersion
    {
        [Key]
        public string Rig { get; set; }
        public string Server { get; set; }
        public string Version { get; set; }
        public string MD5 { get; set; }
    }
}