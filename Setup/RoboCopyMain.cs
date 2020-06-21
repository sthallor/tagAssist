using System;
using System.Configuration;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using log4net;

namespace Setup
{
    public class RoboCopyMain
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private static readonly TimeSpan RepeatCheckEvery = TimeSpan.FromHours(4);
        public static readonly string InstallDir = ConfigurationManager.AppSettings["InstallDir"];
        public static readonly string SourceDir = ConfigurationManager.AppSettings["SourceDir"];
        public static void Run()
        {
            var thread = new Thread(Check);
            thread.Start();
        }

        private static void Check()
        {
            try
            {
                do
                {
                    ExecuteCommand($@"robocopy {SourceDir} {InstallDir} /s /Z");
                    Thread.Sleep(RepeatCheckEvery);
                } while (true);
            }
            catch (Exception e)
            {
                Log.Error(e.Message);
                Log.Error(e.ToString());
            }
        }

        public static void ExecuteCommand(string command)
        {
            Console.WriteLine($"Executing command: {command}");
            var process = new Process
            {
                StartInfo =
                {
                    UseShellExecute = false,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    FileName = "cmd.exe",
                    Arguments = $"/C {command}"
                }
            };
            process.Start();
            process.WaitForExit();
        }
    }
}