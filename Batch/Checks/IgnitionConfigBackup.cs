using System;
using System.IO;
using Batch.FactoryStuff;
using Common;
using Common.Models.Reporting;

namespace Batch.Checks
{
    public class IgnitionConfigBackup : IEgnCheck
    {
        public bool Check(EgnServer egnServer)
        {
            try
            {
                var destination = $@"\\cal0-vp-ace01\e$\share\EdgeHistorianTroubleshooting\IgnitionConfigBackup\{egnServer.RigNumber}-{egnServer.Server}.db";
                var destinationFile = new FileInfo(destination);
                var source = $@"\\{egnServer.Server}\c$\Program Files\Inductive Automation\Ignition\data\db\config.idb";
                var sourceFile = new FileInfo(source);

                if(!destinationFile.Exists || sourceFile.LastWriteTime > destinationFile.LastWriteTime && destinationFile.LastWriteTime < DateTime.Now.Subtract(TimeSpan.FromHours(1)))
                {
                    EsiLog.Info(egnServer, "Copying ignition config.idb to config backup directory.");
                    File.Copy(source, destination + ".tmp", true);
                    if (destinationFile.Exists) File.Delete(destination);
                    File.Move(destination + ".tmp", destination);
                }
                return true;
            }
            catch (Exception)
            {
                EsiLog.Info(egnServer, "Failed to backup config.idb");
                return false;
            }
        }
    }
}