using Newtonsoft.Json;

namespace Common.Models.Ignition
{
    public class Module
    {
        public int version { get; set; }
        [JsonProperty("module-id")]
        public string ModuleId { get; set; }
        [JsonProperty("module-name")]
        public string ModuleName { get; set; }
        public Details details { get; set; }
    }
}