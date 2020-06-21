using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Common.Models.Jira;
using IgorRig.Misc;
using log4net;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;

namespace IgorRig.Processes
{
    public class DataGap 
    {
        private static readonly TimeSpan RepeatCheckEvery = TimeSpan.FromMinutes(30);
        private static readonly TimeSpan ConditionsNotMet = TimeSpan.FromHours(5);
        private const int TagAgeInMinutesToCheck = 15;
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public static void Run()
        {
            var thread = new Thread(Check);
            thread.Start();
        }

        private static void Check()
        {
            try
            {
                do
                {
                    // Checks
                    if (DoesJiraExist())
                    {
                        Log.Warn("Jira ticket exists; skipping this check.");
                        Thread.Sleep(ConditionsNotMet);
                        continue;
                    }
                    var tagDatas = GetTagDataAge();
                    var lessThanAge = tagDatas.Count(x => x.TimeSpan.TotalMinutes < TagAgeInMinutesToCheck);
                    var greaterThanAge = tagDatas.Count(x => x.TimeSpan.TotalMinutes > TagAgeInMinutesToCheck);
                    if (lessThanAge < greaterThanAge && lessThanAge > 0 && lessThanAge < 50)
                    {
                        RigSingleton.Instance.SendMessage($"Found data gap. {lessThanAge} tags under {TagAgeInMinutesToCheck} minutes old. {greaterThanAge} over.");
                        RigSingleton.Instance.EgnServer.Init();
                        RigSingleton.Instance.EgnServer.ServiceController.Stop("Ignition");
                        Thread.Sleep(10000);
                        RigSingleton.Instance.EgnServer.ServiceController.Start("Ignition");
                    }
                    Log.Info($"Tag Data Age: {lessThanAge} tags under {TagAgeInMinutesToCheck} minutes old. {greaterThanAge} over.");
                    Thread.Sleep(RepeatCheckEvery);
                } while (true);
            }
            catch (Exception e)
            {
                Log.Error(e);
                RigSingleton.Instance.SendMessage($"DataGap process abend. {e}");
            }
        }

        private static bool DoesJiraExist()
        {
            try
            {
                var deserializeObject = JsonConvert.DeserializeObject<List<JiraTicket>>(File.ReadAllText(@"C:\Installs\IgorConfig\Common\JiraTickets.json"));
                if (deserializeObject.Any(x =>
                    x.RigNumber == RigSingleton.Instance.EgnServer.RigNumber &&
                    x.Description.Contains("* OPCUA server is reporting bad quality data.")))
                    return true;
            }
            catch (Exception) { /* Ignored */ }
            return false;
        }

        public static List<TagDataAge> GetTagDataAge()
        {
            const string connectionString = "SERVER=localhost;DATABASE=Ignition;UID=root;PASSWORD=ensignDatabase;Allow User Variables=True;SslMode=none;Connect Timeout=90";
            var connection = new MySqlConnection(connectionString);
            connection.Open();
            var cmd1 = connection.CreateCommand();
            cmd1.CommandText = "SELECT pname FROM ignition.sqlth_partitions order by start_time desc limit 1;";
            cmd1.CommandTimeout = 90;
            var reader1 = cmd1.ExecuteReader();
            reader1.Read();
            var tableName = reader1.GetString(0);
            reader1.Close();

            var cmd2 = connection.CreateCommand();
            cmd2.CommandTimeout = 90;
            cmd2.CommandText = $@"select TagId, max(t_stamp) as TimeStamp from {tableName} group by tagid order by 2 desc";
            var reader2 = cmd2.ExecuteReader();
            var tags = new List<TagDataAge>();
            while (reader2.Read())
            {
                var s = reader2.GetString(1);
                var timeStamp = long.Parse(s);
                var dateTime = Utility.UnixTimeStampToDateTime(timeStamp);
                var timeSpan = DateTime.Now - dateTime;
                tags.Add(new TagDataAge {TagId = reader2.GetInt32(0), DateTime = dateTime, TimeSpan = timeSpan});
            }
            reader2.Close();
            return tags;
        }
    }
    public class TagDataAge
    {
        public int TagId { get; set; }
        public DateTime DateTime { get; set; }
        public TimeSpan TimeSpan { get; set; }
    }

}