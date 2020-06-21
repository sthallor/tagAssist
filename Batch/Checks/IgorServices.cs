using System;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Threading;
using Batch.FactoryStuff;
using Common;
using Common.Models.Reporting;

namespace Batch.Checks
{
    public class IgorServices : IEgnCheck
    {
        private const string DropFolder = @"\\CAL0-VP-TFS01\Drops\ACE.IgorRigService";
        public bool Check(EgnServer egnServer)
        {
            try
            {
                var igorRigInstallerService = new ServiceController("IgorRigInstaller", egnServer.Server);
                if (igorRigInstallerService.Status != ServiceControllerStatus.Running)
                {
                    EsiLog.HardError(egnServer, "IgorRigInstaller service not running", "Internal");
                }
                var igorRigService = new ServiceController("Igor", egnServer.Server);
                if (igorRigService.Status != ServiceControllerStatus.Running)
                {
                    EsiLog.HardError(egnServer, "IgorRig service not running", "Internal");
                }

                var readAllText = File.ReadAllText($@"\\{egnServer.Server}\c$\Program Files\Igor\Version.txt");
                var directory = Directory.GetDirectories(DropFolder) //All builds
                    .OrderByDescending(x => x.ToString()).First(); // Latest directory
                var igorRigVersion = decimal.Parse(readAllText);
                var latestVersion = decimal.Parse(directory.Split('\\').Last());
                if (igorRigVersion != latestVersion)
                {
                    EsiLog.HardError(egnServer, $"IgorRig service version is {igorRigVersion}. Available version is {latestVersion}.", "Internal");
                    igorRigInstallerService.Stop();
                    igorRigInstallerService.WaitForStatus(ServiceControllerStatus.Stopped);
                    Thread.Sleep(TimeSpan.FromMinutes(1));
                    igorRigInstallerService.Start();
                }
                return true;
            }
            catch (Exception e)
            {
                if (e.Message.StartsWith("Service IgorServices was not found on computer"))
                {
                    EsiLog.HardError(egnServer, "IgorRigInstaller service not installed", "Internal");
                }
                if (e.Message.StartsWith("Service IgorRig was not found on computer"))
                {
                    EsiLog.HardError(egnServer, "IgorRig service not installed", "Internal");
                }
            }
            return false;
        }
    }
}