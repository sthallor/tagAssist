using System.Linq;
using Batch.FactoryStuff;
using Common;
using Common.Database;

namespace Batch.Checks.Post
{
    public class FieldToInternal : IPostCheck
    {
        public bool Check()
        {
            var rigsInErrorState = Singleton.Instance.Bag.Where(x => x.Error == 3 && x.Category != "Internal").Select(x => x.Rig).Distinct().ToList();
            foreach (var rigInError in rigsInErrorState)
            {
                var warnAndErrorResults = Singleton.Instance.Bag.Where(x => x.Rig == rigInError && x.Error >= 2 && x.Category != "Internal").ToList();
                if (warnAndErrorResults.All(x=> x.Message.StartsWith("Historian last tag data")))
                {
                    foreach (var warnAndErrorResult in warnAndErrorResults)
                    {
                        warnAndErrorResult.Category = "Internal";
                        var server = ReportingDb.GetEgnServers().FirstOrDefault(x => x.RigNumber == rigInError);
                        EsiLog.HardError(server, "Problem with rig, but does not appear to be enough information for a field ticket", "Internal");
                    }
                }
            }
            return true;
        }
    }
}