using System;
using System.Linq;
using System.Management;
using System.Reflection;
using Common;
using Common.Database;
using Common.Models.Reporting;
using log4net;
using Telegram.Bot.Types;

namespace IgorEnterprise.Commands
{
    public class Reboot
    {
        private readonly Message _message;
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private EgnServer _egn;

        public Reboot(Message message)
        {
            _message = message;
            Log.Info("Received command to reboot.");
        }

        public void Execute()
        {
            try
            {
                GetRig();
                WmiReboot();
            }
            catch (Exception e)
            {
                Log.Error(e.Message);
                Log.Error(e.StackTrace);
                Singleton.Instance.SendMessage(_egn, $"Failed to execute reboot command. {e.Message}");
            }
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
            if(_egn == null)
                Log.Info($"Couldn't find rig for {rig}");
        }

        public void WmiReboot()
        {
            if(_egn == null) return;
            var options = new ConnectionOptions { EnablePrivileges = true };
            var scope = new ManagementScope("\\\\" + _egn.Server + "\\root\\CIMV2", options);
            scope.Connect();
            var query = new SelectQuery("Win32_OperatingSystem");
            var searcher = new ManagementObjectSearcher(scope, query);
            foreach (ManagementObject os in searcher.Get())
            {
                var inParams = os.GetMethodParameters("Win32Shutdown");
                //https://docs.microsoft.com/en-us/windows/desktop/cimwin32prov/win32shutdown-method-in-class-win32-operatingsystem
                inParams["Flags"] = 6; //Forced Reboot (2 + 4) - Shuts down and then restarts the computer.
                os.InvokeMethod("Win32Shutdown", inParams, null);
            }
            Singleton.Instance.SendMessage(_egn, "Forced shutdown & restart has started.");
        }
    }
}