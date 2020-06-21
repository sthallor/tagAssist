using System.Reflection;
using System.ServiceProcess;
using log4net;

namespace IgorRigInstaller
{
    public partial class IgorRigInstallerService : ServiceBase
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        public IgorRigInstallerService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            Log.Info("IgorRigInstaller is alive!");
            Install.Run();
        }

        protected override void OnStop()
        {
            Log.Info("IgorRigInstaller is dead.  For now...");
        }
    }
}
