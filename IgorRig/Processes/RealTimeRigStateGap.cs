using System;
using System.Reflection;
using System.Threading;
using IgorRig.Misc;
using log4net;
using MySql.Data.MySqlClient;

namespace IgorRig.Processes
{
    public class RealTimeRigStateGap
    {
        private static readonly TimeSpan RepeatCheckEvery = TimeSpan.FromHours(1);
        private const int HoursToCheck = 4;
        private const int Warning = 15;
        private const int Error = 45;
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public static void Run()
        {
            if (!RigSingleton.Instance.GetHistorianConfig().HistEnvironment.RealTimeRigState) return;
            Log.Info("Starting real time rig state gap check process...!");
            new Thread(Check).Start();
        }

        private static void Check()
        {
            try
            {
                do
                {
                    var missingTimeSpan = GetMissingTimeSpan();
                    var message = $"Real time rig state data timespan missing from last {HoursToCheck} hours is {missingTimeSpan.ToPrettyFormat()}";
                    if (missingTimeSpan.TotalMinutes > Error)
                    {
                        Log.Error(message);
                        RigSingleton.Instance.SendMessage(message);
                    }
                    else if(missingTimeSpan.TotalMinutes > Warning)
                    {
                        Log.Warn(message);
                    }
                    else
                    {
                        Log.Info(message);
                    }
                    Thread.Sleep(RepeatCheckEvery);
                } while (true);
            }
            catch (Exception e)
            {
                Log.Error(e.Message);
                Log.Error(e.ToString());
                RigSingleton.Instance.SendMessage($"RealTimeRigStateGap process abend. {e.Message}");
            }
        }

        public static TimeSpan GetMissingTimeSpan()
        {
            var start = UnixTimeStamp(DateTime.UtcNow - TimeSpan.FromHours(HoursToCheck));
            var finish = UnixTimeStamp(DateTime.UtcNow);
            int recordCount;

            var connectionString = $"SERVER={RigSingleton.Instance.EgnServer.Server};DATABASE=Ignition;UID=root;PASSWORD=ensignDatabase;Allow User Variables=True;SslMode=none;Connect Timeout=90";
            using (var connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = $"SELECT count(*) FROM ignition.sql_tagdata where t_stamp between {start} and {finish}";
                command.CommandTimeout = 90;
                var reader = command.ExecuteReader();
                reader.Read();
                recordCount = reader.GetInt32(0);
                reader.Close();
            }
            var missingTimeSpan = TimeSpan.FromSeconds((HoursToCheck * 60 * 6 - recordCount) * 10);
            return missingTimeSpan;
        }

        public static ulong UnixTimeStamp(DateTime dateTime)
        {
            var unixTimestamp = (ulong)dateTime.Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds;
            return unixTimestamp;
        }
    }
}