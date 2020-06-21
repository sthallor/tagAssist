using System;
using System.Configuration;
using System.IO;
using Common;
using Common.Models.Models;
using Common.Models.Reporting;
using Newtonsoft.Json;

namespace IgorRig.Misc
{
    public sealed class RigSingleton
    {
        private static readonly Lazy<RigSingleton> Lazy = new Lazy<RigSingleton>(() => new RigSingleton());
        public static RigSingleton Instance => Lazy.Value;
        public readonly EgnServer EgnServer;

        private HistorianConfig _config;

        private RigSingleton()
        {
            EgnServer = new EgnServer
            {
                Server = ConfigurationManager.AppSettings["Server"],
                RigNumber = ConfigurationManager.AppSettings["Rig"]
            };
            EgnServer.Init();
        }

        public void SendMessage(string message)
        {
            try
            {
                Singleton.Instance.SendMessage(EgnServer, message);
            }
            catch (Exception)
            {
                // Due to an exception; AuthenticationException: The remote certificate is invalid according to the validation procedure.
                // If the telegram message fails to deliver; move it to Output and have IgorBatch deliver.
                File.WriteAllText($@"C:\Installs\IgorConfig\Output\TgMsg{Instance.EgnServer.RigNumber}_{DateTime.Now:yyMMdd.hhmmss}.txt", message);
            }
        }

        public HistorianConfig GetHistorianConfig()
        {
            if (Singleton.Instance.DebugMode)
            {
                return _config ?? (_config =
                           JsonConvert.DeserializeObject<HistorianConfig>(File.ReadAllText(
                               $@"\\cal0-vp-ace01\share\IgorConfig\Output\HistorianConfig{EgnServer.RigNumber}.json")));
            }
            return _config ?? (_config =
                       JsonConvert.DeserializeObject<HistorianConfig>(File.ReadAllText(
                           $@"C:\Installs\IgorConfig\{EgnServer.RigNumber}\HistorianConfig.json")));
        }

        public HistorianConfig ReGetHistorianConfig()
        {
            if (Singleton.Instance.DebugMode)
            {
                return _config = JsonConvert.DeserializeObject<HistorianConfig>
                    (File.ReadAllText($@"\\cal0-vp-ace01\share\IgorConfig\Output\HistorianConfig{EgnServer.RigNumber}.json"));
            }
            return _config = JsonConvert.DeserializeObject<HistorianConfig>
                (File.ReadAllText($@"C:\Installs\IgorConfig\{EgnServer.RigNumber}\HistorianConfig.json"));
        }
    }
}