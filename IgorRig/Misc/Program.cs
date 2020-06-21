using System.ServiceProcess;

namespace IgorRig.Misc
{
    public static class Program
    {
        public static void Main()
        {
            var servicesToRun = new ServiceBase[]
            {
                new IgorService()
            };
            ServiceBase.Run(servicesToRun);
        }
    }
}