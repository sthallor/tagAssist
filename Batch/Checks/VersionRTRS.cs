using System;
using Batch.FactoryStuff;
using Common;
using Common.Database;
using Common.Models.Reporting;

namespace Batch.Checks
{
    public class VersionRTRS : IEgnCheck
    {
        public bool Check(EgnServer egnServer)
        {
            try
            {
                if (!egnServer.IgnitionController.IsLoggedIn()) return true;
                if (!egnServer.GetHistorianConfig().HistEnvironment.RealTimeRigState) return true;

                using (var db = new IgorDb())
                {
                    var rigStateVersion = db.RtrsVersion.Find(egnServer.RigNumber);
                    if (rigStateVersion == null) return false;
                    if (rigStateVersion.Version != Singleton.Instance.LatestVersion ||
                        rigStateVersion.MD5 != Singleton.Instance.Md5ForFolder)
                    {
                        EsiLog.Warn(egnServer, $"Old version of RTRS detected {rigStateVersion.Version} {rigStateVersion.MD5}", "Internal");
                    }
                }
            }
            catch (Exception) { /* ignored */ }
            return true;
        }
    }
}