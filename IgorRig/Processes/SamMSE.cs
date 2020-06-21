using System;
using System.Diagnostics;
using System.Management;
using System.Reflection;
using System.Threading;
using IgorRig.Misc;
using log4net;

namespace IgorRig.Processes
{
    public class SamMSE
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        
        public static void Run()
        {
            if (!RigSingleton.Instance.GetHistorianConfig().HistEnvironment.RealTimeMSE) return;
            Log.Info("Starting SamMSE process...");
            new Thread(ExecuteCommand).Start();
        }

        public static void Stop()
        {
            if (!RigSingleton.Instance.GetHistorianConfig().HistEnvironment.RealTimeMSE) return;
            Log.Info("Stopping SamMSE process...");
            Terminate("Rscript.exe", RigSingleton.Instance.EgnServer.Server);
        }

        private static void ExecuteCommand()
        {
            try
            {
                TimeSpan timeSpan;
                do
                {
                    var startTime = DateTime.Now;
                    var directory = @"C:\Analytics";
                    Log.Info($"Starting sam.bat in {directory}");
                    var process = new Process
                    {
                        StartInfo =
                        {
                            FileName = "cmd.exe",
                            Arguments = "/c sam.bat",
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
                RigSingleton.Instance.SendMessage("SamMSE process ended before required minimum elapsed time of 15 minutes.");
            }
            catch (Exception e)
            {
                Log.Error(e.Message);
                Log.Error(e.ToString());
                RigSingleton.Instance.SendMessage($"Unhandled exception in SamMSE process. {e.Message}");
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