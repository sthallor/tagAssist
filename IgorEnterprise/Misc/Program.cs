using System.ServiceProcess;

namespace IgorEnterprise.Misc
{
    static class Program
    {
        static void Main()
        {
            var servicesToRun = new ServiceBase[]
            {
                new IgorService()
            };
            ServiceBase.Run(servicesToRun);
        }
    }
}
