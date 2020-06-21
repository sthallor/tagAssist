using System.Linq;
using System.Threading.Tasks;
using Batch.FactoryStuff;
using Common;
using Common.Database;

namespace Batch.Checks.Post
{
    public class Stacked : IPostCheck
    {
        public const int RecentData = 10;
        public bool Check()
        {
            var egnServers = ReportingDb.GetStackedEgnServers();

            Parallel.ForEach(egnServers, egnServer =>
            {
                egnServer.Init();
                var tagData = Singleton.Instance.GetTagData().FirstOrDefault(x => x.Rig == egnServer.RigNumber);
                if (egnServer.IgnitionController.IsLoggedIn() && tagData != null && tagData.GetHoursSinceLastEdr() < RecentData && tagData.GetHoursSinceLastTag() < RecentData)
                {
                    EsiLog.HardError(egnServer, "This rig is reported stacked, but ignition gateway is currently online.", "Internal");
                }
            });
            return true;
        }
    }
}