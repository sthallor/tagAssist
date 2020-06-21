using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Common;
using Common.Database;
using log4net;
using Telegram.Bot.Types.Enums;

namespace IgorEnterprise.Process
{
    public class FailedTgMsg
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private static readonly TimeSpan RepeatCheckEvery = TimeSpan.FromMinutes(5);

        public static void Run()
        {
            var thread = new Thread(Check);
            thread.Start();
        }

        private static void Check()
        {
            do
            {
                string message = "";
                try
                {
                    var files = Directory.GetFiles(@"\\cal0-vp-ace01\e$\share\IgorConfig\Output", "TgMsg*.txt");
                    foreach (var file in files)
                    {
                        var server = ReportingDb.GetAllEgn()
                            .FirstOrDefault(x => x.RigNumber == Between(file, "TgMsg", "_"));
                        message = File.ReadAllText(file).Replace("--->", "").Replace("<---", "").Replace("---","");
                        const int messageLimit = 4000;
                        if (message.Length <= messageLimit)
                        {
                            if (server?.Server == null)
                            {
                                var result = Singleton.Instance.Bot
                                    .SendTextMessageAsync(Singleton.Instance.ChatId, $"{file} 🚧 {message}", ParseMode.Html).Result;
                            }
                            else
                            {
                                var result = Singleton.Instance.Bot
                                    .SendTextMessageAsync(Singleton.Instance.ChatId,
                                        $"<a href=\"http://{server.Server}.ensign.int:8088/\">{server.RigNumber}</a> 🚧 {message}", ParseMode.Html).Result;
                            }
                        }
                        else
                        {
                            var chunks = ChunksUpto(message, messageLimit).ToList();
                            foreach (var chunk in chunks)
                            {
                                var result = Singleton.Instance.Bot
                                    .SendTextMessageAsync(Singleton.Instance.ChatId,
                                        $"<a href=\"http://{server.Server}.ensign.int:8088/\">{server.RigNumber}</a> 🚧 {chunk}", ParseMode.Html).Result;
                                Thread.Sleep(TimeSpan.FromSeconds(15));
                            }
                        }
                        Thread.Sleep(TimeSpan.FromSeconds(3));
                        File.Delete(file);
                    }
                }
                catch (Exception e)
                {
                    if (!e.ToString().Contains("because it is being used by another process."))
                    {
                        Log.Error(e);
                        Log.Error(message);
                        Singleton.Instance.SendMessage($"🚨 IgorEnterprise FailedTgMsg failed. {e}");
                    }
                }
                Thread.Sleep(RepeatCheckEvery);
            } while (true);
        }
        public static string Between(string str, string firstString, string lastString)
        {
            var pos1 = str.IndexOf(firstString, StringComparison.Ordinal) + firstString.Length;
            var pos2 = str.IndexOf(lastString, StringComparison.Ordinal);
            var finalString = str.Substring(pos1, pos2 - pos1);
            return finalString;
        }
        static IEnumerable<string> ChunksUpto(string str, int maxChunkSize)
        {
            for (int i = 0; i < str.Length; i += maxChunkSize)
                yield return str.Substring(i, Math.Min(maxChunkSize, str.Length - i));
        }
    }
}