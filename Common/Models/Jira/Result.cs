namespace Common.Models.Jira
{
    public class Result
    {
        public string id { get; set; }
        public string key { get; set; }
        public string self { get; set; }
        public override string ToString()
        {
            return $"ID:{id} Key:{key} Slef:{self}";
        }
    }
}