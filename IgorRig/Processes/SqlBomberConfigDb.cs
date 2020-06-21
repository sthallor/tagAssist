using System;
using System.Data.SQLite;
using System.IO;
using System.Reflection;
using System.Threading;
using Common.Controllers;
using Common.Models.Reporting;
using IgorRig.Misc;
using log4net;

namespace IgorRig.Processes
{
    public class SqlBomberConfigDb
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private static readonly TimeSpan RepeatCheckEvery = TimeSpan.FromMinutes(2);
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
                    Directory.CreateDirectory(@"C:\Installs\IgorConfig\ConfigDbSQL");
                    var files = Directory.GetFiles(@"C:\Installs\IgorConfig\ConfigDbSQL", "*.sql");
                    foreach (var filePath in files)
                    {
                        var ignitionConfigQuery = File.ReadAllText(filePath);
                        Log.Info($"Executing query;\n{ignitionConfigQuery}");
                        RunSqlCommand(RigSingleton.Instance.EgnServer, ignitionConfigQuery);
                        File.Delete(filePath);
                        RigSingleton.Instance.SendMessage($"👍 Executed ConfigDb script {filePath}");
                    }
                    Thread.Sleep(RepeatCheckEvery);
                } while (true);
            }
            catch (Exception e)
            {
                Log.Error(e.Message);
                Log.Error(e.ToString());
            }
        }
        private static void RunSqlCommand(EgnServer egnServer, string query)
        {
            var configurator = new Configurator(egnServer);
            configurator.StopService();
            Thread.Sleep(TimeSpan.FromMinutes(1));
            configurator.BackupIdb();
            configurator.ExecSql(query);
            configurator.StartService();
        }
    }
    public class Configurator
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private readonly ServiceController _serviceController;
        public Configurator(EgnServer server)
        {
            var server1 = server;
            Log.Info($"Start process for {server1.Server}");
            _serviceController = new ServiceController(server1);
        }

        public void ExecSql(string query)
        {
            Log.Info("Executing query.");

            try
            {
                using (var connection = new SQLiteConnection(@"Data Source=C:\Program Files\Inductive Automation\Ignition\data\db\config.idb;Version=3", true))
                {
                    connection.Open();
                    var sqLiteCommand = new SQLiteCommand(query, connection);
                    var command = sqLiteCommand;
                    command.ExecuteNonQuery();
                }
            }
            catch (Exception e)
            {
                Log.Error("Unable to complete ExecSql step.");
                Log.Error($"{e.Message}");
                RigSingleton.Instance.SendMessage("💀 Failed to execute ConfigDb script.");
                throw;
            }
        }

        public void StopService()
        {
            try
            {
                _serviceController.Stop("Ignition");
            }
            catch (Exception e)
            {
                Log.Error("Unable to stop Ignition service.");
                Log.Error($"{e.Message}");
                throw;
            }
        }

        public void StartService()
        {
            try
            {
                _serviceController.Start("Ignition");
            }
            catch (Exception e)
            {
                Log.Error("Unable to start Ignition service.");
                Log.Error($"{e.Message}");
                throw;
            }
        }

        public void BackupIdb()
        {
            try
            {
                Log.Info("Backing up config.idb.");
                const string sourceFileName = @"C:\Program Files\Inductive Automation\Ignition\data\db\config.idb";
                var destFileName = $@"C:\Program Files\Inductive Automation\Ignition\data\db\config.idb{DateTime.Now:yyyyMMdd_hhmmss}.bak";
                File.Copy(sourceFileName, destFileName);
            }
            catch (Exception e)
            {
                Log.Error("Unable to backup config.idb.");
                Log.Error($"{e.Message}");
                throw;
            }
        }
    }
}