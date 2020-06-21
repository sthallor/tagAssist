using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Common.Controllers;
using Common.Database;
using Common.Models;
using Common.Models.Jira;
using Common.Models.Models;
using Common.Models.Reporting;
using log4net;
using Newtonsoft.Json;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace Common
{
    public sealed class Singleton
    {
        private static readonly Lazy<Singleton> Lazy = new Lazy<Singleton>(() => new Singleton());
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        public static Singleton Instance => Lazy.Value;
        public List<string> LicenseKeys = new List<string>();

        private List<TagData> _tagData = new List<TagData>();
        private List<JiraTicket> _jiraTickets;
        public ConcurrentBag<CheckStatus> Bag = new ConcurrentBag<CheckStatus>();

        public const int DelinquentAfterHours = 5;

        public readonly long ChatId = -1001480352681;
        public readonly TelegramBotClient Bot = new TelegramBotClient("633493863:AAG7moRY8cPv2ZxtU9sqelLY5uzkiDcvOQQ");

        // Realtime Rig State
        public string LatestVersion;
        public string Md5ForFolder;
        public string SourceDir;

        private List<OpcUaDefaults> _opcUaDefaults;

        public bool DebugMode;
        public EgnServer Egn;


        private Singleton()
        {
        }

        public List<TagData> GetTagData()
        {
            if (_tagData.Count == 0)
            {
                Log.Info("Retriving recent tag data for context on checks...");
                _tagData = ReportingDb.GetTagData();
            }
            return _tagData;
        }

        public List<OpcUaDefaults> GetOpcUaDefaults()
        {
            if (DebugMode)
            {
                return _opcUaDefaults ?? (_opcUaDefaults =
                           JsonConvert.DeserializeObject<List<OpcUaDefaults>>(
                               File.ReadAllText(@"\\cal0-vp-ace01\e$\share\IgorConfig\Common\OpcUaDefaults.json")));

            }
            return _opcUaDefaults ?? (_opcUaDefaults = 
               JsonConvert.DeserializeObject<List<OpcUaDefaults>>(
                   File.ReadAllText(@"C:\Installs\IgorConfig\Common\OpcUaDefaults.json")));
        }

        public List<JiraTicket> GetJiraTickets()
        {
            return _jiraTickets ?? (_jiraTickets = JiraController.GetJiraTickets());
        }

        public void SendMessage(string message)
        {
            try
            {
                Retry.Do(() => Bot.SendTextMessageAsync(ChatId, $"{message}", ParseMode.Html, true), TimeSpan.FromMinutes(1));
            }
            catch (Exception e)
            {
                Log.Error($"Could not send Telegram message {message}");
                Log.Error(e);
            }
        }

        public void SendMessage(EgnServer egnServer, string message)
        {
            const int messageLimit = 4000;
            if (message.Length <= messageLimit)
            {
                var result = Bot.SendTextMessageAsync(ChatId, $"<a href=\"http://{egnServer.Server}.ensign.int:8088/\">{egnServer.RigNumber}</a> {message}", ParseMode.Html, true).Result;
            }
            else
            {
                var chunks = ChunksUpto(message, messageLimit).ToList();
                foreach (var chunk in chunks)
                {
                    var result = Bot.SendTextMessageAsync(ChatId, $"<a href=\"http://{egnServer.Server}.ensign.int:8088/\">{egnServer.RigNumber}</a> {chunk}", ParseMode.Html, true).Result;
                    Thread.Sleep(TimeSpan.FromSeconds(15));
                }
            }

        }
        static IEnumerable<string> ChunksUpto(string str, int maxChunkSize)
        {
            for (int i = 0; i < str.Length; i += maxChunkSize)
                yield return str.Substring(i, Math.Min(maxChunkSize, str.Length - i));
        }
    }
}