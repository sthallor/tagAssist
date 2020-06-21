using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Reflection;
using Common;
using Common.Database;
using Common.Models.Reporting;
using IgorEnterprise.Misc;
using log4net;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InputFiles;
using TheArtOfDev.HtmlRenderer.WinForms;

namespace IgorEnterprise.Commands
{
    public class Remarks
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private readonly Message _message;
        private EgnServer _egn;

        public Remarks(Message message)
        {
            _message = message;
            Log.Info("Received command to view remarks.");
        }

        public void Execute()
        {
            try
            {
                GetRig();
                GetRemarks();
            }
            catch (Exception e)
            {
                Log.Error(e.Message);
                Log.Error(e.StackTrace);
                Singleton.Instance.Bot.SendTextMessageAsync(Singleton.Instance.ChatId, $"Failed to execute remarks command. {e.Message}");
            }
        }

        private void GetRemarks()
        {
            var rigRemarks = ReportingDb.GetRigRemarksHtml(_egn.RigNumber);
            var image = HtmlRender.RenderToImage(rigRemarks, new Size(600, 400), new Size(600, 1200), Color.White);
            var stream = image.ToStream(ImageFormat.Bmp);
            var inputOnlineFile = new InputOnlineFile(stream);
            Singleton.Instance.Bot.SendPhotoAsync(Singleton.Instance.ChatId, inputOnlineFile, $"<a href=\"http://{_egn.Server}.ensign.int:8088/\">{_egn.RigNumber}</a>", ParseMode.Html);
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
                _egn = ReportingDb.GetAllEgnServers().FirstOrDefault(x => x.RigNumber == rig);
            }
            if (_egn == null)
                Log.Info($"Couldn't find rig for {rig}");
        }
    }
}