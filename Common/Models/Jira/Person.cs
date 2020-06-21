using Newtonsoft.Json;

namespace Common.Models.Jira
{
    public class Person
    {
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string self { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string name { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string key { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string accountId { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string emailAddress { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public AvatarUrls avatarUrls { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string displayName { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public bool active { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string timeZone { get; set; }
    }
}