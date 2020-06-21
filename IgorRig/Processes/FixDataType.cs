using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using Common.Database;
using IgorRig.Misc;
using log4net;

namespace IgorRig.Processes
{
    public class FixDataType
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        public static void Run()
        {
            var thread = new Thread(Check);
            thread.Start();
        }

        public static void Check()
        {
            // Want to make sure that the OPC tag data has been populated by TagCompare process.
            // Probably should change this to poll at some interval.
            Thread.Sleep(TimeSpan.FromMinutes(5));
            try
            {
                if (RigSingleton.Instance.GetHistorianConfig().OpcUaServers.All(x => x.OpcTags == null)) return;
                var count = 0;
                foreach (var opcUaServer in RigSingleton.Instance.GetHistorianConfig().OpcUaServers)
                {
                    foreach (var historyProvider in opcUaServer.GetHistoryProviders())
                    {
                        var tagData = ConfigDb.GetTagData(opcUaServer, historyProvider);
                        foreach (var tag in tagData)
                        {
                            var refTag = opcUaServer.OpcTags.FirstOrDefault(x => x.NodeId == tag.OpcItemPath);
                            if (refTag == null || tag.DataType == refTag.DataType) continue;

                            count += 1;
                            Log.Error($"{tag.OpcItemPath} Has datatype of {tag.DataType} Should be {refTag.DataType}");
                            ConfigDb.ExecuteNonQuery($@"update sqltag set datatype = '{refTag.DataType}' where sqltag_id = {tag.SqlTagId}");
                        }
                    }
                }
                Log.Info($"Fixed {count} tags with the wrong datatype.");
                if (count > 0)
                {
                    RigSingleton.Instance.SendMessage($"Fixed {count} tags with the wrong datatype.");
                    RestartService();
                }
            }
            catch (Exception e)
            {
                Log.Error(e.Message);
                Log.Error(e.ToString());
                RigSingleton.Instance.SendMessage($"Failed FixDataType {e.Message}");
            }
        }

        private static void RestartService()
        {
            Log.Info("Stopping Ignition service...");
            RigSingleton.Instance.EgnServer.ServiceController.Stop("Ignition");
            Thread.Sleep(TimeSpan.FromMinutes(2));
            Log.Info("Starting Ignition service...");
            RigSingleton.Instance.EgnServer.ServiceController.Start("Ignition");
        }
    }
}