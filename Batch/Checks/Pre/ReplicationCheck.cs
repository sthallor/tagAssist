using System;
using Batch.FactoryStuff;
using Common;
using Common.Database;
using Common.Extensions;
using Common.Models.Reporting;

namespace Batch.Checks.Pre
{
    public class ReplicationCheck : IPreCheck
    {
        private const int Error = 90;
        private const int Warning = 45;

        public bool Check()
        {
            var egnServer = new EgnServer { Server = "cal0-vp-ace01" };
            var reportingLastTag = ReportingDb.GetLastTag();
            var transactionalLastTag = EnterpriseDb.GetLastTag();

            // Replication
            var timeSpan = transactionalLastTag.Subtract(reportingLastTag);
            var message = $"Replication is out by {timeSpan.ToPrettyFormat()}";
            if (timeSpan.TotalMinutes > Error)
            {
                EsiLog.HardError(egnServer, message, "Internal" );
                Utility.SendMessage(message);
                return false;
            }

            if (timeSpan.TotalMinutes > Warning)
            {
                EsiLog.Warn(egnServer, message, "Internal");
                return true;
            }

            // Transactional
            var transactionSpan = DateTime.Now - transactionalLastTag;
            var formattableString = $"IgnitionEnterprise transaction DB hasn't seen a tag in {transactionSpan.ToPrettyFormat()}";
            if (transactionSpan.TotalMinutes > Error)
            {
                EsiLog.HardError(egnServer, formattableString, "Internal");
                Utility.SendMessage(formattableString);
                return false;
            }
            if (transactionSpan.TotalMinutes > Warning)
            {
                EsiLog.Warn(egnServer, formattableString, "Internal");
                Utility.SendMessage(formattableString);
                return false;
            }

            EsiLog.Info(egnServer, "Replication/Transactional DBs are working.");
            return true;
        }
    }
}