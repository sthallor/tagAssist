using System;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Threading;
using IgorRig.Misc;
using log4net;

namespace IgorRig.Processes
{
    public class RoboCopyMain
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private static readonly TimeSpan RepeatCheckEvery = TimeSpan.FromMinutes(20);
        private static string _address = "cal0-vp-ace01";
        private const string UseIpAddress = "192.168.69.158";

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
                    if(!PingHost(_address))
                    {
                        _address = UseIpAddress;
                    }
                    ExecuteCommand($@"robocopy \\{_address}\share\IgorConfig\Common C:\Installs\IgorConfig\Common /s /Z /MIR /w:5 /r:2");
                    ExecuteCommand($@"robocopy \\{_address}\share\IgorConfig\{RigSingleton.Instance.EgnServer.RigNumber} C:\Installs\IgorConfig\{RigSingleton.Instance.EgnServer.RigNumber} /s /Z /MIR /w:5 /r:2");
                    ExecuteCommand($@"robocopy C:\Installs\IgorConfig\Output \\{_address}\share\IgorConfig\Output /mov /w:5 /r:2");
                    Thread.Sleep(RepeatCheckEvery);
                } while (true);
            }
            catch (Exception e)
            {
                RigSingleton.Instance.SendMessage($"Failed RoboCopyMain {RigSingleton.Instance.EgnServer.RigNumber}");
                Log.Error(e.Message);
                Log.Error(e.ToString());
            }
        }

        public static void ExecuteCommand(string command)
        {
            Log.Info($"Executing command {command}");
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

        public static bool PingHost(string nameOrAddress)
        {
            var pingable = false;
            Ping pinger = null;

            try
            {
                pinger = new Ping();
                var reply = pinger.Send(nameOrAddress);
                pingable = reply.Status == IPStatus.Success;
            }
            catch (PingException)
            {
                // Discard PingExceptions and return false;
            }
            finally
            {
                pinger?.Dispose();
            }

            return pingable;
        }
    }
}