using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using IgorRig.Misc;
using log4net;

namespace IgorRig.Processes
{
    internal class DiskSpace
    {
        private static readonly string[] WarnErrorThreshold = ConfigurationManager.AppSettings["FreeSpace"].Split(',');
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private const int PruneAgeLargeEnough = 21; // If prune age is at least this large, don't ask to increase size.
        private static readonly TimeSpan RepeatCheckEvery = TimeSpan.FromHours(4);

        public static void Run()
        {
            var thread = new Thread(Check);
            thread.Start();
        }
        public static void Check()
        {
            try
            {
                do
                {
                    var dataDir = GetDataDir();
                    Log.Info($"DataDir = {dataDir}");
                    foreach (var drive in DriveInfo.GetDrives().Where(x => x.IsReady && x.DriveType == DriveType.Fixed))
                    {
                        Log.Info(drive.Name);
                        var message = $"{drive.Name} {drive.TotalFreeSpace.BytesToString()} free of {drive.TotalSize.BytesToString()}";
                        var mbFree = drive.TotalFreeSpace / 1024 / 1024;
                        var mbCapacity = drive.TotalSize / 1024 / 1024;

                        if (!string.IsNullOrEmpty(dataDir) && dataDir.Substring(0, 2) != "C:" && drive.Name != @"C:\")
                        {
                            message += " Has Dedicated Data Partition.";
                            ProcessDataVolume(dataDir, mbCapacity);
                            if (dataDir.StartsWith("C"))
                            {
                                message += " We probably need to move MariaDB to this drive.";
                                RigSingleton.Instance.SendMessage(message);
                            }
                        }
                        else if (mbFree < long.Parse(WarnErrorThreshold[1]))
                        {
                            RigSingleton.Instance.SendMessage(message);
                        }
                        else if (mbFree < long.Parse(WarnErrorThreshold[0]))
                        {
                            RigSingleton.Instance.SendMessage(message);
                        }
                        Log.Info(message);
                    }
                    Thread.Sleep(RepeatCheckEvery);
                } while (true);
            }
            catch (Exception e)
            {
                Log.Error(e.Message);
                Log.Error(e.ToString());
                RigSingleton.Instance.SendMessage($"DiskSpace process abend. {e.Message}");
            }
        }

        private static string GetDataDir()
        {
            var paths = new List<string>
            {
                @"C:\MySqlData\my.ini",
                @"D:\MySqlData\my.ini",
                @"E:\MySqlData\my.ini",
                @"F:\MySqlData\my.ini"
            };
            foreach (var path in paths)
            {
                if (File.Exists(path))
                {
                    var dataDir = GetDataDir(path);
                    return dataDir;
                }
            }
            RigSingleton.Instance.SendMessage("Could not locate MySQL data directory");
            return "";
        }

        private static string GetDataDir(string file)
        {
            string dataDir = null;
            try
            {
                using (var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    using (var reader = new StreamReader(stream))
                    {
                        var content = reader.ReadToEnd();
                        var strings = Regex.Split(content, @"\r?\n|\r").ToList();
                        dataDir = strings.FirstOrDefault(x => x.StartsWith("datadir"))?.Split('=')[1];
                    }
                }
            }
            catch (Exception) { /* ignored */ }
            return dataDir;
        }

        private static void ProcessDataVolume(string dataDir, long mbCapacity)
        {
            var directoryInfo = new DirectoryInfo($@"{dataDir}\Ignition\");
            var files = directoryInfo.GetFiles("sqlt_data*.ibd");
            var largestFile = files.OrderByDescending(x => x.Length).FirstOrDefault();
            var largestInMb = largestFile.Length / 1024 / 1024;
            if(largestInMb == 0)
                return;
            var maxPartitions = mbCapacity / largestInMb;
            if (maxPartitions > PruneAgeLargeEnough)
            {
                maxPartitions = PruneAgeLargeEnough;
            }

            try
            {
                RigSingleton.Instance.EgnServer.Init();
                var pruneAge = RigSingleton.Instance.EgnServer.IgnitionController.GetPruneAge();
                if (pruneAge != 0 && maxPartitions != pruneAge + 1 && pruneAge < PruneAgeLargeEnough)
                {
                    var message = $"Set prune age to {maxPartitions - 1}. Currently set to {pruneAge}. Largest {largestFile.Length.BytesToString()}";
                    RigSingleton.Instance.SendMessage(message);
                }
            }
            catch (Exception) { /* Ignored */ }
        }
    }
}