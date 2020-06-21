using System;
using System.IO;
using System.Linq;
using Batch.FactoryStuff;
using Common;
using Common.Models.Reporting;

namespace Batch.Checks.Pre
{
    public class VersionRTRS : IPreCheck
    {
        public bool Check()
        {
            try
            {
                var egnServer = new EgnServer { Server = "cal0-vp-ace01" };
                Singleton.Instance.SourceDir = Directory.GetDirectories(@"\\cal0-vp-ace01\e$\Analytics\Rcode\", "*_master")
                    .OrderByDescending(x => x).FirstOrDefault();
                Singleton.Instance.LatestVersion = Singleton.Instance.SourceDir?.Split('\\').Last();
                Singleton.Instance.Md5ForFolder = Utility.CreateMd5ForRTRS(Singleton.Instance.SourceDir);
                EsiLog.Info(egnServer, $"Latest realtime rig state version: {Singleton.Instance.LatestVersion} {Singleton.Instance.Md5ForFolder}");
            }
            catch (Exception) { /* ignored */ }
            return true;
        }
    }
}