using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Common;
using IgorEnterprise.Commands;
using log4net;
using Telegram.Bot.Args;
using Telegram.Bot.Types.Enums;

namespace IgorEnterprise
{
    public class MessageRouter
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        public static void MessageReceived(object sender, MessageEventArgs e)
        {
            Log.Info($"Message received from Username:{e.Message.From.Username} Id:{e.Message.From.Id}");
            if (e.Message.Type != MessageType.Text)
            {
                Log.Warn("Message was not text?!");
                return;
            }
            Log.Info($"Text: {e.Message.Text}");
            var authorizedUsers = new List<int> {521159278, 985919967}; // Brian, Mark
            if (!authorizedUsers.Contains(e.Message.From.Id))
            {
                Singleton.Instance.Bot.SendTextMessageAsync(e.Message.Chat.Id, $"🙉🙈🙊 Sorry, I can't hear/see/speak to you! \nPlease ask Brian to authorize you. Give him the number: *{e.Message.From.Id}*", ParseMode.Markdown);
                return;
            }

            try
            {
                if (e.Message.ReplyToMessage.Text.Contains("[ToDoItem:"))
                {
                    new Thread(() => { new ToDo(e.Message).Execute(); }).Start();
                    return;
                }
                if (e.Message.ReplyToMessage.Text.Contains("Do you want to create a JIRA Ticket?"))
                {
                    new Thread(() => { new Jira(e.Message).Execute(); }).Start();
                    return;
                }
            }
            catch (Exception) { /* ignored */ }
            try
            {
                var command = e.Message.Text.Split(' ')[0];
                switch (command)
                {
                    case "/restart":
                        new Thread(() => { new Restart(e.Message).Execute(); }).Start();
                        break;
                    case "/reboot":
                        new Thread(() => { new Reboot(e.Message).Execute(); }).Start();
                        break;
                    case "/remarks":
                        new Thread(() => { new Remarks(e.Message).Execute(); }).Start();
                        break;
                    case "/report":
                        new Thread(() => { new Report(e.Message).Execute(); }).Start();
                        break;
                    case "/todo":
                        new Thread(() => { new ToDo(e.Message).Execute(); }).Start();
                        break;
                    default:
                        Singleton.Instance.Bot.SendTextMessageAsync(e.Message.Chat.Id, "🙈 Command not recognized.");
                        break;
                }
            }
            catch (Exception) { /* ignored */ }
        }
    }
}