using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Common.Database.Context;
using Common.Models.Jira;
using log4net;
using Newtonsoft.Json;

namespace Common.Database
{
    public class EnterpriseDb
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public static void SaveJiraTickets()
        {
            var jiraTickets = Singleton.Instance.GetJiraTickets().Where(x=> x.Status != "Canceled").Distinct(new JiraDuplicateTicketsFixEqualityComparer()).ToList();
            var serializeObject = JsonConvert.SerializeObject(jiraTickets, Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
            Retry.Do(() => File.WriteAllText(@"\\CAL0-VP-ACE01\e$\share\IgorConfig\Common\JiraTickets.json", serializeObject), TimeSpan.FromMinutes(2));

            using (var dbContext = new EnterpriseDbContext())
            {
                dbContext.Database.ExecuteSqlCommand("delete from rig_issuetracking_tickets");
                foreach (var ticket in jiraTickets)
                {
                    var isActive = "1"; //jiraTicket.Status == "Canceled" ? "0" : "1";
                    dbContext.Database.ExecuteSqlCommand(
                        $"Insert into rig_issuetracking_tickets Values('{ticket.RigNumber}','{ticket.Key}','https://ensignenergy.atlassian.net/browse/{ticket.Key}', {isActive})");
                }
            }
        }
        public static DateTime GetLastTag()
        {
            using (var dbContext = new EnterpriseDbContext())
            {
                const string sql = "select top 1 t_stamp from sqlth_1_data order by t_stamp desc";
                var transactionalLastTag = dbContext.Database.SqlQuery<long>(sql).FirstOrDefault();
                return Utility.UnixTimeStampToDateTime(transactionalLastTag);
            }
        }

        public static void ExecuteQueryStatements(List<string> statements)
        {
            if (statements.Count == 0) return;
            using (var dbContext = new EnterpriseDbContext())
            {
                foreach (var statement in statements)
                {
                    Log.Info($"Executing query statement {statement}");
                    dbContext.Database.ExecuteSqlCommand(statement);
                }
            }
        }
    }
}