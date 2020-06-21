using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using Common;
using Common.Database;
using Common.Models.Reporting;
using log4net;
using Telegram.Bot.Types;

namespace IgorEnterprise.Commands
{
    public class Restart
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private readonly Message _message;
        private EgnServer _egn;

        public Restart(Message message)
        {
            _message = message;
            Log.Info("Received command to restart.");
        }

        public void Execute()
        {
            try
            {
                GetRig();
                RestartServices();
            }
            catch (Exception e)
            {
                Log.Error(e.Message);
                Log.Error(e.StackTrace);
                Singleton.Instance.SendMessage(_egn, $"Failed to execute restart command. {e.Message}");
            }
        }

        private void RestartServices()
        {
            _egn.ServiceController.Stop("Igor");
            _egn.ServiceController.Stop("Ignition");
            _egn.ServiceController.Stop("MySQL");
            Thread.Sleep(TimeSpan.FromMinutes(3));
            _egn.ServiceController.Start("MySQL");
            _egn.ServiceController.Start("Ignition");
            _egn.ServiceController.Start("Igor");
            Singleton.Instance.SendMessage(_egn,"Successfully restarted Ignition/MySQL/Igor services.");
        }

        private void GetRig()
        {
            var rig = "";
            if (_message.ReplyToMessage != null)
            {
                rig = _message.ReplyToMessage.EntityValues.FirstOrDefault();
            }

            if (_message.ReplyToMessage == null)
            {
                rig = _message.Text.Split(' ')[1];
            }

            if (!string.IsNullOrEmpty(rig))
            {
                _egn = ReportingDb.GetEgnServers().FirstOrDefault(x => x.RigNumber == rig);
            }
            if (_egn == null)
                Log.Info($"Couldn't find rig for {rig}");
        }
    }
}