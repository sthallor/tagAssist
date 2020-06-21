using System.Reflection;
using System.ServiceProcess;
using IgorRig.Processes;
using log4net;

namespace IgorRig
{
    public partial class IgorService : ServiceBase
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public IgorService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            Log.Info("Igor is alive!");
            ResetTrial.Run();
            IgnitionConfigSetter.Run();
            TagCompare.Run();
            InstallCheck.Run();
            RealTimeRigState.Run();
            SamMSE.Run();
            AlarmIGBT.Run();
            DataGap.Run();
            DiskSpace.Run();
            CleanTempFiles.Run();
            RoboCopyMain.Run();
            SqlBomberConfigDb.Run();
            ClockDrift.Run();
            TagThrottle.Run();
            ConfigCheck.Run();
            FixDataType.Run();
            InstallInstaller.Run();
        }

        protected override void OnStop()
        {
            RealTimeRigState.Stop();
            SamMSE.Stop();
            AlarmIGBT.Stop();
            Log.Info("Igor is dead.  For now...");
        }
    }
}