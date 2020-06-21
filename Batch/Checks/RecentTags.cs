using System;
using System.Linq;
using Batch.FactoryStuff;
using Common;
using Common.Extensions;
using Common.Models.Reporting;

namespace Batch.Checks
{
    public class RecentTags : IEgnCheck
    {
        private const int AttempRestartReDiscoverAfterHours = 1;

        public bool Check(EgnServer egnServer)
        {
            // Last tag data
            var rigTags = Singleton.Instance.GetTagData().Where(x => x.Rig == egnServer.RigNumber).ToList();
            foreach (var tagData in rigTags)
            {
                var message = $"Historian last tag data {tagData.Device} {TimeSpan.FromHours(tagData.GetHoursSinceLastTag()).ToPrettyFormat()}";
                if (tagData.GetHoursSinceLastTag() > Singleton.DelinquentAfterHours)
                    EsiLog.Error(egnServer, message);
                else
                    EsiLog.Info(egnServer, message);
            }

            var sinceLastTag = rigTags.Where(x => x.GetHoursSinceLastTag() > AttempRestartReDiscoverAfterHours).ToList();
            if (sinceLastTag.Any() && egnServer.IgnitionController.IsLoggedIn())
            {
                egnServer.IgnitionController.RestartModules();
                egnServer.IgnitionController.ReDiscoverEndpoints();
                return false;
            }
            return true;
        }
    }
}