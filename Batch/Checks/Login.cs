using Batch.FactoryStuff;
using Common;
using Common.Models.Reporting;

namespace Batch.Checks
{
    public class Login : IEgnCheck
    {
        public bool Check(EgnServer egnServer)
        {
            if (!egnServer.IgnitionController.IsLoggedIn())
            {
                EsiLog.Error(egnServer, "Unable to login to ignition server.");
                return false;
            }
            EsiLog.Info(egnServer, "Sucessfully logged into Ignition gateway.");
            return true;
        }
    }
}