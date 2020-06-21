using System;
using Newtonsoft.Json;

namespace Common.Models.Jira
{
    public class Fields
    {
        public string summary { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public Person assignee { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public Person reporter { get; set; }
        public Customfield10400 customfield_10400 { get; set; } //Rig
        public Customfield10453 customfield_10453 { get; set; } //Reported by
        public Customfield10454 customfield_10454 { get; set; } //EGN
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public Status status { get; set; }
        public Project project { get; set; }
        public IssueType issuetype { get; set; }
        public Component[] components { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? created { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? updated { get; set; }
        public string description { get; set; }
    }
}