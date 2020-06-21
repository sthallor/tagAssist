using System;
using System.Linq;
using System.Reflection;
using Common.Database;
using Common.Extensions;
using Common.Models;
using Common.Models.Reporting;
using log4net;

namespace Common
{
    public class EsiLog
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public static void Debug(EgnServer egn, string message)
        {
            var checkStatus = new CheckStatus { Host = egn.Server, Rig = egn.RigNumber, Message = message, Error = 0 };
            if (!Singleton.Instance.Bag.Any(x => x.Rig == egn.RigNumber && x.Error == 0 && x.Host == egn.Server && x.Message == message))
            {
                Singleton.Instance.Bag.Add(checkStatus);
                Log.Debug($"{egn.Server}({egn.RigNumber}) {message}");
            }
        }

        public static void Info(EgnServer egn, string message)
        {
            Singleton.Instance.Bag.Add(new CheckStatus { Host = egn.Server, Rig = egn.RigNumber, Message = message, Error = 1 });
            Log.Info($"{egn.Server}({egn.RigNumber}) {message}");
        }
        public static void Warn(EgnServer egn, string message, string category)
        {
            Singleton.Instance.Bag.Add(new CheckStatus { Host = egn.Server, Rig = egn.RigNumber, Message = message, Error = 2, Category = category});
            Log.Warn($"{egn.Server}({egn.RigNumber}) {message}");
        }

        public static void HardError(EgnServer egn, string message, string category)
        {
            // Don't write duplicate messages here.
            if(Singleton.Instance.Bag.Any(x => x.Host == egn.Server && x.Rig == egn.RigNumber && x.Message == message && x.Category == category))
                return;
            Singleton.Instance.Bag.Add(new CheckStatus { Host = egn.Server, Rig = egn.RigNumber, Message = message, Error = 3, Category = category});
            var s = $"{egn.Server}({egn.RigNumber}) {message}";
            Log.Error(s);
        }

        public static void Error(EgnServer egn, string message)
        {
            try
            {
                var rigRemarks = ReportingDb.GetRigRemarks(egn.RigNumber);
                var lastRemark = rigRemarks.OrderByDescending(x=> x.EffectiveDate).FirstOrDefault();
                var rigIsMoving = 
                   lastRemark.RemarkType.ToLower() == "rig idle" ||
                   lastRemark.RemarkType.ToLower() == "move rig" ||
                   lastRemark.RemarkType.ToLower() == "rig up" ||
                   lastRemark.RemarkType.ToLower() == "rig up equipment" ||
                   lastRemark.RemarkType.ToLower() == "rig watch" ||
                   lastRemark.RemarkType.ToLower() == "nipple down bop" ||
                   lastRemark.RemarkType.ToLower() == "nipple up bop" ||
                   lastRemark.RemarkType.ToLower() == "w/o lease/location" ||
                   lastRemark.RemarkType.ToLower() == "wait on lease" ||
                   lastRemark.RemarkType.ToLower() == "tear down" ||
                   lastRemark.RemarkType.ToLower() == "working day l" ||
                   lastRemark.RemarkType.ToLower() == "equipment upgrade/maintenance" ||
                   lastRemark.RemarkType.ToLower().Contains("rig down") ||
                   lastRemark.RemarkType.ToLower().Contains("downtime") ||
                   lastRemark.Remark.ToLower().Contains("move rig") ||
                   lastRemark.Remark.ToLower().Contains("rig idle") ||
                   lastRemark.Remark.ToLower().Contains("rig up") ||
                   lastRemark.Remark.ToLower().Contains("rig down") ||
                   lastRemark.Remark.ToLower().Contains("rig release") ||
                   lastRemark.Remark.ToLower().Contains("wait on daylight") ||
                   lastRemark.Remark.ToLower().Contains("hot stacked") ||
                   lastRemark.Remark.ToLower().Contains("rigging down");

                var drillingPaused = !rigRemarks.Any(x => x.EffectiveDate > DateTime.Now.Subtract(TimeSpan.FromDays(5))); // 5 Days Since Last Tour Sheet

                var rigTags = Singleton.Instance.GetTagData().Where(x => x.Rig == egn.RigNumber).ToList();
                var recentTagData = rigTags.Count > 0 && rigTags.All(x => x.GetHoursSinceLastTag() < Singleton.DelinquentAfterHours);

                var activeTicket = Singleton.Instance.GetJiraTickets().FirstOrDefault(x => x.RigNumber == egn.RigNumber && x.Status != "Canceled");

                var recentCanceledTicket = Singleton.Instance.GetJiraTickets().FirstOrDefault(x => x.RigNumber == egn.RigNumber && 
                    x.Status == "Canceled" && x.Created > DateTime.Now.Subtract(TimeSpan.FromDays(4)));

                var noRecentEdr = rigTags.OrderBy(x=> x.GetHoursSinceLastEdr()).FirstOrDefault(x=> x.GetHoursSinceLastEdr() > 4);
                if (noRecentEdr != null)
                {
                    var noEdrTimeSpan = TimeSpan.FromHours(noRecentEdr.GetHoursSinceLastEdr());
                    Debug(egn, $"Ignoring issue: No recent EDR data. {noEdrTimeSpan.ToPrettyFormat()}");
                }

                if (rigIsMoving)
                {
                    Debug(egn, "Ignoring issue: Rig remarks indicate rigging up/down/idle or moving.");
                }
                if (drillingPaused)
                {
                    Debug(egn, "Ignoring issue: Rig remarks indicate a pause in drilling activity.");
                }
                if (activeTicket != null)
                {
                    if(!Singleton.Instance.Bag.Any(x=> x.Rig == egn.RigNumber && x.Message.Contains("Already an open JIRA ticket")))
                        Debug(egn, $"Ignoring issue: Already an open JIRA ticket <a href=\"https://ensignenergy.atlassian.net/browse/{activeTicket.Key}\">{activeTicket.Key}</a>.");
                }
                if (recentCanceledTicket != null)
                {
                    if (!Singleton.Instance.Bag.Any(x => x.Rig == egn.RigNumber && x.Message.Contains("Recently canceled JIRA ticket")))
                        Debug(egn, $"Ignoring issue: Recently canceled JIRA ticket <a href=\"https://ensignenergy.atlassian.net/browse/{recentCanceledTicket.Key}\">{recentCanceledTicket.Key}</a>.");
                }
                if (recentTagData)
                {
                    var rigTag = rigTags.OrderBy(x => x.GetHoursSinceLastTag()).FirstOrDefault();
                    var timeSpan = TimeSpan.FromHours(rigTag.GetHoursSinceLastTag());
                    Debug(egn, $"Ignoring issue: It's only been {timeSpan.ToPrettyFormat()}since last tag data.");
                }
                if (rigIsMoving || recentTagData || drillingPaused || activeTicket != null || recentCanceledTicket != null || noRecentEdr != null)
                {
                    Warn(egn, message, "");
                }
                else
                {
                    Singleton.Instance.Bag.Add(new CheckStatus { Host = egn.Server, Rig = egn.RigNumber, Message = message, Error = 3 });
                    Log.Error($"{egn.Server}({egn.RigNumber}) {message}");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }
}