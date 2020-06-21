using Batch.FactoryStuff;
using Common.Models.Reporting;

namespace Batch.Checks
{
    public class RecentEdr : IEgnCheck
    {
        public bool Check(EgnServer egnServer)
        {
            return true;
        }
    }
}