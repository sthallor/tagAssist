using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using log4net;

namespace IgorRig.Processes
{
    public class InstallCheck
    {
        public static readonly string InstallDir = @"C:\Installs\";
        public static readonly string SourceDir = @"\\cal0-vp-ace01\share\IgorConfig\Installs\deployment-7.9.9";
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        public static void Run()
        {
            var thread = new Thread(Check);
            thread.Start();
        }
        public static void Check()
        {
            RoboCopy();
            InstallJava();
            InstallIgnition();
            //InstallMariaDb();
        }
        private static void InstallMariaDb()
        {
            var dataDir = @"DATADIR=C:\MySqlData";
            try
            {
                var drive = DriveInfo.GetDrives().Where(x => x.IsReady && x.DriveType == DriveType.Fixed &&
                            (decimal)x.TotalFreeSpace / (decimal)x.TotalSize * 100 > 25 &&  // With 25% or more free space
                            x.TotalFreeSpace / 1024m / 1024m / 1024m > 50). // With 25GB of free space
                            OrderByDescending(x=> x.Name).FirstOrDefault(); // Get an alternate drive before defaulting to C
                if (drive != null)
                {
                    dataDir = $@"DATADIR={drive.Name}MySqlData";
                }
            }
            catch (Exception) { /* Ignored */ }
            if (!Directory.Exists(@"C:\Program Files\MariaDB 10.1"))
            {
                var command = $@"msiexec.exe /i {InstallDir}mariadb-10.1.9-winx64.msi {dataDir} SERVICENAME=MySQL DEFAULTUSER=root PASSWORD=ensignDatabase /passive";
                Log.Warn($"Missing MariaDb. Executing command {command}");
                ExecuteCommand(command);
                Log.Info("Finished MariaDb install.");
            }
        }

        private static void InstallIgnition()
        {
            if (!Directory.Exists(@"C:\Program Files\Inductive Automation\Ignition\webserver"))
            {
                var command = $@"{InstallDir}Ignition-7.9.9-windows-x64-installer.exe --mode unattended --unattendedmodeui none";
                Log.Warn($"Missing Ignition. Executing command {command}");
                ExecuteCommand(command);
                ExecuteCommand("net start Ignition");
                Log.Info("Finished Ignition install.");
            }
        }

        private static void InstallJava()
        {
            if (!Directory.Exists(@"C:\Program Files\Java\jre1.8.0_251"))
            {
                var command = $@"{InstallDir}jre-8u251-windows-x64.exe /s";
                Log.Warn($"Missing Java Runtime. Executing command {command}");
                ExecuteCommand(command);
                Log.Info("Finished Java install.");
            }
        }

        private static void RoboCopy()
        {
            if (!Directory.Exists(@"C:\Program Files\MariaDB 10.4") ||
                !Directory.Exists(@"C:\Program Files\Inductive Automation\Ignition\webserver") ||
                !Directory.Exists(@"C:\Program Files\Java\jre1.8.0_251"))
            {
                var command = $@"robocopy {SourceDir} {InstallDir} /log:""C:\Program Files\Igor\robocopy.log"" /s /Z /w:5 /r:2";
                Log.Warn($"Detected missing dependencies. Executing command {command}");
                ExecuteCommand(command);
            }
            else
            {
                Log.Info("Found all software dependencies.");
            }
        }

        public static void ExecuteCommand(string command)
        {
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