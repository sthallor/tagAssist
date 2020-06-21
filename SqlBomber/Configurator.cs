using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Reflection;
using Common.Controllers;
using Common.Models.Reporting;
using log4net;

namespace SqlBomber
{
    public class Configurator
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private readonly EgnServer _server;
        private readonly ServiceController _serviceController;
        public Configurator(EgnServer server)
        {
            _server = server;
            Log.Info($"Start process for {_server.Server}");
            _serviceController = new ServiceController(_server);
        }

        public void ExecSql(string query)
        {
            Log.Info($"{_server.Server} ({_server.RigNumber}) Executing query.");
            
            try
            {
                using (var connection = new SQLiteConnection($@"Data Source=\\\\{_server.Server}\c$\Program Files\Inductive Automation\Ignition\data\db\config.idb;Version=3", true))
                {
                    connection.Open();
                    var sqLiteCommand = new SQLiteCommand(query, connection);
                    var command = sqLiteCommand;
                    command.ExecuteNonQuery();
                }
            }
            catch (Exception e)
            {
                Log.Error($"{_server.Server} ({_server.RigNumber}) Unable to complete ExecSql step.");
                Log.Error($"{_server.Server} ({_server.RigNumber}) {e.Message}");
                throw;
            }
        }

        public void ExecQuery(string query)
        {
            Log.Info($"{_server.Server} ({_server.RigNumber}) Executing query.");
            try
            {
                List<string> columns;
                using (var connection = new SQLiteConnection(GetValue(), true))
                {
                    Log.Debug($"{_server.Server} ({_server.RigNumber}) Opening Connection...");
                    connection.Open();
                    var command = new SQLiteCommand(query, connection);
                    Log.Debug($"{_server.Server} ({_server.RigNumber}) Executing reader");
                    var reader = command.ExecuteReader();
                    columns = Enumerable.Range(0, reader.FieldCount).Select(reader.GetName).ToList();
                    while (reader.Read())
                    {
                        var line = $"{_server.Server},{_server.RigNumber},";
                        foreach (var s in columns)
                        {
                            line += $"{reader[s.Trim()]},";
                        }
                        Log.Debug($"{_server.Server} ({_server.RigNumber}) Add query output");
                        Program.QueryOutput.Add($"{line.Remove(line.Length - 1)}");
                    }
                }
                Log.Debug($"{_server.Server} ({_server.RigNumber}) Finished connection...");
                if(Program.Columns == null)
                    Program.Columns = columns;
            }
            catch (Exception e)
            {
                Log.Error($"{_server.Server} ({_server.RigNumber}) Unable to complete ExecSql step.");
                Log.Error($"{_server.Server} ({_server.RigNumber}) {e.Message}");
                throw;
            }
        }

        private string GetValue()
        {
            var destination = $@"\\cal0-vp-ace01\e$\share\EdgeHistorianTroubleshooting\IgnitionConfigBackup\{_server.RigNumber}-{_server.Server}.db";
            var destinationFile = new FileInfo(destination);
            var source = $@"\\{_server.Server}\c$\Program Files\Inductive Automation\Ignition\data\db\config.idb";
            var sourceFile = new FileInfo(source);
            if (!destinationFile.Exists || sourceFile.LastWriteTime > destinationFile.LastWriteTime)
            {
                Console.WriteLine("Copying file...");
                File.Copy(source, destination, true);
            }
            var connectionString = $@"Data Source=\\{destination};Version=3";
            return connectionString;
        }

        public void StopService()
        {
            try
            {
                _serviceController.Stop("Ignition");
            }
            catch (Exception e)
            {
                Log.Error($"{_server.Server} ({_server.RigNumber}) Unable to stop Ignition service.");
                Log.Error($"{_server.Server} ({_server.RigNumber}) {e.Message}");
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
                Log.Error($"{_server.Server} ({_server.RigNumber}) Unable to start Ignition service.");
                Log.Error($"{_server.Server} ({_server.RigNumber}) {e.Message}");
                throw;
            }
        }

        public void BackupIdb()
        {
            try
            {
                Log.Info($"{_server.Server} ({_server.RigNumber}) Backing up config.idb.");
                var sourceFileName = $@"\\{_server.Server}\c$\Program Files\Inductive Automation\Ignition\data\db\config.idb";
                var destFileName = $@"\\{_server.Server}\c$\Program Files\Inductive Automation\Ignition\data\db\config.idb{DateTime.Now:yyyyMMdd_hhmmss}.bak";
                File.Copy(sourceFileName, destFileName);
            }
            catch (Exception e)
            {
                Log.Error($"{_server.Server} ({_server.RigNumber}) Unable to backup config.idb.");
                Log.Error($"{_server.Server} ({_server.RigNumber}) {e.Message}");
                throw;
            }
        }
    }
}