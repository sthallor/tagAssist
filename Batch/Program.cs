using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Batch.FactoryStuff;
using Common;
using Common.Controllers;
using Common.Database;
using Common.Models.Reporting;
using log4net;

namespace Batch
{
    public static class Program
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private static void Main()
        {
            JiraController.GetJiraTickets();
            //DoInstall();
            //DoCheck();
            try
            {
                PreCheck();
                var egnServers = ReportingDb.GetEgnServers();
                Singleton.Instance.GetTagData();
                Parallel.ForEach(egnServers, EgnCheck);
                PostCheck();
                JiraTickets();
                SendMails();
            }
            catch (Exception e)
            {
                Singleton.Instance.SendMessage($"🚨 IgorBatch failed. {e}");
                Log.Error(e);
                throw;
            }
        }

        private static void DoInstall()
        {
            var server = new EgnServer { Server = "EGN14-VP-HIST01", RigNumber = "150", Division = "USR" };
            server.Init();
            //server.IgnitionController.DeleteModules(); !! doesn't exist yet.
            server.IgnitionController.SetGatewayName(); 
            server.IgnitionController.SetDbTranslation();
            server.IgnitionController.CreateDbConnection();
            server.IgnitionController.WebSandfEndpoint(); // This doesn't work if the module isn't installed. lol
            server.IgnitionController.CreateSplitter();
            server.IgnitionController.SetDataPruning();
        }

        private static void DoCheck()
        {
            var rigList = new List<string> {"776"};
            var egnServers = ReportingDb.GetEgnServers();//.Where(x => rigList.Contains(x.RigNumber));

            Parallel.ForEach(egnServers, egnServer =>
            {
                egnServer.Init();
                if (egnServer.IgnitionController.IsLoggedIn())
                {
                    //var igorService = new ConfigCheck();
                    //igorService.Check(egnServer);
                }
            });
        }

        private static void PreCheck()
        {
            var checks = CheckFactory.GetPreChecks();
            Parallel.ForEach(checks, check => { check.Check(); });
        }

        private static void EgnCheck(EgnServer egnServer)
        {
            egnServer.Init();
            var checks = CheckFactory.GetEgnChecks();
            Parallel.ForEach(checks, check => { check.Check(egnServer); });
        }

        private static void PostCheck()
        {
            var checks = CheckFactory.GetPostChecks();
            Parallel.ForEach(checks, check => { check.Check(); });
        }

        private static void JiraTickets()
        {
            JiraController.CreateTickets();
            EnterpriseDb.SaveJiraTickets();
        }

        private static void SendMails()
        {
            Utility.SendCheckStatusSummaryFull();
            Utility.SendCheckStatusSummaryActionable();
            //if (DateTime.Now.DayOfWeek == DayOfWeek.Monday && DateTime.Now.Hour < 9) JiraController.SendJiraSummary();
        }
    }
}