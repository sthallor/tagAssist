using System.Linq;
using Batch.FactoryStuff;
using Common;
using Common.Models.Reporting;

namespace Batch.Checks
{
    public class Quarantine : IEgnCheck
    {
        public bool Check(EgnServer egnServer)
        {
            if (!egnServer.IgnitionController.IsLoggedIn())
                return false;
            // Retry quarantined data
            var quarantines = egnServer.IgnitionController.GetStoreAndForwardQuarantines().Where(x => x.TxnCount > 0).ToList();
            if (quarantines.Any())
            {
                foreach (var quarantine in quarantines)
                {
                    var message = $"Found quarantine of {quarantine.TxnCount} {quarantine.DataType} records.";
                    EsiLog.Info(egnServer, message);
                    egnServer.IgnitionController.InvokeQuarantineAction(quarantine.DataType, "[retry]");
                }
                if (egnServer.IgnitionController.GetStoreAndForwardQuarantines().Where(x => x.TxnCount > 0).ToList().Any())
                {
                    EsiLog.Info(egnServer, "Retrying quarantined records did not successfully clear the queue.");
                    return false;
                }
                EsiLog.Info(egnServer, "Retrying quarantined records successfully cleared the queue.");
                return true;
            }
            EsiLog.Info(egnServer, "Did not find quarantine records.");
            return true;
        }
    }
}