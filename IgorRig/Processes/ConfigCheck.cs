using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using IgorRig.Misc;
using log4net;

namespace IgorRig.Processes
{
    public class ConfigCheck
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        public static void Run()
        {
            var thread = new Thread(Check);
            thread.Start();
        }
        public static void Check()
        {
            try
            {
                var historianConfig = RigSingleton.Instance.GetHistorianConfig();
                RigSingleton.Instance.EgnServer.Init();
                var opcServers = RigSingleton.Instance.EgnServer.IgnitionController.GetOpcServers();
                foreach (var opcServer in opcServers.Where(x => x.Name != "Ignition OPC-UA Server"))
                {
                    if (historianConfig.OpcUaServers == null) return;
                    if (historianConfig.OpcUaServers.All(x => x.Host != opcServer.Description))
                    {
                        Log.Error("OpcUa Config doesn't match Historian.json file.");
                        RigSingleton.Instance.SendMessage("OpcUa Config doesn't match Historian.json file.");
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error(e);
                RigSingleton.Instance.SendMessage($"ConfigCheck Failed: {e.Message}");
            }
        }
    }
}