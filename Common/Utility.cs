using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Common.Models;
using log4net;
using Telegram.Bot.Types.Enums;

namespace Common
{
    public static class Utility
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private const string DoNotReply = "donotreply@ensignenergy.com";


        public static void SendEmailMessage(string htmlText)
        {
            var mail = new MailMessage();
            var client = new SmtpClient
            {
                Port = 25,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Host = ConfigurationManager.AppSettings["MailServer"]
            };
            foreach (var mailAddress in ConfigurationManager.AppSettings["MailTo"].Split(';'))
            {
                mail.To.Add(new MailAddress(mailAddress));
            }

            mail.From = new MailAddress(DoNotReply);
            mail.Subject = "Igor Summary";
            var viewHtml = AlternateView.CreateAlternateViewFromString(htmlText, null, "text/html");
            mail.AlternateViews.Add(viewHtml);
            client.Send(mail);
        }

        public static void SendMessage(string htmlText)
        {
            var mail = new MailMessage();
            var client = new SmtpClient
            {
                Port = 25,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Host = ConfigurationManager.AppSettings["MailServer"]
            };
            mail.To.Add(new MailAddress("German.Vega@ensignenergy.com"));
            mail.CC.Add(new MailAddress("Brian.Ogletree@ensignenergy.com"));
            mail.CC.Add(new MailAddress("Mark.Kuziej@ensignenergy.com"));
            mail.CC.Add(new MailAddress("Norbert.Happ@ensignenergy.com"));

            mail.From = new MailAddress(DoNotReply);
            mail.Subject = "Replication error";
            var viewHtml = AlternateView.CreateAlternateViewFromString(htmlText, null, "text/html");
            mail.AlternateViews.Add(viewHtml);
            client.Send(mail);
        }

        public static void SendCheckStatusSummaryActionable()
        {
            var strBody = new StringBuilder();
            strBody.Append("<html><head>");
            strBody.Append(MarkStyle());
            strBody.Append("</head>");
            strBody.Append("<body>");
            strBody.Append(GetInternal());
            strBody.Append(GetFieldService());
            strBody.Append(GetIgnored());
            strBody.Append("</body></html>");
            SendEmailMessage(strBody.ToString());
        }

        private static string GetIgnored()
        {
            var strTickets = new StringBuilder();
            strTickets.Append(string.Format("<h3>Ignored...</h3><table border=\"1\">" +
                                            "<thead><tr><th>EGN</th><th>Rig</th><th style=\"text-align:left\">Message</th></tr></thead><tbody>"));
            var rigsIgnored = Singleton.Instance.Bag.Where(x => x.Message.StartsWith("Ignoring")).Select(x => x.Rig).Distinct().ToList();
            var checks = new List<CheckStatus>();
            foreach (var rigIgnored in rigsIgnored)
            {
                var warnAndErrorResults = Singleton.Instance.Bag.Where(x => x.Rig == rigIgnored && x.Error >= 2 && x.Category != "Internal").ToList();
                var debug = Singleton.Instance.Bag.Where(x => x.Rig == rigIgnored && x.Error == 0).ToList();
                checks.AddRange(warnAndErrorResults);
                checks.AddRange(debug);
            }
            foreach (var check in checks.OrderBy(x=> x.Rig))
            {
                strTickets.Append("<tr " + GetSummaryColor(check.Error) + $" ><td><a href=\"http://{check.Host}:8088/main/web/home\">{check.Host}</a></td><td>" + check.Rig + "</td><td style=\"text-align:left\">" +
                                  check.Message + "</td></tr>");
            }
            strTickets.Append("</tbody></table>");
            return strTickets.ToString();
        }

        private static string GetFieldService()
        {
            var strTickets = new StringBuilder();
            strTickets.Append(string.Format("<h3>Field</h3><table border=\"1\">" +
                                            "<thead><tr><th>EGN</th><th>Rig</th><th style=\"text-align:left\">Message</th></tr></thead><tbody>"));
            var rigsInErrorState = Singleton.Instance.Bag.Where(x => x.Error == 3 && x.Category != "Internal").Select(x => x.Rig).Distinct().ToList();
            var checks = new List<CheckStatus>();
            foreach (var rigInError in rigsInErrorState)
            {
                var warnAndErrorResults = Singleton.Instance.Bag.Where(x => x.Rig == rigInError && x.Error >= 2 && x.Category != "Internal").ToList();
                checks.AddRange(warnAndErrorResults);
            }
            foreach (var check in checks.OrderBy(x => x.Rig))
            {
                var s = "<tr " + GetSummaryColor(check.Error) + $" ><td><a href=\"http://{check.Host}:8088/main/web/home\">{check.Host}</a></td><td>" + check.Rig + "</td><td style=\"text-align:left\">" +
                            check.Message + "</td></tr>";
                //var message = Singleton.Instance.Bot.SendTextMessageAsync(Singleton.Instance.ChatId, $"<a href=\"http://{check.Host}.ensign.int:8088\">{check.Rig}</a> {check.Message}", ParseMode.Html).Result;
                strTickets.Append(s);
            }
            strTickets.Append("</tbody></table>");
            return strTickets.ToString();
        }

