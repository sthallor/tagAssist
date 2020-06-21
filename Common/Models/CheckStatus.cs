namespace Common.Models
{
    public class CheckStatus
    {
        public string Host { get; set; }
        public string Rig { get; set; }
        public string Message { get; set; }
        public int Error { get; set; }
        public string Category { get; set; }
    }
}