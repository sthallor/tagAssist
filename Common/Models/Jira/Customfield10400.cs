using Newtonsoft.Json;

namespace Common.Models.Jira
{
    public class Customfield10400
    {
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string self { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string value { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string id { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string name { get; set; }
    }
}
 