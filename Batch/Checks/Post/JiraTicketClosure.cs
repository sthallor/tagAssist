using System.Linq;
using Batch.FactoryStuff;
using Common;
using Common.Database;

namespace Batch.Checks.Post
{
    public class JiraTicketClosure : IPostCheck
    {
        public bool Check()
        {
            var stackedRigs = ReportingDb.GetStackedEgnServers();
            var allRigs = ReportingDb.GetAllEgnServers();
            // Ignore Canceled tickets. 
            // Also don't close manually created tickets. Those probably won't start with "Historian communication..."
            foreach (var ticket in Singleton.Instance.GetJiraTickets().Where(x => x.Status != "Canceled" && x.Summary.StartsWith("Historian communication")))
            {
                var server = allRigs.FirstOrDefault(x => x.RigNumber == ticket.RigNumber);
                if (server == null) continue;

                if (stackedRigs.Any(x => x.RigNumber == ticket.RigNumber))
                {
                    var message = $"Jira ticket <a href=\"https://ensignenergy.atlassian.net/browse/{ticket.Key}\">{ticket.Key}</a> Rig is now stacked.";
                    EsiLog.HardError(server, message, "Internal");
                    return true;
                }

                if (!Singleton.Instance.Bag.Any(x => x.Rig == ticket.RigNumber && x.Error > 1))
                {
                    var message = $"Jira ticket <a href=\"https://ensignenergy.atlassian.net/browse/{ticket.Key}\">{ticket.Key}</a> appears to have been resolved.";
                    EsiLog.HardError(server, message, "Internal");
                    return true;
                }
            }
            return true;
        }
    }
}