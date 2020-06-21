using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Reflection;
using Common.Models.Models;
using Common.Models.Reporting;
using log4net;

namespace Common.Database
{
    public static class ConfigDb
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        public static readonly string DefaultScanClass = "Minutes5";

        public static string GetConnectionRefreshEnterprise(EgnServer _egn)
        {
            var destination = $@"\\cal0-vp-ace01\e$\share\EdgeHistorianTroubleshooting\IgnitionConfigBackup\{_egn.RigNumber}-{_egn.Server}.db";
            var destinationFile = new FileInfo(destination);
            var source = $@"\\{_egn.Server}\c$\Program Files\Inductive Automation\Ignition\data\db\config.idb";
            var sourceFile = new FileInfo(source);
            if (!destinationFile.Exists || sourceFile.LastWriteTime > destinationFile.LastWriteTime && destinationFile.LastWriteTime < DateTime.Now.Subtract(TimeSpan.FromHours(1)))
            {
                Console.WriteLine("Copying File...");
                File.Copy(source, destination + ".tmp", true);
                if (destinationFile.Exists) File.Delete(destination);
                File.Move(destination + ".tmp", destination);
            }
            var connectionString = $@"Data Source=\\{destination};Version=3";
            return connectionString;
        }

        private static SQLiteConnection GetCopyReadConnection()
        {
            string connectionString;
            if(Singleton.Instance.DebugMode)
            {
                connectionString = $@"Data Source=\\\\cal0-vp-ace01\share\EdgeHistorianTroubleshooting\IgnitionConfigBackup\{Singleton.Instance.Egn.RigNumber}-{Singleton.Instance.Egn.Server}.db;Version=3";
                return new SQLiteConnection(connectionString, true);
            }
            var source = @"C:\Program Files\Inductive Automation\Ignition\data\db\config.idb";
            var sourceFile = new FileInfo(source);
            var destination = @"C:\Program Files\Inductive Automation\Ignition\data\db\configRead.idb";
            var destinationFile = new FileInfo(destination);
            if (!destinationFile.Exists || sourceFile.LastWriteTime > destinationFile.LastWriteTime)
            {
                File.Copy(source, destination, true);
            }
            connectionString = $@"Data Source={destination};Version=3";
            return new SQLiteConnection(connectionString, false);
        }

        private static SQLiteConnection GetUpdateConnection()
        {
            string connectionString;
            if (Singleton.Instance.DebugMode)
            {
                connectionString = $@"Data Source=\\\\cal0-vp-ace01\share\EdgeHistorianTroubleshooting\IgnitionConfigBackup\{Singleton.Instance.Egn.RigNumber}-{Singleton.Instance.Egn.Server}.db;Version=3";
                return new SQLiteConnection(connectionString, true);
            }
            var source = @"C:\Program Files\Inductive Automation\Ignition\data\db\config.idb";
            connectionString = $@"Data Source={source};Version=3";
            return new SQLiteConnection(connectionString, false);
        }