        private static string GetInternal()
        {
            var strAction = new StringBuilder();
            strAction.Append(string.Format("<h3>Office</h3><table border=\"1\">" +
                                           "<thead><tr><th>EGN</th><th>Rig</th><th style=\"text-align:left\">Message</th></tr></thead><tbody>"));

            var checkStatusList = Singleton.Instance.Bag.Where(x => x.Category == "Internal").OrderBy(x => x.Rig).ToList();
            foreach (var check in checkStatusList.OrderBy(x=> x.Rig))
            {
                var s = "<tr " + GetSummaryColor(check.Error) + $" ><td><a href=\"http://{check.Host}:8088/main/web/home\">{check.Host}</a></td><td>" + check.Rig + "</td><td style=\"text-align:left\">" +
                            check.Message + "</td></tr>";
                var message = Singleton.Instance.Bot.SendTextMessageAsync(Singleton.Instance.ChatId, $"<a href=\"http://{check.Host}.ensign.int:8088\">{check.Rig}</a> {check.Message}", ParseMode.Html, true).Result;
                strAction.Append(s);
            }
            strAction.Append("</tbody></table>");
            return strAction.ToString();
        }

        private static string GetColor(int checkError)
        {
            var color = "";
            switch (checkError)
            {
                case 0:
                    color = @"BGCOLOR=#00FF00";
                    break;
                case 1:
                    break;
                case 2:
                    color = @"BGCOLOR=#FFFF00";
                    break;
                case 3:
                    color = @"BGCOLOR=#FF0000";
                    break;
            }
            return color;
        }

        private static string GetSummaryColor(int checkError)
        {
            var color = "";
            switch (checkError)
            {
                case 0:
                    color = @"BGCOLOR=#00FF00";
                    break;
            }
            return color;
        }

        public static void SendCheckStatusSummaryFull()
        {
            var strBody = new StringBuilder();
            strBody.Append("<html><head>");
            strBody.Append(MarkStyle());
            strBody.Append("</head>");
            strBody.Append("<body>");
            strBody.Append(string.Format("<h3>Igor Summary</h3><table border=\"1\">" +
                                         "<thead><tr><th>EGN</th><th>Rig</th><th style=\"text-align:left\">Message</th></tr></thead><tbody>"));

            var checkStatusList = Singleton.Instance.Bag.OrderBy(x => x.Rig).ToList();

            foreach (var check in checkStatusList)
            {
                strBody.Append("<tr " + GetColor(check.Error) + " ><td>" + check.Host + "</td><td>" + check.Rig + "</td><td style=\"text-align:left\">" +
                               check.Message + "</td></tr>");
            }
            strBody.Append("</tbody></table></body></html>");
            SendEmailMessage(strBody.ToString());
        }

        public static DateTime UnixTimeStampToDateTime(long unixTimeStamp)
        {
            var dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            // ReSharper disable once PossibleLossOfFraction
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp / 1000);
            return dtDateTime.ToLocalTime();
        }

        public static string MarkStyle()
        {
            return @"     <style type = ""text/css"">
    table, td, th{
      border: 1px solid black; 
         padding: 5px;
         border-collapse: collapse;
         font-family: calibri;
                                font-size: 14px;
         text-align: center;
    } 
        h3 {
         font-family: calibri;
    } 
    </style>
";
        }

        public static string CreateMd5ForRTRS(string path)
        {
            var files = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories).OrderBy(p => p).ToList();
            var md5 = MD5.Create();
            for (int i = 0; i < files.Count; i++)
            {
                var file = files[i];
                if(file.EndsWith(".txt") || file.EndsWith(".docx") || file.EndsWith(".pptx") || file.Contains("Thumbs.db"))
                    continue; // These files get changed post version cut and don't matter to functionality...
                // hash path
                var relativePath = file.Substring(path.Length + 1);
                var pathBytes = Encoding.UTF8.GetBytes(relativePath.ToLower());
                md5.TransformBlock(pathBytes, 0, pathBytes.Length, pathBytes, 0);
                // hash contents
                var contentBytes = File.ReadAllBytes(file);
                if (i == files.Count - 1)
                    md5.TransformFinalBlock(contentBytes, 0, contentBytes.Length);
                else
                    md5.TransformBlock(contentBytes, 0, contentBytes.Length, contentBytes, 0);
            }
            return BitConverter.ToString(md5.Hash).Replace("-", "").ToLower();
        }
        public static string EncryptString(string inputString)
        {
            byte[] iv = { 12, 21, 43, 17, 57, 35, 67, 27 };
            var encryptKey = "aXb2uy4z"; // MUST be 8 characters
            var key = Encoding.UTF8.GetBytes(encryptKey);
            var byteInput = Encoding.UTF8.GetBytes(inputString);
            var provider = new DESCryptoServiceProvider();
            var memStream = new MemoryStream();
            var transform = provider.CreateEncryptor(key, iv);
            var cryptoStream = new CryptoStream(memStream, transform, CryptoStreamMode.Write);
            cryptoStream.Write(byteInput, 0, byteInput.Length);
            cryptoStream.FlushFinalBlock();
            return Convert.ToBase64String(memStream.ToArray());
        }
        public static string DecryptString(string inputString)
        {
            byte[] iv = { 12, 21, 43, 17, 57, 35, 67, 27 };
            var encryptKey = "aXb2uy4z"; // MUST be 8 characters
            var key = Encoding.UTF8.GetBytes(encryptKey);
            var byteInput = Convert.FromBase64String(inputString);
            var provider = new DESCryptoServiceProvider();
            var memStream = new MemoryStream();
            var transform = provider.CreateDecryptor(key, iv);
            var cryptoStream = new CryptoStream(memStream, transform, CryptoStreamMode.Write);
            cryptoStream.Write(byteInput, 0, byteInput.Length);
            cryptoStream.FlushFinalBlock();
            var encoding1 = Encoding.UTF8;
            return encoding1.GetString(memStream.ToArray());
        }
    }
}