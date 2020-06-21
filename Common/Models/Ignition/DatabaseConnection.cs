namespace Common.Models.Ignition
{
    public class DatabaseConnection
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string JdbcDriver { get; set; }
        public string Translator { get; set; }
        public string Status { get; set; }
    }
}