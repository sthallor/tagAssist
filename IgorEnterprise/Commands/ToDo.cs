using System;
using System.Linq;
using System.Reflection;
using Common;
using Common.Database;
using Common.Models.Igor;
using Common.Models.Reporting;
using log4net;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace IgorEnterprise.Commands
{
    public class ToDo
    {
        private readonly Message _message;
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private EgnServer _egn;

        public ToDo(Message message)
        {
            _message = message;
            Log.Info("Received command to ToDo.");
        }

        public void Execute()
        {
            try
            {
                GetRig();
                DoThing();
            }
            catch (Exception e)
            {
                Log.Error(e.Message);
                Log.Error(e.ToString());

                Singleton.Instance.SendMessage(_egn, $"Failed to execute todo command. {e.Message}");
            }
        }

        private void DoThing()
        {
            using (var db = new IgorDb())
            {
                if (_message.Text.ToLower() == "/todo")
                {
                    var message = "📇 I'll reminded of these when Igition server is online.\n";
                    foreach (var todo in db.ToDoList)
                    {
                        if(!string.IsNullOrEmpty(todo.Rig))
                            message += $"▫️ <a href=\"http://{todo.Server}.ensign.int:8088/\">{todo.Rig}</a> {todo.Message}\n";
                        else
                            message += $"▫️ {todo.Server} {todo.Message}\n";
                    }
                    Singleton.Instance.Bot.SendTextMessageAsync(Singleton.Instance.ChatId, $" {message}", ParseMode.Html);
                }
                else
                {
                    {
                        if (_message.ReplyToMessage != null)
                        {
                            var idStart = _message.ReplyToMessage.Text.IndexOf("[ToDoItem:");
                            var idEnds = _message.ReplyToMessage.Text.IndexOf(']', idStart + 1);
                            var id = int.Parse(_message.ReplyToMessage.Text.Substring(idStart + 10, idEnds - idStart - 10));
                            var toDoList = db.ToDoList.Find(id);
                            db.ToDoList.Remove(toDoList);
                            Singleton.Instance.Bot.SendTextMessageAsync(Singleton.Instance.ChatId, "❎ Removed from todo list.");
                        }
                        else
                        {
                            var firstSpace = _message.Text.IndexOf(' ');
                            var secondSpace = _message.Text.IndexOf(' ', firstSpace + 1);
                            var toDoList = new ToDoList
                            {
                                Rig = _egn.RigNumber, Server = _egn.Server,
                                Message = _message.Text.Substring(secondSpace + 1)
                            };
                            db.ToDoList.Add(toDoList);
                            Singleton.Instance.Bot.SendTextMessageAsync(Singleton.Instance.ChatId, "✅ Added todo list.");
                        }

                        db.SaveChanges();
                    }
                }
            }
        }

        private void GetRig()
        {
            try
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

                if (_egn == null && !string.IsNullOrEmpty(rig))
                {
                    _egn = new EgnServer {RigNumber = "", Server = rig};
                }

                if (_egn == null)
                {
                    Log.Info($"Couldn't find rig for {rig}");
                }
            }
            catch (Exception) { /* ignored */ }
        }
    }
}