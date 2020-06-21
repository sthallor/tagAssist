using System.ComponentModel.DataAnnotations;

namespace Common.Models.Igor
{
    public class ResetTrial
    {
        [Key]
        public string Rig { get; set; }
        public string Server { get; set; }
    }
}
