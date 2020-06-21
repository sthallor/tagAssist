using System.Collections.Generic;
using System.Linq;
using Batch.FactoryStuff;
using Common;
using Common.Models.Reporting;

namespace Batch.Checks
{
    public class Module : IEgnCheck
    {
        public bool Check(EgnServer egnServer)
        {
            if (!egnServer.IgnitionController.IsLoggedIn())
                return false;

            var modules = egnServer.IgnitionController.GetModules();
            var goodStates = new List<string> { "Running", "Trial", "Loaded" };
            foreach (var module in modules.Where(x=> !goodStates.Contains(x.State)))
            {
                var message = $"Issue with module: {module.Name} {module.License} ({module.State})";
                EsiLog.Warn(egnServer, message, "Internal");
            }
            return true;
        }
    }
}