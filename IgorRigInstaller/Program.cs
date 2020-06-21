using System.ServiceProcess;

namespace IgorRigInstaller
{
    static class Program
    {
        static void Main()
        {
            var servicesToRun = new ServiceBase[]
            {
                new IgorRigInstallerService()
            };
            ServiceBase.Run(servicesToRun);
        }
    }
}
