using System.Reflection;
using Batch.FactoryStuff;
using Common;
using Common.Database;
using Common.Models.Reporting;
using log4net;

namespace Batch.Checks.Post
{
    public class EdrDataEnterpriseTags : IPostCheck
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        public bool Check()
        {
            Log.Info("Starting EdrDataEnterpriseTags check");
            var statements = ReportingDb.GetMissingEdrTags();
            Log.Info($"Found {statements.Count} statements to execute");
            EnterpriseDb.ExecuteQueryStatements(statements);
            if (statements.Count > 0)
            {
                var egnServer = new EgnServer { Server = "cal0-vp-ace01" };
                EsiLog.HardError(egnServer, $"Inserted {statements.Count} records into sqlth_drv table.", "Internal");
            }
            Log.Info("Finished EdrDataEnterpriseTags check");
            return true;
        }
    }
}