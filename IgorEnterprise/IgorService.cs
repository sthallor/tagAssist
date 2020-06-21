using System.Reflection;
using System.ServiceProcess;
using Common;
using IgorEnterprise.Process;
using log4net;

namespace IgorEnterprise
{
    public partial class IgorService : ServiceBase
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        public IgorService()
        {
            Log.Info("Igor is alive.");
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            Singleton.Instance.Bot.OnMessage += MessageRouter.MessageReceived;
            Singleton.Instance.Bot.StartReceiving();
            Log.Info("Bot is listening...");
            RoboCopyMain.Run();
            FailedTgMsg.Run();
        }

        protected override void OnStop()
        {
            Singleton.Instance.Bot.StopReceiving();
            Log.Info("Igor is dead.  For now...");
        }
    }
}
