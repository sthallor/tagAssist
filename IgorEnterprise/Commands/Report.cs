using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Reflection;
using Common;
using Common.Database;
using IgorEnterprise.Misc;
using log4net;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InputFiles;
using TheArtOfDev.HtmlRenderer.WinForms;

namespace IgorEnterprise.Commands
{
    public class Report
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private readonly Message _message;

        public Report(Message message)
        {
            _message = message;
            Log.Info("Received command to view report.");
        }

        public void Execute()
        {
            try
            {
                GetReport();
            }
            catch (Exception e)
            {
                Log.Error(e.Message);
                Log.Error(e.StackTrace);
                Singleton.Instance.Bot.SendTextMessageAsync(Singleton.Instance.ChatId, $"Failed to execute report command. {e.Message}");
            }
        }

        private void GetReport()
        {
            var rigRemarks = ReportingDb.GetRigReportHtml(_message.Text.Split(' ')[1]);
            var image = HtmlRender.RenderToImage(rigRemarks, new Size(800, 400), new Size(600, 1200), Color.White);
            var stream = image.ToStream(ImageFormat.Bmp);
            var inputOnlineFile = new InputOnlineFile(stream);
            Singleton.Instance.Bot.SendPhotoAsync(Singleton.Instance.ChatId, inputOnlineFile, null, ParseMode.Html);
        }
    }
}