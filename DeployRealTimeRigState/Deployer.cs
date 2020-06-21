using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity.Migrations;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Common.Controllers;
using Common.Database;
using Common.Models.Igor;
using Common.Models.Reporting;
using DbUp;
using DeployRealTimeRigState.Misc;
using log4net;

namespace DeployRealTimeRigState
{
    public class Deployer
    {
        private EgnServer EgnServer { get; }
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private string _existingVersion;
        private string _rVersionInstalled;

        public Deployer(EgnServer egnServer)
        {
            EgnServer = egnServer;
            Log.Info($"{EgnServer.Server} ({EgnServer.RigNumber})...");
        }
        public void Execute()
        {
            try
            {
                CheckIgnitionTags();
                DeployR();
                DeployOdbc();
                UpgradeDatabase();
                DeployLatestCode();
                DeployLatestData();
            }
            catch (Exception) { /* Ignored */ }
        }

        private void CheckIgnitionTags()
        {
            try
            {
                Log.Info("Checking for RIG_STATE_DATASET SQLTAG record.");
                var execQuery = ExecQuery("select * from SQLTAG where name = 'RIG_STATE_DATASET'");
                if (execQuery.Count == 0)
                {
                    Log.Info("Missing required RIG_STATE_DATASET SQLTAG record.");
                    var serviceController = new ServiceController(EgnServer);
                    serviceController.Stop("Ignition");
                    Log.Info("Inserting required SQLTAG/SQLTAGPROP records.");
                    ExecSql(
                        @"update sequences set val=val+1 where srctable='SQLTAG';

with ProviderToUse as (SELECT MIN(SQLTAGPROVIDER_ID) as ProviderId FROM SQLTAGPROVIDER)
insert into SQLTAG select val,1,0,'EDGE_ANALYTICS','', ProviderId,'Folder','Int4','Read_Write','Default' from sequences, ProviderToUse where srctable='SQLTAG';
update sequences set val=val+1 where srctable='SQLTAG';

with ProviderToUse as (SELECT MIN(SQLTAGPROVIDER_ID) as ProviderId FROM SQLTAGPROVIDER)
insert into SQLTAG select val,1,0,'RIG_STATE_DATASET','EDGE_ANALYTICS',ProviderId,'DB','DataSet','Read_Write','Default Historical' from sequences, ProviderToUse where srctable='SQLTAG';
insert into SQLTAGPROP select val,'Expression',null,'CALL `get_most_recent_rigstate_tagdata`()',null,'',null,0 from sequences where srctable='SQLTAG';
insert into SQLTAGPROP select val,'ExpressionType',2,null,null,'',null,0 from sequences where srctable='SQLTAG';
insert into SQLTAGPROP select val,'QueryType',1,null,null,'',null,0 from sequences where srctable='SQLTAG';
insert into SQLTAGPROP select val,'SQLBindingDatasource',null,'local',null,'',null,0 from sequences where srctable='SQLTAG';

insert into SQLTAGEVENTSCRIPTS select val,'valueChanged', '	if previousValue.value is not None and currentValue.value.getValueAt(0,1) != previousValue.value.getValueAt(0,1):
		value = currentValue.value.getValueAt(0,0)
		priorValue = previousValue.value.getValueAt(0,0)
		date = system.date.fromMillis(currentValue.value.getValueAt(0,1))
		quality = 192
		paths = [''edge_analytics/rig_state'']
		values=[value]
		qualities=[quality]
		dates = [date]
		tagProvider = ''default''
		historyProvider = ''splitter''
		system.tag.storeTagHistory(historyProvider,tagProvider,paths,values,qualities,dates)', '1', '' from sequences where srctable='SQLTAG';");
                    serviceController.Start("Ignition");
                }
                else
                {
                    Log.Info("Found the required RIG_STATE_DATASET SQLTAG record.");
                }
            }
            catch (Exception)
            {
            }
        }

        private string GetTrainingDataPattern()
        {
            var readAllText = File.ReadAllLines($@"\\cal0-vp-ace01\e$\Analytics\Rcode\{Program.LatestVersion}\parms\prod\classify\trainRT.prm").FirstOrDefault(x=> x.StartsWith("Training Data Filename"));
            var last = readAllText.Split('/').Last();
            return last.Substring(0, last.IndexOf("wells") - 1) + "*";
        }

        private void DeployLatestData()
        {
            var path = $@"\\{EgnServer.Server}\c$\Analytics\Rcode\TrainData\";
            Directory.CreateDirectory(path);
            var strings = Directory.GetFiles(path, GetTrainingDataPattern());
            if (strings.Length > 0)
            {
                Log.Info($"Found {strings.Length} existing training data files matching this version of RCode.");
            }
            else
            {
                Log.Info("Compressing training data...");
                Utility.ZipFilesMatchingPattern(@"\\cal0-vp-ace01\e$\Analytics\Rcode\tempData.zip", @"\\cal0-vp-ace01\e$\Analytics\Rcode\TrainData\", GetTrainingDataPattern());
                Log.Info("Copying training data...");
                var source = new FileInfo(@"\\cal0-vp-ace01\e$\Analytics\Rcode\tempData.zip");
                var destination = new FileInfo($@"\\{EgnServer.Server}\c$\Analytics\Rcode\TrainData\tempData.zip");
                if (destination.Exists) destination.Delete();
                source.CopyTo(destination, x => Console.Write($"{x}% Complete\b\b\b\b\b\b\b\b\b\b\b\b"));
                Console.WriteLine("100% Complete");
                Log.Info("Decompressing training data...");
                Utility.Execute(@"""C:\Program Files\7-Zip\7z.exe"" x C:\Analytics\Rcode\TrainData\tempData.zip -oC:\Analytics\Rcode\TrainData\ -y", EgnServer.Server);
            }
        }

        private void DeployLatestCode()
        {
            var dest = $@"\\{EgnServer.Server}\c$\Analytics\Rcode\{Program.LatestVersion}";
            // No version installed, or newer one exists. And.. redundantly; does directory not already exist?
            if (GetExistingVersion() == null || GetExistingVersion() != Program.LatestVersion || GetExistingVersionMd5() != Program.Md5ForFolder)
            {
                Log.Info("Found update to RCode.");
                Log.Info($"Upgrading from version {GetExistingVersion()} {GetExistingVersionMd5()} to {Program.LatestVersion} {Program.Md5ForFolder}.");
                Utility.CloneDirectory(Program.SourceDir, @"\\cal0-vp-ace01\e$\Analytics\Rcode\temp");
                Log.Info("Splicing ensign specific paths...");
                ReplaceContents(@"\\cal0-vp-ace01\e$\Analytics\Rcode\temp");
                Log.Info("Compressing RCode.");
                Utility.CreateZip(@"\\cal0-vp-ace01\e$\Analytics\Rcode\temp.zip", @"\\cal0-vp-ace01\e$\Analytics\Rcode\temp");
                Directory.CreateDirectory(dest);
                var source = new FileInfo(@"\\cal0-vp-ace01\e$\Analytics\Rcode\temp.zip");
                var destFileName = new FileInfo(dest + @"\temp.zip");
                Log.Info("Copying RCode to EGN.");
                source.CopyTo(destFileName, x => Console.Write($"{x}% Complete\b\b\b\b\b\b\b\b\b\b\b\b"));
                Console.WriteLine("100% Complete");
                Log.Info("Decompressing RCode.");
                Utility.Execute($@"""C:\Program Files\7-Zip\7z.exe"" x C:\Analytics\Rcode\{Program.LatestVersion}\temp.zip -oC:\Analytics\Rcode\{Program.LatestVersion}\ -y", EgnServer.Server);
                _existingVersion = null;
                Directory.CreateDirectory($@"\\{EgnServer.Server}\c$\Analytics\Rcode\WellData_RealTime\RTclassify\");
                Directory.CreateDirectory($@"\\{EgnServer.Server}\c$\Analytics\Rcode\WellData_RealTime\RTpredict\");

                var serviceController = new ServiceController(EgnServer);
                serviceController.Stop("Igor");
                Thread.Sleep(10000);
                serviceController.Start("Igor");
                SetVersionUpdated();
            }
            else
            {
                Log.Info($"Existing RCode version {GetExistingVersion()} {GetExistingVersionMd5()} is up to date.");
            }
        }

        private void SetVersionUpdated()
        {
            var igorDb = new IgorDb();
            var version = new RealTimeRigStateVersion
            {
                Rig = EgnServer.RigNumber,
                Server = EgnServer.Server,
                Version = Program.LatestVersion,
                MD5 = Program.Md5ForFolder
            };
            igorDb.RtrsVersion.AddOrUpdate(version);
            igorDb.SaveChanges();
        }

        private string GetExistingVersionMd5()
        {
            var igorDb = new IgorDb();
            var realTimeRigStateVersion = igorDb.RtrsVersion.Find(EgnServer.RigNumber);
            return realTimeRigStateVersion?.MD5;
        }

        private string GetExistingVersion()
        {
            var path = $@"\\{EgnServer.Server}\c$\Analytics\Rcode\";
            Directory.CreateDirectory(path);
            return _existingVersion ?? (_existingVersion = Directory.GetDirectories(path, "*_master").OrderByDescending(x => x).FirstOrDefault()?.Split('\\').Last());
        }

        private void UpgradeDatabase()
        {
            var connectionString = $"SERVER={EgnServer.Server};DATABASE=Ignition;UID=root;PASSWORD=ensignDatabase;Allow User Variables=True";
            var upgrader = DeployChanges.To.MySqlDatabase(connectionString).WithScriptsFromFileSystem(@"..\..\Scripts").LogToAutodetectedLog().Build();
            upgrader.PerformUpgrade();
            //TODO: Add rig specific db changes
        }

        private static void ReplaceContents(string sDir)
        {
            foreach (var dir in Directory.GetDirectories(sDir))
            {
                foreach (var path in Directory.GetFiles(dir).Where(x=> !x.Contains("~")))
                {
                    var text = File.ReadAllText(path);
                    text = text.Replace(@"C:\Program Files\R\R-3.2.2\bin\x64\Rscript.exe", @"C:\Program Files\R\R-3.4.2\bin\x64\Rscript.exe");
                    text = text.Replace(@"E:/Analytics/Rcode/", @"C:/Analytics/Rcode/");
                    text = text.Replace(@"C:/Analytics/Rcode/TestData/", "C:/Analytics/Rcode/WellData_RealTime/");
                    text = text.Replace(@"'parms/dev/", "'parms/prod/");
                    text = text.Replace(@"Real Time ODBC Connect Name             [rig550]", "Real Time ODBC Connect Name             [Ignition]");
                    text = text.Replace(@"Real Time ODBC Connect Name             [rig140]", "Real Time ODBC Connect Name             [Ignition]");
                    text = text.Replace(@"Filename for log .txt output (blank for none)                     [rig778_log.txt]", "Filename for log .txt output (blank for none)                     [rig_log.txt]");
                    text = text.Replace(@"Filename for run to run summary log .csv output (blank for none)  [rig778_runlog.csv]", "Filename for run to run summary log .csv output (blank for none)  [rig_runlog.csv]");
                    text = text.Replace(@"Filename for results .csv output (blank for none)                 [rig778_results.csv]", "Filename for results .csv output (blank for none)                 [rig_results.csv]");
                    text = text.Replace(@"Filename for diagnostics .csv output (blank for none)             [rig778_diagnostics.csv]", "Filename for diagnostics .csv output (blank for none)             [rig_diagnostics.csv]");
                    text = text.Replace(@"Filename for log .txt output (blank for none)         [rig140_log.txt]", "Filename for log .txt output (blank for none)         [rig_log.txt]");
                    text = text.Replace(@"Filename for results .csv output (blank for none)     [rig140_results.csv]", "Filename for results .csv output (blank for none)     [rig_results.csv]");
                    text = text.Replace(@"Filename for diagnostics .csv output (blank for none) [rig140_diagnostics.csv]", "Filename for diagnostics .csv output (blank for none) [rig_diagnostics.csv]");
                    text = text.Replace(@"Elapsed time program duration (hours)                             [0.02]", "Elapsed time program duration (hours)                             [0.99]");
                    text = text.Replace(@"Elapsed time program duration (hours)                             [.02]", "Elapsed time program duration (hours)                             [0.99]");
                    text = text.Replace(@"Verbose log file output (Yes/No)                                  [No]", @"Verbose log file output (Yes/No)                                  [Yes]");
                    text = text.Replace(@"Verbose logfile output (Yes/No)                             [No]", @"Verbose logfile output (Yes/No)                             [Yes]");
                    text = text.Replace(@"Append time stamp to output filenames (Yes/No)                    [No]", @"Append time stamp to output filenames (Yes/No)                    [Yes]");
                    File.WriteAllText(path, text);
                }
                ReplaceContents(dir);
            }
        }

        private void DeployR()
        {
            if (!Directory.Exists($@"\\{EgnServer.Server}\c$\Program Files\R"))
            {
                Log.Info("Existing R installation not found.");
            }
            else
            {
                _rVersionInstalled = Directory.GetDirectories($@"\\{EgnServer.Server}\c$\Program Files\R").OrderByDescending(x => x).FirstOrDefault()?.Split('\\').Last().Split('-').Last();
                Log.Info($"Found verion {_rVersionInstalled} of R installed.");
            }
            var rVersion = ConfigurationManager.AppSettings["R_Version"];

            Version installedR = null;
            if (_rVersionInstalled != null)
            {
                installedR = new Version(_rVersionInstalled);
            }
            var availableR = new Version(rVersion);

            if (installedR == null || availableR > installedR)
            {
                var rDir = $@"\\{EgnServer.Server}\c$\Installs\";
                Directory.CreateDirectory(rDir);
                var destination = $@"{rDir}{ConfigurationManager.AppSettings["R_InstallFile"]}";
                if (!File.Exists(destination))
                {
                    Log.Info($@"Copying {ConfigurationManager.AppSettings["R_InstallFile"]} to \\{EgnServer.Server}\c$\Installs\");
                    var sourceFileName = $@"{ConfigurationManager.AppSettings["AceServerInstallsDirectory"]}{ConfigurationManager.AppSettings["R_InstallFile"]}";
                    var source = new FileInfo(sourceFileName);
                    var destFile = new FileInfo(destination);
                    source.CopyTo(destFile, x => Console.Write($"{x}% Complete\b\b\b\b\b\b\b\b\b\b\b\b"));
                    Console.WriteLine("100% Complete");
                }
                var command = $@"C:\Installs\{ConfigurationManager.AppSettings["R_InstallFile"]} /VERYSILENT";
                Log.Info($"Executing command {command}");
                Utility.Execute(command, EgnServer.Server);
            }
        }

        private void DeployOdbc()
        {
            const string visualC = "vcredist_x64.exe";
            var source1 = new FileInfo($@"{ConfigurationManager.AppSettings["AceServerInstallsDirectory"]}{visualC}");
            var destination1 = new FileInfo($@"\\{EgnServer.Server}\c$\Installs\{visualC}");
            if (File.Exists(destination1.ToString()))
            {
                Log.Info("Found Microsoft Visual C++ 2013 Redistributable Package (x64)");
            }
            else
            {
                Log.Info("Copying Microsoft Visual C++ 2013 Redistributable Package(x64)...");
                source1.CopyTo(destination1, x => Console.Write($"{x}% Complete\b\b\b\b\b\b\b\b\b\b\b\b"));
                Console.WriteLine("100% Complete");
                Log.Info("Starting Visual C++ 2013 redist silent install..");
                Utility.Execute($@"C:\Installs\{visualC} /install /quiet /norestart", EgnServer.Server);
            }

            const string odbcDriver = "mysql-connector-odbc-5.3.11-winx64.msi";
            var source2 = new FileInfo($@"{ConfigurationManager.AppSettings["AceServerInstallsDirectory"]}{odbcDriver}");
            var destination2 = new FileInfo($@"\\{EgnServer.Server}\c$\Installs\{odbcDriver}");
            if (File.Exists(destination2.ToString()))
            {
                Log.Info("Found MySQL ODBC Connector");
            }
            else
            {
                Log.Info("Copying MySQL ODBC Connector...");
                source2.CopyTo(destination2, x => Console.Write($"{x}% Complete\b\b\b\b\b\b\b\b\b\b\b\b"));
                Console.WriteLine("100% Complete");
                Log.Info("Starting MySQL ODBC Connector silent install..");
                Utility.Execute($@"msiexec /i C:\Installs\{odbcDriver} /qn", EgnServer.Server);
            }

            const string odbcRegistry = "IgnitionODBC.reg";
            var source3 = new FileInfo($@"{ConfigurationManager.AppSettings["AceServerInstallsDirectory"]}{odbcRegistry}");
            var destination3 = new FileInfo($@"\\{EgnServer.Server}\c$\Installs\{odbcRegistry}");
            if (File.Exists(destination3.ToString()))
            {
                Log.Info("Found Ignition ODBC Connection definition.");
            }
            else
            {
                Log.Info("Copying Ignition ODBC connection definition...");
                source3.CopyTo(destination3, x => Console.Write($"{x}% Complete\b\b\b\b\b\b\b\b\b\b\b\b"));
                Console.WriteLine("100% Complete");
                Log.Info("Importing registry script to define Ignition ODBC connection.");
                Utility.Execute($@"regedit /s C:\Installs\{odbcRegistry}", EgnServer.Server);
            }
        }

        private void ExecSql(string query)
        {
            try
            {
                using (var connection = new SQLiteConnection($@"Data Source=\\\\{EgnServer.Server}\c$\Program Files\Inductive Automation\Ignition\data\db\config.idb;Version=3", true))
                {
                    connection.Open();
                    var sqLiteCommand = new SQLiteCommand(query, connection);
                    var command = sqLiteCommand;
                    command.ExecuteNonQuery();
                }
            }
            catch (Exception e)
            {
                Log.Error($"{EgnServer.Server} ({EgnServer.RigNumber}) Unable to complete ExecSql step.");
                Log.Error($"{EgnServer.Server} ({EgnServer.RigNumber}) {e.Message}");
                throw;
            }
        }

        private List<string> ExecQuery(string query)
        {
            var queryOutput = new List<string>();
            try
            {
                using (var connection = new SQLiteConnection(ConfigDb.GetConnectionRefreshEnterprise(EgnServer), true))
                {
                    connection.Open();
                    var command = new SQLiteCommand(query, connection);
                    var reader = command.ExecuteReader();
                    var columns = Enumerable.Range(0, reader.FieldCount).Select(reader.GetName).ToList();
                    while (reader.Read())
                    {
                        var line = $"{EgnServer.Server},{EgnServer.RigNumber},";
                        foreach (var s in columns)
                        {
                            line += $"{reader[s.Trim()]},";
                        }
                        queryOutput.Add($"{line.Remove(line.Length - 1)}");
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error($"{EgnServer.Server} ({EgnServer.RigNumber}) Unable to complete ExecSql step.");
                Log.Error($"{EgnServer.Server} ({EgnServer.RigNumber}) {e.Message}");
                throw;
            }
            return queryOutput;
        }
    }
}