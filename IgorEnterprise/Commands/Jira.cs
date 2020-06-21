using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Common;
using Common.Database;
using Common.Models.Jira;
using Common.Models.Reporting;
using log4net;
using Newtonsoft.Json;
using Telegram.Bot.Types;

namespace IgorEnterprise.Commands
{
    public class Jira
    {
        private const string UserPassword = "brian.ogletree@ensignenergy.com:DpGviEvcPoSaf2A57kkuB0C1";
        private const string BaseAddress = "https://ensignenergy.atlassian.net/";

        private readonly Message _message;
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private EgnServer _egn;

        public Jira(Message message)
        {
            _message = message;
            Log.Info("Received command to Jira.");
        }

        public void Execute()
        {
            try
            {
                GetRig();
                DoThing();
            }
            catch (Exception e)
            {
                Log.Error(e.Message);
                Log.Error(e.ToString());

                Singleton.Instance.SendMessage(_egn, $"Failed to execute Jira command. {e.Message}");
            }
        }

        private void DoThing()
        {
            var messageText = _message.ReplyToMessage.Text.Split(new []{"\n"}, StringSplitOptions.None);
            var description = messageText.Skip(1).Aggregate("", (current, s) => current + (s + "\n"));
            var issue = new Issue
            {
                fields = new Fields
                {
                    project = new Project { key = "WTSS" },
                    issuetype = new IssueType { name = "Edge Analytics" },
                    components = new[] { new Component { name = "Edge Analytics" } },
                    assignee = new Person { accountId = "557058:28777449-8f0c-4ef2-baf8-49e790e93444" }, // Tony
                    summary = $"Historian communication problems on Rig {_egn.RigNumber}",
                    customfield_10400 = new Customfield10400 { value = $"{_egn.RigNumber}" },
                    customfield_10453 = new Customfield10453 { value = "Other" },
                    customfield_10454 = new Customfield10454 { value = $"{_egn.EgnKitNumber}" },
                    description = description
                }
            };
            issue.fields.customfield_10400.value = issue.fields.customfield_10400.value.Replace("T0", "T");
            var serializeObject = JsonConvert.SerializeObject(issue);
            var stringContent = new StringContent(serializeObject, Encoding.UTF8, "application/json");
            var result = PostJiraContent(stringContent, "/rest/api/2/issue").Result;
            var jiraResult = JsonConvert.DeserializeObject<Result>(result);
            var message = $"Created Jira ticket <a href=\"https://ensignenergy.atlassian.net/browse/{jiraResult.key}\">{jiraResult.key}</a>";
            Singleton.Instance.SendMessage(message);
            Log.Info(jiraResult);
            if(string.IsNullOrWhiteSpace(jiraResult.key))
                Log.Warn(result);
        }

        private static async Task<string> PostJiraContent(StringContent stringContent, string requestUri)
        {
            using (var client = new HttpClient())
            {
                var byteArray = Encoding.ASCII.GetBytes(UserPassword);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
                client.BaseAddress = new Uri(BaseAddress);
                try
                {
                    var response = await client.PostAsync(requestUri, stringContent);
                    var result = await response.Content.ReadAsStringAsync();
                    return result;
                }
                catch (Exception)
                {
                    return null;
                }
            }
        }

        private void GetRig()
        {
            try
            {
                var rig = "";
                if (_message.ReplyToMessage != null)
                {
                    rig = _message.ReplyToMessage.EntityValues.FirstOrDefault();
                }

                if (_message.ReplyToMessage == null)
                {
                    rig = _message.Text.Split(' ')[1];
                }

                if (!string.IsNullOrEmpty(rig))
                {
                    _egn = ReportingDb.GetEgnServers().FirstOrDefault(x => x.RigNumber == rig);
                }
                if (_egn == null)
                    Log.Info($"Couldn't find rig for {rig}");
            }
            catch (Exception) { /* ignored */ }
        }
    }
}