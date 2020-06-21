using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Common.Database;
using Common.Models.Reporting;
using log4net;

namespace SqlBomber
{
    public static class Program
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        public static ConcurrentBag<string> QueryOutput = new ConcurrentBag<string>();
        public static ConcurrentBag<string> FinishedList = new ConcurrentBag<string>();
        private static readonly string QueryOutputFile = ConfigurationManager.AppSettings["QueryOutputFile"];
        public static readonly string IgnitionConfigQuery = ConfigurationManager.AppSettings["IgnitionConfigQuery"];
        public static readonly DateTime Start = DateTime.Now;
        public static List<string> Columns { get; set; }

        public static void Main()
        {
            var egnServers = ReportingDb.GetEgnServersSql(File.ReadAllText("EgnQuery.sql"));
            Log.Info($"Starting configurator on {egnServers.Count} ignition servers..");
            var ignitionConfigQuery = File.ReadAllText(IgnitionConfigQuery);
            Log.Info($"Executing query;\n{ignitionConfigQuery}");
            if (ignitionConfigQuery.ToLower().Trim().StartsWith("select "))
            {
                Log.Info("In query mode.");
                Console.WriteLine("Press any key to continue.");
                Console.ReadKey();
                var dataTable = RunSqlQuery(egnServers, ignitionConfigQuery);
                WriteToFile(dataTable);
            }
            else
            {
                Log.Warn("In command mode. (This stops/starts Ignition Service)");
                Console.WriteLine("Press any key to continue.");
                Console.ReadKey();
                RunSqlCommand(egnServers, ignitionConfigQuery);
            }
        }

        private static void WriteToFile(DataTable dataTable)
        {
            StringBuilder sb = new StringBuilder();

            IEnumerable<string> columnNames = dataTable.Columns.Cast<DataColumn>().
                                              Select(column => column.ColumnName);
            sb.AppendLine(string.Join(",", columnNames));

            foreach (DataRow row in dataTable.Rows)
            {
                IEnumerable<string> fields = row.ItemArray.Select(field => field.ToString());
                sb.AppendLine(string.Join(",", fields));
            }
            try
            {
                File.WriteAllText(QueryOutputFile, sb.ToString());
            }
            catch (Exception)
            {
                Console.WriteLine("Cannot write output file. Could you have excel open locking the file?");
                Console.WriteLine("Press any key to continue.");
                Console.ReadKey();
                File.WriteAllText(QueryOutputFile, sb.ToString());
            }
        }

        private static void RunSqlCommand(List<EgnServer> egnServers, string query)
        {
            Parallel.ForEach(egnServers, server =>
            {
                var configurator = new Configurator(server);
                try
                {
                    configurator.StopService();
                    configurator.BackupIdb();
                    configurator.ExecSql(query);
                    configurator.StartService();
                }
                catch (Exception)
                {
                    // ignored
                }
                FinishedList.Add(server.Server);
            });
        }

        public static DataTable RunSqlQuery(List<EgnServer> egnServers, string query)
        {
            Parallel.ForEach(egnServers, async server =>
            {
                var configurator = new Configurator(server);
                try
                {
                    var b = await Timeout.ForAsync(() => configurator.ExecQuery(query), TimeSpan.FromMinutes(10));
                    if (b)
                        Log.Info($"{server.Server} ({server.RigNumber}) Completed in time.");
                    else
                        Log.Info($"{server.Server} ({server.RigNumber}) Failed due to timeout.");
                }
                catch (Exception)
                {
                    // ignored
                }
                FinishedList.Add(server.Server);
            });
            while (FinishedList.Count < egnServers.Count)
            {
                Thread.Sleep(1000);
                if (DateTime.Now.Subtract(Start).Minutes > 10)
                    break;
            }
            var data = new DataTable();
            data.Columns.Add("EgnServer");
            data.Columns.Add("Rig");
            foreach (var column in Columns)
            {
                data.Columns.Add(column);
            }
            foreach (var row in QueryOutput)
            {
                try
                {
                    data.Rows.Add(row.Split(','));
                }
                catch (Exception)
                {
                    Log.Error(row);
                }
            }
            return data;
        }
    }
}