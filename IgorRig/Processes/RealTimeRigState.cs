using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Reflection;
using System.Threading;
using IgorRig.Misc;
using log4net;

namespace IgorRig.Processes
{
    public class RealTimeRigState
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        
        public static void Run()
        {
            if (!RigSingleton.Instance.GetHistorianConfig().HistEnvironment.RealTimeRigState) return;
            Log.Info("Starting real time rig state classifier process...");
            new Thread(() => ExecuteCommand("RTclassify.bat")).Start();
        }

        public static void Stop()
        {
            if (!RigSingleton.Instance.GetHistorianConfig().HistEnvironment.RealTimeRigState) return;
            Log.Info("Stopping real time rig state classifier process...!");
            Terminate("Rscript.exe", RigSingleton.Instance.EgnServer.Server);
        }

        private static void ExecuteCommand(string command)
        {
            try
            {
                do
                {
                    var startTime = DateTime.Now;
                    var sourceDir = Directory.GetDirectories(@"C:\Analytics\Rcode\", "*_master")
                        .OrderByDescending(x => x).FirstOrDefault();
                    var latestVersion = sourceDir?.Split('\\').Last();
                    var directory = $@"C:\Analytics\Rcode\{latestVersion}\batch\prod\classify";
                    Log.Info($"Starting {command} in {directory}");
                    var process = new Process
                    {
                        StartInfo =
                        {
                            FileName = "cmd.exe",
                            Arguments = "/c " + command,
                            WorkingDirectory = directory,
                            CreateNoWindow = true,
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true
                        }
                    };
                    process.Start();
                    process.WaitForExit();
                    Thread.Sleep(TimeSpan.FromSeconds(15));
                    var lastWriteTime = File.GetLastWriteTime(@"C:\Program Files\Igor\Version.txt");
                    var tsServiceUpdate = DateTime.Now - lastWriteTime;
                    var tsRunTime = DateTime.Now - startTime;
                    if (tsRunTime.TotalMinutes > 15) continue; 
                    if (tsServiceUpdate.TotalMinutes < 60) continue;
                    RigSingleton.Instance.SendMessage("RealTimeRigState process ended before required minimum elapsed time of 15 minutes.");
                    Thread.Sleep(TimeSpan.FromMinutes(15)); // Just to prevent getting messages flooding.
                } while (true);
            }
            catch (Exception e)
            {
                Log.Error(e.Message);
                Log.Error(e.ToString());
                RigSingleton.Instance.SendMessage($"Unhandled exception in rig state classification process. {e.Message}");
            }
        }

        public static void Terminate(string proc, string server)
        {
            var scope = new ManagementScope($@"\\{server}\root\cimv2");
            var query = new SelectQuery($"select * from Win32_process where name = '{proc}'");
            using (var searcher = new ManagementObjectSearcher(scope, query))
            {
                foreach (ManagementObject process in searcher.Get())
                {
                    process.InvokeMethod("Terminate", null);
                }
            }
        }
    }
}