        public static List<IgnitionData> GetTagData(OpcUaServerConfig opcUaServer, HistoryProvider historyProvider)
        {
            var count = 0;
            var query = $@"select a.sqltag_id, a.Path, a.Name, a.DataType, 
  b.strval as OPCItemPath,
  c.strval as OPCServer,
  d.strval as HistoricalScanclass,
  e.strval as PrimaryHistoryProvider
from SQLTAG a 
left outer join SQLTAGPROP b on a.sqltag_id = b.tagid and b.name = 'OPCItemPath'
left outer join SQLTAGPROP c on a.sqltag_id = c.tagid and c.name = 'OPCServer'
left outer join SQLTAGPROP d on a.sqltag_id = d.tagid and d.name = 'HistoricalScanclass'
left outer join SQLTAGPROP e on a.sqltag_id = e.tagid and e.name = 'PrimaryHistoryProvider'
where a.enabled = 1 and e.strval = '{historyProvider.Name}' and c.strval = '{opcUaServer.Name}' and a.path like '{historyProvider.RootFolder}%'
";
            var execQuery = new List<IgnitionData>();
            try
            {
                using (var connection = GetCopyReadConnection())
                {
                    connection.Open();
                    var command = new SQLiteCommand(query, connection);
                    var reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        execQuery.Add(new IgnitionData(reader));
                        count += 1;
                        if (count % 10 == 0)
                            Console.Write(":");
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error($"{e.Message}");
            }
            return execQuery;
        }

        public static List<IgnitionData> GetEnterpriseHistorianTags()
        {
            var count = 0;
            var query = @"select a.sqltag_id, a.Path, a.Name, a.DataType, 
  b.strval as OPCItemPath,
  c.strval as OPCServer,
  d.strval as HistoricalScanclass,
  e.strval as PrimaryHistoryProvider
from SQLTAG a 
left outer join SQLTAGPROP b on a.sqltag_id = b.tagid and b.name = 'OPCItemPath'
left outer join SQLTAGPROP c on a.sqltag_id = c.tagid and c.name = 'OPCServer'
left outer join SQLTAGPROP d on a.sqltag_id = d.tagid and d.name = 'HistoricalScanclass'
left outer join SQLTAGPROP e on a.sqltag_id = e.tagid and e.name = 'PrimaryHistoryProvider'
where a.enabled = 1 and e.strval = 'enterprise_historian'";
            var execQuery = new List<IgnitionData>();
            try
            {
                using (var connection = GetCopyReadConnection())
                {
                    connection.Open();
                    var command = new SQLiteCommand(query, connection);
                    var reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        execQuery.Add(new IgnitionData(reader));
                        count += 1;
                        if (count % 10 == 0)
                            Console.Write(":");
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error($"{e.Message}");
            }
            return execQuery;
        }

        public static List<IgnitionData> GetSplitterTags()
        {
            var count = 0;
            var query = @"select a.sqltag_id, a.Path, a.Name, a.DataType, 
  b.strval as OPCItemPath,
  c.strval as OPCServer,
  d.strval as HistoricalScanclass,
  e.strval as PrimaryHistoryProvider
from SQLTAG a 
left outer join SQLTAGPROP b on a.sqltag_id = b.tagid and b.name = 'OPCItemPath'
left outer join SQLTAGPROP c on a.sqltag_id = c.tagid and c.name = 'OPCServer'
left outer join SQLTAGPROP d on a.sqltag_id = d.tagid and d.name = 'HistoricalScanclass'
left outer join SQLTAGPROP e on a.sqltag_id = e.tagid and e.name = 'PrimaryHistoryProvider'
where a.enabled = 1 and c.strval is not null and e.strval = 'splitter' and c.strval <> 'Ignition OPC-UA Server'
";
            var execQuery = new List<IgnitionData>();
            try
            {
                using (var connection = GetCopyReadConnection())
                {
                    connection.Open();
                    var command = new SQLiteCommand(query, connection);
                    var reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        execQuery.Add(new IgnitionData(reader));
                        count += 1;
                        if (count % 10 == 0)
                            Console.Write(":");
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error($"{e.Message}");
            }
            return execQuery;
        }

        public static int ExecuteNonQuery(string query)
        {
            try
            {
                using (var connection = GetUpdateConnection())
                {
                    connection.Open();
                    var command = new SQLiteCommand(query, connection);
                    var nonQuery = command.ExecuteNonQuery();
                    return nonQuery;
                }
            }
            catch (Exception e)
            {
                Log.Error($"{e.Message}");
                Log.Error($"{query}");
                return 0;
            }
        }

        public static List<IgnitionFolder> GetFolders(string query)
        {
            var ignitionFolders = new List<IgnitionFolder>();
            try
            {
                using (var connection = GetCopyReadConnection())
                {
                    connection.Open();
                    var command = new SQLiteCommand(query, connection);
                    var reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        ignitionFolders.Add(new IgnitionFolder(reader));
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error($"{e.Message}");
                Log.Error($"{query}");
            }
            return ignitionFolders;
        }

        public static void CreateFolder(IgnitionFolder item)
        {
            var query = $@"with NextSqlTagIdInSeq as (SELECT MAX(val)+1 as TagId FROM SEQUENCES WHERE NAME='SQLTAG_SEQ'),
ProviderToUse as (SELECT MIN(SQLTAGPROVIDER_ID) as ProviderId FROM SQLTAGPROVIDER)
INSERT INTO SQLTAG(SQLTAG_ID,PROVIDERID,OWNERID,NAME,PATH,ENABLED,TAGTYPE,DATATYPE,ACCESSRIGHTS,SCANCLASS)
SELECT TagId, ProviderId, 0, '{item.Name}', '{item.Path}', 1, 'Folder', 'Int4', 'Read_Write', 'Default' FROM NextSqlTagIdInSeq, ProviderToUse;
UPDATE SEQUENCES SET val = val+1 WHERE NAME='SQLTAG_SEQ';";
            ExecuteNonQuery(query);
        }

        public static void CreateTag(OpcTagInfo opcTagInfo, HistoryProvider historyProvider, OpcUaServerConfig opcUaServer)
        {
            var query = $@"with NextSqlTagIdInSeq as (SELECT MAX(val)+1 as TagId FROM SEQUENCES WHERE NAME='SQLTAG_SEQ'),
            ProviderToUse as (SELECT MIN(SQLTAGPROVIDER_ID) as ProviderId FROM SQLTAGPROVIDER)
            INSERT INTO SQLTAG(SQLTAG_ID,PROVIDERID,OWNERID,NAME,PATH,ENABLED,TAGTYPE,DATATYPE,ACCESSRIGHTS,SCANCLASS)
            SELECT TagId, ProviderId, 0, '{OpcTagInfo.GetName(opcTagInfo, opcUaServer)}', '{OpcTagInfo.GetPath(opcTagInfo, opcUaServer, historyProvider)}', 1, 'OPC', '{opcTagInfo.DataType}', 'Read_Write', 'Default' FROM NextSqlTagIdInSeq, ProviderToUse;
            UPDATE SEQUENCES SET val = val+1 WHERE NAME='SQLTAG_SEQ';
            insert into SQLTAGPROP select val, 'HistoricalScanclass', null, '{historyProvider.ScanClass}', null, '', null, 0 from sequences where srctable='SQLTAG';
            insert into SQLTAGPROP select val, 'HistoryEnabled', 1, null, null, '', null, 0 from sequences where srctable='SQLTAG';
            insert into SQLTAGPROP select val, 'PrimaryHistoryProvider', null, '{historyProvider.Name}', null, '', null, 0 from sequences where srctable='SQLTAG';
            insert into SQLTAGPROP select val, 'HistoryMaxAgeMode', 4, null, null, '', null, 0 from sequences where srctable='SQLTAG';
            insert into SQLTAGPROP select val, 'HistoryMaxAge', 5, null, null, '', null, 0 from sequences where srctable='SQLTAG';
            insert into SQLTAGPROP select val, 'OPCItemPath', null, '{opcTagInfo.NodeId}', null, '', null, 0 from sequences where srctable='SQLTAG';
            insert into SQLTAGPROP select val, 'OPCServer', null, '{opcUaServer.Name}', null, '', null, 0 from sequences where srctable='SQLTAG';
            ";
            if (OpcTagInfo.GetName(opcTagInfo, opcUaServer).EndsWith("_mps"))
            {
                query += "insert into SQLTAGPROP select val, 'FormatString', null, '#,##0.#######', null, '', null, 0 from sequences where srctable='SQLTAG';";
                query += "insert into SQLTAGPROP select val, 'Deadband', null, null, '1.0e-07', '', null, 0 from sequences where srctable='SQLTAG';";
                query += "insert into SQLTAGPROP select val, 'HistoricalDeadband', null, null, '1.0e-07', '', null, 0 from sequences where srctable='SQLTAG';";
            }
            ExecuteNonQuery(query);
        }

        public static void DeleteFolder(IgnitionFolder item)
        {
            var query = $@"delete from sqltag where TagType = 'Folder' and Name = '{item.Name}' and Path = '{item.Path}'";
            ExecuteNonQuery(query);
        }

        public static bool DeleteTag(IgnitionData tag)
        {
            if (MariaDb.RecentTagData(tag))
            {
                Log.Info($"Found recent tag data for {tag.OpcItemPath} will not delete config.");
                return false;
            }
            Log.Info($"No matching OpcUa tag: Delete from Ignition {tag.OpcItemPath}");
            var query = $"delete from sqltagprop where tagid = {tag.SqlTagId};" +
                        $"delete from sqltag where sqltag_id = {tag.SqlTagId}";
            ExecuteNonQuery(query);
            return true;
        }

        public static void CreateRequiredScanClass()
        {
            try
            {
                using (var connection = GetUpdateConnection())
                {
                    connection.Open();
                    var cmd = connection.CreateCommand();
                    var query = "select count(*) from SQLTSCANCLASS where name = 'Minutes5'";
                    cmd.CommandText = query;
                    cmd.CommandTimeout = 90;
                    long count;
                    using (var reader = cmd.ExecuteReader())
                    {
                        reader.Read();
                        count = (long) reader[0];
                    }

                    if (count < 1)
                    {
                        Log.Info("Didn't find required scan class. Creating it now...");
                        var queryTxt =
                            @"with NextIdInSeq as (SELECT MAX(val)+1 as SeqId FROM SEQUENCES WHERE NAME='SQLTSCANCLASS_SEQ'), ProviderToUse as (SELECT MIN(SQLTAGPROVIDER_ID) as ProviderId FROM SQLTAGPROVIDER)
insert into SQLTSCANCLASS (sqltscanclass_id, providerid, name, lorate, hirate, drivingtagpath, comparison, comparevalue, mode, staletimeout, execflags, writetimeout)
select SeqId, ProviderId, 'Minutes5', 300000, 300000, '', 'Equal', 0, 'Direct', 3000000, 0, null from NextIdInSeq, ProviderToUse;
UPDATE SEQUENCES SET val = val+1 WHERE NAME='SQLTSCANCLASS_SEQ';";
                        var command = new SQLiteCommand(queryTxt, connection);
                        var nonQuery = command.ExecuteNonQuery();
                        if (nonQuery != 2)
                            Log.Error("That didn't work...");
                    }
                    else
                    { // This is here because I initially created the staletimeout too low. This could be removed in the future..
                        var queryTxt = @"update SQLTSCANCLASS set staletimeout = 3000000 where name = 'Minutes5' and staletimeout <> 3000000;
delete from SQLTSCANCLASS where name = 'minutes5' and lorate = 10000;
";
                        var command = new SQLiteCommand(queryTxt, connection);
                        var executeNonQuery = command.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception) { /* Ignored */ }
        }
    }
}