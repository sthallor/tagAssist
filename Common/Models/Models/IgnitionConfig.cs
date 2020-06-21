using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace Common.Models.Models
{
    public class IgnitionConfig
    {
        public WebStoreAndForward WebStoreAndForward { get; set; }
        public Databases Databases { get; set; }
        public IgnitionModules Modules { get; set; }
        public Tags Tags { get; set; }
        public static IgnitionConfig GetConfig()
        {
            return JsonConvert.DeserializeObject<IgnitionConfig>(File.ReadAllText(@"C:\Installs\IgorConfig\Common\IgnitionConfig.json"));
        }
    }

    public class Tags
    {
        public Local Local { get; set; }
        public Splitter Splitter { get; set; }
    }

    public class Local
    {
        public string Name { get; set; }
        public int PartitionLength { get; set; }
        public string PartitionUnits { get; set; }
        public int PruneAge { get; set; }
        public string PruneAgeUnits { get; set; }
    }

    public class Splitter
    {
        public string Name { get; set; }
        public string FirstConnection { get; set; }
        public string SecondConnection { get; set; }
    }
    public class Databases
    {
        public Translators Translators { get; set; }
        public Connections Connections { get; set; }
    }

    public class Connections
    {
        public Connection MYSQL { get; set; }
    }

    public class Connection
    {
        public string Name { get; set; }
        public string ConnectUrl { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public class Translators
    {
        public Translator MYSQL { get; set; }
    }

    public class Translator
    {
        public string DataTypeMappingString { get; set; }
    }

    public class IgnitionModules
    {
        public List<string> RemoveModules { get; set; }
    }
}