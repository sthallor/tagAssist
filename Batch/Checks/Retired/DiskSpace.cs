using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Management;
using System.Text.RegularExpressions;
using System.Threading;
using Common;
using Common.Extensions;
using Common.Models.Reporting;

namespace Batch.Checks.Retired
{
    public class DiskSpace
    {
        private static readonly string[] WarnErrorThreshold = ConfigurationManager.AppSettings["FreeSpace"].Split(',');
        private static readonly List<string> DrivesToCheck = new List<string> {"DriveLetter = 'C:'", "DriveLetter = 'D:'", "DriveLetter = 'E:'", "DriveLetter = 'F:'" };
        private const int UsingLessThanThisInMBmeansMoveDataDir = 5000;

        public bool Check(EgnServer egnServer)
        {
            string dataDir = GetDataDir(egnServer);

            var path = new ManagementPath {NamespacePath = @"root\cimv2", Server = egnServer.Server};
            var scope = new ManagementScope(path);

            foreach (var condition in DrivesToCheck)
            {
                string[] selectedProperties = { "FreeSpace", "Capacity" };
                var query = new SelectQuery("Win32_Volume", condition, selectedProperties);

                try
                {

                    using (var searcher = new ManagementObjectSearcher(scope, query))
                    using (var results = searcher.Get())
                    {
                        var volume = results.Cast<ManagementObject>().SingleOrDefault();
                        if (volume != null)
                        {
                            var freeSpace = (ulong) volume.GetPropertyValue("FreeSpace");
                            var capacity = (ulong) volume.GetPropertyValue("Capacity");
                            var message = $"{condition} {freeSpace.BytesToString()} free of {capacity.BytesToString()}";
                            var mbFree = freeSpace / 1024 / 1024;
                            var mbCapacity = capacity / 1024 / 1024;

                            if (!string.IsNullOrEmpty(dataDir) && dataDir.Substring(0, 2) != "C:" && condition.Contains(dataDir.Substring(0, 2)))
                            {
                                message += " Dedicated Data Partition.";
                                ProcessDataVolume(egnServer, dataDir, mbCapacity);
                            }

                            if (mbCapacity - mbFree < UsingLessThanThisInMBmeansMoveDataDir)
                            {
                                message += " We probably need to move MariaDB to this drive.";
                                EsiLog.Warn(egnServer, message, "Internal");
                            }
                            else if (mbFree < ulong.Parse(WarnErrorThreshold[1]))
                                EsiLog.HardError(egnServer, message, "Internal");
                            else if (mbFree < ulong.Parse(WarnErrorThreshold[0]))
                                EsiLog.Warn(egnServer, message, "Internal");
                            else
                                EsiLog.Info(egnServer, message);
                        }
                    }
                }
                catch (Exception)
                {
                }
            }
            return true;
        }

        private static string GetDataDir(EgnServer egnServer)
        {
            if (string.IsNullOrEmpty(egnServer.RigNumber))
                return "";
            //TODO: Standardize this? Can't be arsed right now.
            try
            {
                var try1 = Retry.Do(() => GetDataDir(egnServer, $@"\\{egnServer.Server}\c$\Program Files\MariaDB 10.1\data\my.ini"), TimeSpan.FromSeconds(5));
                if (!string.IsNullOrEmpty(try1))
                    return try1;
            }
            catch (Exception) { /* ignored */ }

            try
            {
                var try2 = Retry.Do(() => GetDataDir(egnServer, $@"\\{egnServer.Server}\e$\MariaDB\my.ini"), TimeSpan.FromSeconds(5));
                if (!string.IsNullOrEmpty(try2))
                    return try2;
            }
            catch (Exception) { /* ignored */ }

            try
            {
                var try3 = Retry.Do(() => GetDataDir(egnServer, $@"\\{egnServer.Server}\d$\MariaDB\my.ini"), TimeSpan.FromSeconds(5));
                if (!string.IsNullOrEmpty(try3))
                    return try3;
            }
            catch (Exception) { /* ignored */ }

            try
            {
                var try4 = Retry.Do(() => GetDataDir(egnServer, $@"\\{egnServer.Server}\d$\MySqlData\my.ini"), TimeSpan.FromSeconds(5));
                if (!string.IsNullOrEmpty(try4))
                    return try4;
            }
            catch (Exception) { /* ignored */ }

            try
            {
                var try5 = Retry.Do(() => GetDataDir(egnServer, $@"\\{egnServer.Server}\f$\MySqlData\my.ini"), TimeSpan.FromSeconds(5));
                if (!string.IsNullOrEmpty(try5))
                    return try5;
            }
            catch (Exception) { /* ignored */ }

            if (egnServer.IgnitionController.IsLoggedIn())
            {
                EsiLog.HardError(egnServer, "Could not locate MySQL data directory", "Internal");
            }
            return "";
        }

        private static string GetDataDir(EgnServer egnServer, string file)
        {
            string dataDir;
            try
            {
                using (FileStream stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        var content = reader.ReadToEnd();
                        var strings = Regex.Split(content, @"\r?\n|\r").ToList();
                        dataDir = strings.FirstOrDefault(x => x.StartsWith("datadir")).Split('=')[1];
                    }
                }
                //dataDir = File.ReadAllLines(directory).FirstOrDefault(x => x.StartsWith("datadir")).Split('=')[1];
            }
            catch (Exception e)
            {
                if (egnServer.IgnitionController.IsLoggedIn() && !e.Message.StartsWith("Could not find a part of the path"))
                {
                    EsiLog.Info(egnServer, $"DiskSpace: {e.Message}");
                }
                throw;
            }
            return dataDir;
        }

        private static void ProcessDataVolume(EgnServer egnServer, string dataDir, ulong mbCapacity)
        {
            if (string.IsNullOrEmpty(egnServer.RigNumber))
                return;

            var directoryInfo = new DirectoryInfo($@"\\{egnServer.Server}\{dataDir.Substring(0, 1)}$\{dataDir.Substring(3)}\Ignition\");
            var files = directoryInfo.GetFiles("sqlt_data*.ibd");
            var largestFile = files.OrderByDescending(x => x.Length).FirstOrDefault();
            var largestInMb = (ulong) (largestFile.Length / 1024 / 1024);

            var maxPartitions = mbCapacity / largestInMb;
            var pruneAge = egnServer.IgnitionController.GetPruneAge();
            if (pruneAge != 0 && maxPartitions != (ulong) (pruneAge + 1))
                EsiLog.Warn(egnServer, $"Set prune age to {maxPartitions - 1}. Currently set to {pruneAge}. Largest {((ulong)largestFile.Length).BytesToString()}", "Internal");
        }
    }


    public static class Retry
    {
        public static void Do(Action action, TimeSpan retryInterval, int maxAttemptCount = 3)
        {
            Do<object>(() =>
            {
                action();
                return null;
            }, retryInterval, maxAttemptCount);
        }

        public static T Do<T>(Func<T> action, TimeSpan retryInterval, int maxAttemptCount = 3)
        {
            var exceptions = new List<Exception>();

            for (int attempted = 0; attempted < maxAttemptCount; attempted++)
            {
                try
                {
                    if (attempted > 0)
                    {
                        Thread.Sleep(retryInterval);
                    }
                    return action();
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }
            throw new AggregateException(exceptions);
        }
    }
}