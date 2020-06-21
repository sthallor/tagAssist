using System;
using Common;
using Common.Extensions;
using Common.Models.Reporting;
using MySql.Data.MySqlClient;

namespace Batch.Checks.Retired
{
    public class RigStateLocal
    {
        private const int ConsideredRecentDataInMinutes = 15;

        public bool Check(EgnServer egnServer)
        {
            try
            {
                // if (!_realTimeRigStateRigs.Contains(egnServer.RigNumber)) return true;
                var connectionString = $"SERVER={egnServer.Server};DATABASE=Ignition;UID=root;PASSWORD=ensignDatabase;Allow User Variables=True;SslMode=none;Connect Timeout=90";
                using (var connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    var cmd = connection.CreateCommand();
                    cmd.CommandText = @"CALL `get_most_recent_rigstate_tagdata`()";
                    cmd.CommandTimeout = 90;
                    var reader = cmd.ExecuteReader();
                    var read = reader.Read();
                    if (read)
                    {
                        var s = reader.GetString(1);
                        var timeStamp = long.Parse(s);
                        var unixTimeStampToDateTime = Utility.UnixTimeStampToDateTime(timeStamp);
                        var timeSpan = DateTime.Now - unixTimeStampToDateTime;
                        if (timeSpan.TotalMinutes > ConsideredRecentDataInMinutes)
                            EsiLog.HardError(egnServer, $"Rig state has not been classified in {timeSpan.ToPrettyFormat()}", "Internal");
                        else
                            EsiLog.Info(egnServer, $"Rig state has been recently classified {timeSpan.ToPrettyFormat()}");
                    }
                    else
                    {
                        EsiLog.HardError(egnServer, "Call to get_most_recent_rigstate_tagdata() proc did not return a result.", "Internal");
                    }
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}