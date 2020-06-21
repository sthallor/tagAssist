using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using IgorRig.Misc;
using log4net;
using Newtonsoft.Json;

namespace IgorRig.Processes
{
    public class ResetTrial
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        
        public static void Run()
        {
            if (!RigSingleton.Instance.GetHistorianConfig().HistEnvironment.ResetTrial) return;
            Log.Info("Starting ResetTrial process...");
            new Thread(Check).Start();
        }

        private static void Check()
        {
            do
            {
                try
                {
                    RigSingleton.Instance.EgnServer.Init();
                    var trialModules = string.Join(",", RigSingleton.Instance.EgnServer.IgnitionController.GetModules().Where(x => x.License == "Trial").Select(x => x.Name));
                    if (!string.IsNullOrEmpty(trialModules))
                    {
                        Log.Info($"{trialModules} modules in trial.");
                        dynamic trialInfo = JsonConvert.DeserializeObject(RigSingleton.Instance.EgnServer.IgnitionController.GetHtmlContent("/main/data/status/trial"));
                        if (trialInfo.remainingDisplay == "0:00:00")
                        {
                            Log.Info("Resetting trial...");
                            var htmlContent = RigSingleton.Instance.EgnServer.IgnitionController.PutHtmlContent("/main/data/status/trial");
                            Log.Info($"htmlContent: {htmlContent}");
                            Thread.Sleep(TimeSpan.FromMinutes(15)); // Sometimes resetting trial doesn't work and this loop would hammer hard the Ignition gateway.
                        }
                        trialInfo = JsonConvert.DeserializeObject(RigSingleton.Instance.EgnServer.IgnitionController.GetHtmlContent("/main/data/status/trial"));
                        Log.Info(trialInfo.ToString());
                        Log.Info($"Sleeping for {trialInfo.remainingDisplay.ToString()}");
                        Thread.Sleep(TimeSpan.Parse(trialInfo.remainingDisplay.ToString()));
                    }
                    else
                    {
                        Log.Info("Sleeping for 15 minutes; no modules in trial?");
                        Thread.Sleep(TimeSpan.FromMinutes(15));
                    }

                    if (!RigSingleton.Instance.GetHistorianConfig().HistEnvironment.ResetTrial)
                    {
                        Log.Info("ResetTrial was turned off");
                        return;
                    }
                }
                catch (Exception e)
                {
                    Log.Error(e);
                    Log.Error("Failed ResetTrial process.");
                    RigSingleton.Instance.SendMessage($"Failed ResetTrial {e.Message}");
                }
            } while (true);
        }
    }
}