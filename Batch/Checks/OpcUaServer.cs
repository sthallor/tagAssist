using System.Linq;
using Batch.FactoryStuff;
using Common;
using Common.Models.Reporting;

namespace Batch.Checks
{
    public class OpcUaServer : IEgnCheck
    {
        public bool Check(EgnServer egnServer)
        {
            if (!egnServer.IgnitionController.IsLoggedIn())
                return false;
            // OPC-UA Servers
            var opcServers = egnServer.IgnitionController.GetOpcServers();
            foreach (var opcServer in opcServers)
            {
                var message = $"{opcServer.Status}: {opcServer.Description} ({opcServer.Name})";
                if (opcServer.Status == "Connected")
                    EsiLog.Info(egnServer, message);
                else
                {
                    EsiLog.Warn(egnServer, message, "");
                }
            }
            if (opcServers.Any(x => x.Status != "Connected"))
            {
                egnServer.IgnitionController.RestartModules();
                if (egnServer.IgnitionController.GetOpcServers().Where(x => x.Status != "Connected").ToList().Any())
                {
                    EsiLog.Error(egnServer, "Restarting faulted modules did not resolve the connection");
                    return false;
                }
            }

            foreach (var opcServer in opcServers.Where(x => x.Description.Contains("-peco-")))
            {
                var server = new EgnServer { Server = opcServer.Description, RigNumber = egnServer.RigNumber };
                server.Init();
                new DbFaulted().Check(server);
                new License().Check(server);
            }

            foreach (var opcServer in opcServers.Where(x=> x.Description.Contains("-ig-")))
            {
                var server = new EgnServer{Server = opcServer.Description, RigNumber = egnServer.RigNumber};
                server.Init();
                new DbFaulted().Check(server);
                new License().Check(server);
            }
            return true;
        }
    }
}