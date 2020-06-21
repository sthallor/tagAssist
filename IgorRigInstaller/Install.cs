using System;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection;
using System.ServiceProcess;
using System.Threading;
using log4net;

namespace IgorRigInstaller
{
    public class Install
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private static readonly TimeSpan RepeatCheckEvery = TimeSpan.FromHours(1);
        private static string _address = "CAL0-VP-TFS01";
        private const string UseIpAddress = "192.168.69.34";
        private const string Destination = @"C:\Installs\ACE.IgorRigService";
        private static readonly string Server = ConfigurationManager.AppSettings["Server"];
        private static readonly string Rig = ConfigurationManager.AppSettings["Rig"];

        public static void Run()
        {
            if (Server == string.Empty || Rig == string.Empty)
            {
                Log.Error("Missing Server or Rig setting in app.config.");
                return;
            }

            var thread = new Thread(Check);
            thread.Start();
        }

        private static void Check()
        {
            do
            {
                try
                {
                    Directory.CreateDirectory(Destination);
                    Directory.CreateDirectory(@"C:\Installs\IgorConfig\Output");
                    GetIgorBuilds();
                    var installedVersion = GetIgorVersion();
                    var directory = Directory.GetDirectories(Destination) //All builds
                        .OrderByDescending(x => x.ToString()).First(); // Latest directory
                    var latestVersion = decimal.Parse(directory.Split('\\').Last());
                    if (installedVersion == latestVersion)
                    {
                        Thread.Sleep(RepeatCheckEvery);
                        continue;
                    }

                    Log.Info($"Rig running Igor version: {installedVersion}. Latest version is {latestVersion}.");
                    var serviceController = new ServiceController("Igor");
                    if (serviceController.Status != ServiceControllerStatus.Stopped)
                    {
                        Log.Info("Stopping Igor Service");
                        serviceController.Stop();
                        serviceController.WaitForStatus(ServiceControllerStatus.Stopped);
                        Thread.Sleep(TimeSpan.FromMinutes(4));
                    }

                    DoInstall(latestVersion);
                    Log.Info("Starting Igor Service");
                    serviceController.Start();
                    SetIgorVersion(latestVersion);
                    var message = $"Igor service upgraded from {installedVersion} to {latestVersion}";
                    File.WriteAllText($@"C:\Installs\IgorConfig\Output\TgMsg{Rig}_{DateTime.Now:yyMMdd.hhmmss}.txt",
                        message);
                    Thread.Sleep(RepeatCheckEvery);
                }
                catch (Exception e)
                {
                    Log.Error(e);
                    Thread.Sleep(TimeSpan.FromMinutes(5));
                }
            } while (true);
        }

        private static void DoInstall(decimal latestVersion)
        {
            var targetPath = @"C:\Program Files\Igor";
            var directoryInfo = new DirectoryInfo(targetPath);
            if (!directoryInfo.Exists) Directory.CreateDirectory(targetPath);
            Directory.CreateDirectory(targetPath + @"\x64");
            Directory.CreateDirectory(targetPath + @"\x86");
            File.Copy($@"{Destination}\{latestVersion}\x64\SQLite.Interop.dll",
                @"C:\Program Files\Igor\x64\SQLite.Interop.dll", true);
            File.Copy($@"{Destination}\{latestVersion}\x86\SQLite.Interop.dll",
                @"C:\Program Files\Igor\x86\SQLite.Interop.dll", true);
            var files = Directory.GetFiles($@"{Destination}\{latestVersion}");
            foreach (var file in files)
            {
                var fileName = Path.GetFileName(file);
                var destFile = Path.Combine(targetPath, fileName);
                File.Copy(file, destFile, true);
            }

            var readAllLines = File.ReadAllLines(@"C:\Program Files\Igor\IgorRig.exe.config");
            readAllLines = readAllLines.Select(str =>
            {
                if (str.StartsWith(@"    <add key=""Server"" value="""))
                    str = $@"     <add key=""Server"" value=""{Server}"" />";
                return str;
            }).ToArray();
            readAllLines = readAllLines.Select(str =>
            {
                if (str.StartsWith(@"    <add key=""Rig"" value="""))
                    str = $@"     <add key=""Rig"" value=""{Rig}"" />";
                return str;
            }).ToArray();
            File.WriteAllLines(@"C:\Program Files\Igor\IgorRig.exe.config", readAllLines);
        }

        private static decimal GetIgorVersion()
        {
            try
            {
                var readAllText = File.ReadAllText(@"C:\Program Files\Igor\Version.txt");
                var version = decimal.Parse(readAllText);
                return version;
            }
            catch (Exception)
            {
                return 0m;
            }
        }

        private static void SetIgorVersion(decimal version)
        {
            try
            {
                File.WriteAllText(@"C:\Program Files\Igor\Version.txt", version.ToString(CultureInfo.InvariantCulture));
            }
            catch (Exception)
            {
                /* Ignored */
            }
        }

        private static void GetIgorBuilds()
        {
            if (!PingHost(_address)) _address = UseIpAddress;
            var source = $@"\\{_address}\Drops\ACE.IgorRigService";
            var roboCopyParms = @"/log:""C:\Program Files\IgorRigInstaller\robocopy.log"" /s /Z /MIR /w:5 /r:2";
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
}