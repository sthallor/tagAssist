using System.Linq;
using Batch.FactoryStuff;
using Common;
using Common.Models.Reporting;

namespace Batch.Checks.Post
{
    public class DupeLicense : IPostCheck
    {
        public bool Check()
        {
            var dupeLicense = Singleton.Instance.LicenseKeys.GroupBy(x => x).Where(group => group.Count() > 1).Select(group => group.Key).ToList();
            foreach (var license in dupeLicense)
            {
                EsiLog.HardError(new EgnServer { RigNumber = "", Server = "" }, $"{license} license is used multiple times.", "Internal");
            }
            return true;
        }
    }
}