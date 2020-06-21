using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Common;
using Common.Database;
using log4net;

namespace DeployRealTimeRigState
{
    public static class Program
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private static readonly DateTime Start = DateTime.Now;
        public static string SourceDir;
        public static string LatestVersion;
        public static string Md5ForFolder;
        private static void Main()
        {
            SourceDir = Directory.GetDirectories(@"\\cal0-vp-ace01\e$\Analytics\Rcode\", "*_master")
                .OrderByDescending(x => x).FirstOrDefault();
            LatestVersion = SourceDir?.Split('\\').Last();
            Md5ForFolder = Utility.CreateMd5ForRTRS(SourceDir);
            Log.Info("Running RigStateClassification Deployment...");
            var egnServers = ReportingDb.GetEgnServers();
            Log.Info($"Processing {egnServers.Count} EgnServers.");
            foreach (var egnServer in egnServers.OrderBy(x => x.RigNumber))
            {
                new Deployer(egnServer).Execute();
            }
            Log.Info("Duration:" + (DateTime.Now - Start));
        }
    }
}