using System;
using System.Data.Entity.Migrations;
using System.Linq;
using System.Reflection;
using System.Threading;
using Common.Database;
using Common.Models.Igor;
using Common.Models.Reporting;
using log4net;
using Newtonsoft.Json;
using Telegram.Bot.Types;

namespace IgorEnterprise.Commands
{
    public class Reset
    {
        private readonly Message _message;
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private static readonly TimeSpan TimespantoWatch = TimeSpan.FromDays(7);
        private readonly DateTime _timeToStopWatching = DateTime.Now + TimespantoWatch;
        private EgnServer _egn;
        private string _trialModules;

        public Reset(Message message)
        {
            Log.Info("Received command message to reset.");
            _message = message;
        }

        public Reset(ResetTrial resetTrial)
        {
            try
            {
                Log.Info($"Starting service found db entry for {resetTrial.Rig} {resetTrial.Server}");
                _egn = IgnitionDataConnector.GetEgnServers().FirstOrDefault(x => x.RigNumber == resetTrial.Rig) ??
                       new EgnServer { RigNumber = resetTrial.Rig, Server = resetTrial.Server };
            }
            catch (Exception e)
            {
                Log.Error(e.ToString());
                try
                {
                    Singleton.Instance.SendMessage(_egn, $"Failed to execute reset command. {e.Message}");
                }
                catch (Exception) { /*Ignored*/ }
            }
        }

        public void Execute()
        {
            do
            {
                try
                {
                    GetRig();
                    if (string.IsNullOrEmpty(_trialModules))
                    {
                        _egn.Init();
                        _trialModules = string.Join(",", _egn.IgnitionController.GetModules().Where(x => x.License == "Trial").Select(x => x.Name));
                    }
                    ResetTrialForTimeSpan();
                }
                catch (Exception e)
                {
                    if (e.Message != "Cannot perform runtime binding on a null reference")
                    {
                        Log.Debug(e.Message);
                        Log.Debug(e.StackTrace);
                        Log.Debug(_egn);
                        Log.Debug(_egn.RigNumber);
                        Log.Debug(_egn.Server);
                        Log.Error($"{_egn.Server}({_egn.RigNumber}) Failed to execute reset command. {e.Message}");
                        Singleton.Instance.SendMessage(_egn, $"Failed to execute reset command. {e.Message}");
                    }
                    Thread.Sleep(TimeSpan.FromHours(1));
                }
            } while (DateTime.Now < _timeToStopWatching);
        }

        public void ResetTrialForTimeSpan()
        {
            if (!string.IsNullOrEmpty(_trialModules))
            {
                Singleton.Instance.SendMessage(_egn, $"⏱️ Resetting trial on {_trialModules}. You have until {_timeToStopWatching}.");
            }
            do
            {
                _egn.Init();
                dynamic trialInfo = JsonConvert.DeserializeObject(_egn.IgnitionController.GetHtmlContent("/main/data/status/trial"));
                if (trialInfo.trialState == "NoneInDemo")
                {
                    Singleton.Instance.SendMessage(_egn, $"No modules in trial state. Remove entry from ResetTrial db.");
                }
                if (trialInfo.remainingDisplay == "0:00:00")
                {
                    Log.Info($"{_egn.Server}({_egn.RigNumber}) Resetting trial...");
                    var htmlContent = _egn.IgnitionController.PutHtmlContent("/main/data/status/trial");
                    Log.Info($"{_egn.Server}({_egn.RigNumber}) {htmlContent}");
                    Thread.Sleep(TimeSpan.FromMinutes(15)); // Sometimes resetting trial doesn't work and this loop would hammer hard the Ignition gateway.
                }

                trialInfo = JsonConvert.DeserializeObject(_egn.IgnitionController.GetHtmlContent("/main/data/status/trial"));
                Thread.Sleep(TimeSpan.Parse(trialInfo.remainingDisplay.ToString()));
            } while (DateTime.Now < _timeToStopWatching);
        }

        private void GetRig()
        {
            if (_egn != null) return;
            var rig = "";
            if (_message.ReplyToMessage != null) rig = _message.ReplyToMessage.EntityValues.FirstOrDefault();

            if (_message.ReplyToMessage == null) rig = _message.Text.Split(' ')[1];

            if (!string.IsNullOrEmpty(rig))
                _egn = IgnitionDataConnector.GetEgnServers().FirstOrDefault(x => x.RigNumber == rig);

            if (_egn == null)
            {
                Log.Info($"Couldn't find rig for {rig}");
                return;
            }

            using (var db = new IgorDb())
            {
                try
                {
                    db.ResetTrial.AddOrUpdate(new ResetTrial { Rig = _egn.RigNumber, Server = _egn.Server });
                    db.SaveChanges();
                }
                catch (Exception) { /* ignored */ }
            }
        }
    }
}