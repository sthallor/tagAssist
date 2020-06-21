using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;
using IgorRig.Misc;
using log4net;

namespace IgorRig.Processes
{
    public class InstallInstaller
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private static readonly TimeSpan RepeatCheckEvery = TimeSpan.FromHours(1);
        private static string _address = "CAL0-VP-TFS01";
        private const string UseIpAddress = "192.168.69.34";
        private const string Destination = @"C:\Installs\ACE.IgorRigInstaller";

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
                    Directory.CreateDirectory(Destination);
                    GetIgorBuilds();
                    var installedVersion = GetIgorVersion();
                    decimal latestVersion;
                    try
                    {
                        var directory = Directory.GetDirectories(Destination) //All builds
                            .OrderByDescending(x => x.ToString()).First(); // Latest directory
                        latestVersion = decimal.Parse(directory.Split('\\').Last());
                    }
                    catch (Exception)
                    {
                        Thread.Sleep(RepeatCheckEvery);
                        continue;
                    }
                    if (installedVersion == latestVersion)
                    {
                        Thread.Sleep(RepeatCheckEvery);
                        continue;
                    }
                    Log.Info($"Rig running IgorRigInstaller version: {installedVersion}. Latest version is {latestVersion}.");
                    var serviceController = new ServiceController("IgorRigInstaller");
                    if (serviceController.Status != ServiceControllerStatus.Stopped)
                    {
                        Log.Info("Stopping IgorRigInstaller Service");
                        serviceController.Stop();
                        serviceController.WaitForStatus(ServiceControllerStatus.Stopped);
                        Thread.Sleep(TimeSpan.FromMinutes(4));
                    }
                    DoInstall(latestVersion);
                    Log.Info("Starting IgorRigInstaller Service");
                    serviceController.Start();
                    SetIgorVersion(latestVersion);
                    RigSingleton.Instance.SendMessage($"IgorRigInstaller service upgraded from {installedVersion} to {latestVersion}");
                    Thread.Sleep(RepeatCheckEvery);
                } while (true);
            }
            catch (Exception e)
            {
                Log.Error(e.Message);
                Log.Error(e.ToString());
                RigSingleton.Instance.SendMessage($"InstallInstaller process failed. {e.Message}");
            }
        }

        private static void DoInstall(decimal latestVersion)
        {
            var targetPath = @"C:\Program Files\IgorRigInstaller";
            var directoryInfo = new DirectoryInfo(targetPath);
            if (!directoryInfo.Exists) Directory.CreateDirectory(targetPath);
            var files = Directory.GetFiles($@"{Destination}\{latestVersion}");
            foreach (var file in files)
            {
                var fileName = Path.GetFileName(file);
                if (fileName == "IgorRigInstaller.exe.config")
                    continue;
                var destFile = Path.Combine(targetPath, fileName);
                File.Copy(file, destFile, true);
            }
        }

        private static decimal GetIgorVersion()
        {
            try
            {
                var readAllText = File.ReadAllText(@"C:\Program Files\IgorRigInstaller\Version.txt");
                var version = decimal.Parse(readAllText);
                return version;
            }
            catch (Exception) { return 0m; }
        }

        private static void SetIgorVersion(decimal version)
        {
            try
            {
                File.WriteAllText(@"C:\Program Files\IgorRigInstaller\Version.txt", version.ToString(CultureInfo.InvariantCulture));
            }
            catch (Exception) { /* Ignored */ }
        }

        private static void GetIgorBuilds()
        {
            if (!PingHost(_address))
            {
                _address = UseIpAddress;
            }
            var source = $@"\\{_address}\Drops\ACE.IgorRigInstaller";
            var roboCopyParms = @"/log:""C:\Program Files\Igor\robocopy.log"" /s /Z /MIR /w:5 /r:2";
            ExecuteCommand($@"robocopy {source} {Destination} {roboCopyParms}");
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
            Log.Info($"Finished command {command}");
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
    public static class Timeout
    {
        public static async Task<bool> ForAsync(Action operationWithTimeout, TimeSpan maxTime)
        {
            var timeoutTask = Task.Delay(maxTime);
            var completionSource = new TaskCompletionSource<Thread>();

            // This will await while any of both given tasks end.
            await Task.WhenAny
            (
                timeoutTask,
                Task.Factory.StartNew
                (
                    () =>
                    {
                        // This will let main thread access this thread and force a Thread.Abort
                        // if the operation must be canceled due to a timeout
                        completionSource.SetResult(Thread.CurrentThread);
                        operationWithTimeout();
                    }
                )
            );

            // Since timeoutTask was completed before wrapped File.Copy task you can 
            // consider that the operation timed out
            if (timeoutTask.Status == TaskStatus.RanToCompletion)
            {
                // Timed out!
                Thread thread = await completionSource.Task;
                thread.Abort();
                return false;
            }
            return true;
        }
    }
}
