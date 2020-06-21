using System;
using Batch.FactoryStuff;
using Common;
using Common.Extensions;
using Common.Models.Reporting;
using MySql.Data.MySqlClient;

namespace Batch.Checks
{
    public class LocalRecentTags : IEgnCheck
    {
        private const int ConsideredRecentDataInMinutes = 5;
        // GRANT ALL ON *.* to root@localhost IDENTIFIED BY 'ensignDatabase'; 
        // GRANT ALL ON*.* to root@'%' IDENTIFIED BY 'ensignDatabase';

        public bool Check(EgnServer egnServer)
        {
            try
            {
                var connectionString = $"SERVER={egnServer.Server};DATABASE=Ignition;UID=root;PASSWORD=ensignDatabase;Allow User Variables=True;SslMode=none;Connect Timeout=90";
                using (var connection = new MySqlConnection(connectionString))
                {
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
                    cmd2.CommandText = $@"select t_stamp from {tableName} order by t_stamp desc limit 1";
                    var reader2 = cmd2.ExecuteReader();
                    EsiLog.Info(egnServer, "Success in establishing MySQL connection to ignition db.");
                    while (reader2.Read())
                    {
                        var s = reader2.GetString(0);
                        var idnumber = long.Parse(s);
                        var unixTimeStampToDateTime = Utility.UnixTimeStampToDateTime(idnumber);
                        var timeSpan = DateTime.Now - unixTimeStampToDateTime;
                        EsiLog.Info(egnServer, timeSpan.TotalMinutes > ConsideredRecentDataInMinutes
                            ? $"Rig has not seen tag data in {timeSpan.ToPrettyFormat()}."
                            : "Rig has recent tag data.");
                    }
                    reader2.Close();
                    return true;
                }
            }
            catch (Exception)
            {
                if(egnServer.IgnitionController.IsLoggedIn())
                    EsiLog.Info(egnServer, "Failed to establish MySQL connection to ignition db.");
                return false;
            }
        }
    }
}