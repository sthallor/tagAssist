using Common.Models.Models;
using MySql.Data.MySqlClient;

namespace Common.Database
{
    public class MariaDb
    {
        public static int GetTagId(IgnitionData tag)
        {
            var sqlTagId = 0;
            using (var connection = new MySqlConnection(GetMariaDbConnectionString()))
            {
                connection.Open();
                var cmd = connection.CreateCommand();
                var query = $"select * from sqlth_te where tagpath = 'local/{tag.Path}/{tag.Name}' and retired is null";
                cmd.CommandText = query;
                cmd.CommandTimeout = 90;
                var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    sqlTagId = (int)reader[0];
                    return sqlTagId;
                }
            }
            return sqlTagId;
        }

        public static bool RecentTagData(IgnitionData tag)
        {
            var tagId = GetTagId(tag);
            var withPartitions = GetCommandWithPartitions(tagId);
            return DoItNow(withPartitions);
        }

        public static string GetCommandWithPartitions(int tagid)
        {
            var connectionString = GetMariaDbConnectionString();
            using (var connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                var cmd1 = connection.CreateCommand();
                var query1 = "SELECT pname FROM ignition.sqlth_partitions";
                cmd1.CommandText = query1;
                cmd1.CommandTimeout = 90;
                var reader1 = cmd1.ExecuteReader();


                var query3 = "";
                while (reader1.Read())
                    query3 += $@"select count(*) from {reader1.GetString(0)} where tagid = {tagid} union ";
                query3 = query3.Substring(0, query3.Length - 7); // Remove last 'union'
                return query3;
            }
        }

        private static string GetMariaDbConnectionString()
        {
            return Singleton.Instance.DebugMode ?
                $"SERVER={Singleton.Instance.Egn.Server};DATABASE=Ignition;UID=root;PASSWORD=ensignDatabase;Allow User Variables=True;SslMode=none;Connect Timeout=90" :
                "SERVER=localhost;DATABASE=Ignition;UID=root;PASSWORD=ensignDatabase;Allow User Variables=True;SslMode=none;Connect Timeout=90";
        }

        public static bool DoItNow(string query3)
        {
            var connectionString = GetMariaDbConnectionString();
            using (var connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                var cmd3 = connection.CreateCommand();
                cmd3.CommandTimeout = 90;
                cmd3.CommandText = query3;
                var reader3 = cmd3.ExecuteReader();
                while (reader3.Read())
                {
                    var s = reader3.GetString(0);
                    var idnumber = long.Parse(s);
                    if (idnumber > 0) return true;
                }
                reader3.Close();
            }
            return false;
        }
    }
}