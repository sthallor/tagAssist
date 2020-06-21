using System.ServiceProcess;

namespace IgorEnterprise
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
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
