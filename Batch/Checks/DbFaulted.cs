using System.Linq;
using Batch.FactoryStuff;
using Common;
using Common.Models.Reporting;

namespace Batch.Checks
{
    public class DbFaulted : IEgnCheck
    {
        public bool Check(EgnServer egnServer)
        {
            if (!egnServer.IgnitionController.IsLoggedIn())
                return false;
            var ignitionDbConnections = egnServer.IgnitionController.GetDatabaseConnections();
            if (ignitionDbConnections.Any(x => x.Status == "Faulted"))
            {
                const string message = "Local database connection in faulted state.";
                if (egnServer.Server.Contains("-ig-") || egnServer.Server.Contains("-peco-"))
                {
                    EsiLog.Error(egnServer, message);
                }
                else
                {
                    EsiLog.HardError(egnServer, message, "Internal");
                }
                return false;
            }
            EsiLog.Info(egnServer, "Ignition database connections are valid.");
            return true;
        }
    }
}