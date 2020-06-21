using Batch.FactoryStuff;
using Common.Models.Reporting;

namespace Batch.Checks.Pre
{
    public class EnterpriseCheck : IPreCheck
    {
        public bool Check()
        {
            var egnServer = new EgnServer {Server = "cal0-vp-ace01"};
            egnServer.Init();
            new Login().Check(egnServer);
            new License().Check(egnServer);
            new DbFaulted().Check(egnServer);
            return true;
        }
    }
}