using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Common.Database;
using Common.Models.Jira;
using Newtonsoft.Json;
using Telegram.Bot.Types.Enums;

namespace Common.Controllers
{
    public static class JiraController
    {
        private const string UserPassword = "brian.ogletree@ensignenergy.com:DpGviEvcPoSaf2A57kkuB0C1";
        private const string BaseAddress = "https://ensignenergy.atlassian.net/";

        public static void CreateTickets()
        {
            var rigsInErrorState = Singleton.Instance.Bag.Where(x => x.Error == 3 && x.Category != "Internal").Select(x => x.Rig).Distinct().ToList();
            foreach (var rigInError in rigsInErrorState)
            {
                var description = "";
                var warnAndErrorResults = Singleton.Instance.Bag.Where(x => x.Rig == rigInError && x.Error >= 2 && x.Category != "Internal").ToList();
                foreach (var result in warnAndErrorResults)
                    description += result.Message + "\n";
                CreateJiraTicket(rigInError, description);
            }
        }

        public static void CreateJiraTicket(string rig, string description)
        {
            var egnServer = ReportingDb.GetEgnServers().FirstOrDefault(x => x.RigNumber == rig);
            if (egnServer == null) return;
            Singleton.Instance.SendMessage(egnServer, $"Do you want to create a JIRA Ticket?\n{description}");
        }

        public static List<JiraTicket> GetJiraTickets()
        {
            var result = PostJiraContent(GetJiraQueryString(), "rest/api/2/search").Result;
            var jiraRootObject = JsonConvert.DeserializeObject<JiraRootObject>(result);
            var jiraTickets = JiraTickets(jiraRootObject);
            return jiraTickets;
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

        public static void SendJiraSummary()
        {
            var strBody = new StringBuilder();
            strBody.Append("<html><head>");
            strBody.Append(Utility.MarkStyle());
            strBody.Append("</head>");
            strBody.Append("<body>");
            strBody.Append(string.Format("<h3>Igor JIRA Summary</h3><table border=\"1\">" +
                                         "<thead><tr><th>Key</th><th>EgnNumber</th><th>RigNumber</th><th>Assignee</th><th>Created</th><th>Updated</th><th>Status</th><th style=\"text-align:left\">Description</th></tr></thead><tbody>"));
            foreach (var ticket in Singleton.Instance.GetJiraTickets().Where(x=>x.Status != "Canceled").OrderBy(x => x.RigNumber))
            {
                strBody.Append($@"<tr>
<td><a href=""https://ensignenergy.atlassian.net/browse/{ticket.Key}"">{ticket.Key}</a></td>
<td><a href=""http://{ticket.EgnNumber}:8088"">{ticket.EgnNumber}</a></td>
<td>{ticket.RigNumber}</td>
<td>{ticket.Assignee}</td>
<td>{ticket.Created:M}</td>
<td>{ticket.Updated:M}</td>
<td>{ticket.Status}</td>
<td style=""text-align:left"">{ticket.Description?.Replace("\n", "<br>")}</td>
</tr>");
            }
            strBody.Append("</tbody></table></body></html>");
            Utility.SendEmailMessage(strBody.ToString());
        }

        private static List<JiraTicket> JiraTickets(JiraRootObject jiraRootObject)
        {
            var jiraTickets = new List<JiraTicket>();
            var egnServers = ReportingDb.GetEgnServers();
            foreach (var issue in jiraRootObject.issues)
            {
                var jiraTicket = new JiraTicket
                {
                    Id = issue.id,
                    Key = issue.key,
                    RigNumber = issue.fields.customfield_10400?.value,
                    
                    Summary = issue.fields.summary,
                    Assignee = issue.fields.assignee?.name,
                    Created = issue.fields.created,
                    Updated = issue.fields.updated,
                    Description = issue.fields.description,
                    Status = issue.fields.status?.name
                };
                if (jiraTicket.RigNumber.StartsWith("T") && jiraTicket.RigNumber.Length == 3)
                {
                    jiraTicket.RigNumber = jiraTicket.RigNumber.Replace("T", "T0");
                }
                jiraTicket.EgnNumber = egnServers.FirstOrDefault(x => x.RigNumber == jiraTicket.RigNumber)?.Server;
                jiraTickets.Add(jiraTicket);
            }
            return jiraTickets;
        }

        private static StringContent GetJiraQueryString()
        {
            const string query = @"{
    ""jql"": ""created > -365d and watcher = currentUser() and project = WTSS AND status in (Canceled, Open, \""In Progress\"", Reopened, Backlog, \""Selected for Development\"", \""Code Review\"", Testing, \""To Do\"", \""In Review\"", \""Waiting for support\"", \""Waiting for customer\"", Pending, Escalated, \""Work in progress\"")"",
    ""startAt"": 0,
    ""maxResults"": 50,
    ""fields"": [
        ""summary"",
        ""status"",
        ""assignee"",
        ""customfield_10400"",
        ""text"",
        ""updated"",
        ""created"",
        ""description""
    ]
}";
            var stringContent = new StringContent(query, Encoding.UTF8, "application/json");
            return stringContent;
        }
    }
}