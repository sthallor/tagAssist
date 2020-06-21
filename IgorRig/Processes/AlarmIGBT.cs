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
    public class AlarmIGBT
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        
        public static void Run()
        {
            if (!RigSingleton.Instance.GetHistorianConfig().HistEnvironment.RealTimeIGBT) return;
            Log.Info("Starting real time IGBT alarm predictor...");
            new Thread(() => ExecuteCommand("RTpredict.bat")).Start();
        }

        public static void Stop()
        {
            if (!RigSingleton.Instance.GetHistorianConfig().HistEnvironment.RealTimeIGBT) return;
            Log.Info("Stopping real time IGBT alarm predictor...");
            Terminate("Rscript.exe", RigSingleton.Instance.EgnServer.Server);
        }

        private static void ExecuteCommand(string command)
        {
            try
            {
                TimeSpan timeSpan;
                do
                {
                    var startTime = DateTime.Now;
                    var sourceDir = Directory.GetDirectories(@"C:\Analytics\Rcode\", "*_master")
                        .OrderByDescending(x => x).FirstOrDefault();
                    var latestVersion = sourceDir?.Split('\\').Last();
                    var directory = $@"C:\Analytics\Rcode\{latestVersion}\batch\prod\predict";
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
                    timeSpan = DateTime.Now - startTime;
                } while (timeSpan.TotalMinutes > 15);
                RigSingleton.Instance.SendMessage("IGBT alarm predict ended before required minimum elapsed time of 15 minutes.");
            }
            catch (Exception e)
            {
                Log.Error(e.Message);
                Log.Error(e.ToString());
                RigSingleton.Instance.SendMessage($"Unhandled exception in IGBT alarm prediction. {e.Message}");
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