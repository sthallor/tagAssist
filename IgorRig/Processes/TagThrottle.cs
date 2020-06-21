using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Common.Database;
using IgorRig.Misc;
using IgorRig.Models;
using log4net;
using Newtonsoft.Json;

namespace IgorRig.Processes
{
    public class TagThrottle
    {
        private const string ConfigDir = @"C:\Installs\IgorConfig\Common\TagThrottlingConfig";
        public static readonly string VersionFile = @"C:\Program Files\Igor\ThrottleVersion.txt";
        private static readonly TimeSpan RepeatCheckEvery = TimeSpan.FromMinutes(3);
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private static bool _restartRequired;
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
                    ConfigDb.CreateRequiredScanClass();
                    if (GetAvailableVersion() != null && GetAppliedVersion() != GetAvailableVersion())
                    {
                        Log.Info("Found new throttle file version to apply.");
                        if (ConfigDb.GetSplitterTags().Any()) // Don't process throttling file unless we've got rid of the old splitter configuration.
                        {
                            Log.Warn("Still splitter tags present. Do not proceed with throttle file.");
                            Thread.Sleep(TimeSpan.FromHours(2));
                            continue;
                        }
                        Apply();
                        RestartService();
                        UpdateThrottleVersion();
                    }
                    Thread.Sleep(RepeatCheckEvery);
                } while (true);
            }
            catch (Exception e)
            {
                Log.Error(e.Message);
                Log.Error(e.ToString());
                RigSingleton.Instance.SendMessage($"Failed TagThrottle {e.Message}");
            }
        }

        private static void Apply()
        {
            var throttleFile = JsonConvert.DeserializeObject<List<ThrottleFile>>(File.ReadAllText($@"{ConfigDir}\tagList.json"));
            var historianTags = ConfigDb.GetEnterpriseHistorianTags();
            var slow = 0;
            var speed = 0;
            var inserts = 0;
            foreach (var tag in historianTags)
            {
                var updated = false;
                foreach (var scanClass in throttleFile)
                {
                    if (scanClass.TagList.Any(x => x.ToLower() == tag.TagPath.ToLower()))
                    {
                        updated = true;
                        if (tag.HistoricalScanclass != GetScanClass(scanClass.ScanClass))
                        {
                            speed += 1;
                            _restartRequired = true;
                            ConfigDb.ExecuteNonQuery($@"update sqltagprop set strval = '{GetScanClass(scanClass.ScanClass)}' where tagid = {tag.SqlTagId} and name = 'HistoricalScanclass'");
                        }
                    }
                }

                if (!updated && tag.HistoricalScanclass != ConfigDb.DefaultScanClass)
                {
                    slow += 1;
                    _restartRequired = true;
                    var query = $@"update sqltagprop set strval = '{ConfigDb.DefaultScanClass}' where tagid = {tag.SqlTagId} and name = 'HistoricalScanclass'";
                    var nonQuery = ConfigDb.ExecuteNonQuery(query);
                    if (nonQuery == 0)
                    {
                        inserts += 0;
                        query = $@"insert into SQLTAGPROP select {tag.SqlTagId}, 'HistoricalScanclass', null, '{ConfigDb.DefaultScanClass}', null, '', null, 0";
                        nonQuery = ConfigDb.ExecuteNonQuery(query);
                        if (nonQuery == 0)
                        {
                            Log.Error("Did not update or insert correctly.");
                        }
                    }
                }
            }
            var message = "Throttle adjustment:";
            if (speed != 0) message += $" 🏃💨 Sped up {speed} tags.";
            if (slow != 0) message += $" 🐌 Slowed down {slow} tags.";
            if (inserts != 0) message += $" 🛠️ Fixed scanclass for {inserts} tags.";
            if (speed != 0 || slow != 0 || inserts != 0)
            {
                Log.Info(message);
                RigSingleton.Instance.SendMessage(message);
            }
        }

        private static string GetScanClass(string scanClass)
        {
            switch (scanClass)
            {
                case "10s":
                    return "Default Historical";
                case "1s":
                    return "Default";
                default:
                    throw new NotImplementedException();
            }
        }

        private static void RestartService()
        {
            if (_restartRequired)
            {
                Log.Info("Stopping Ignition service...");
                RigSingleton.Instance.EgnServer.ServiceController.Stop("Ignition");
                Thread.Sleep(TimeSpan.FromMinutes(2));
                Log.Info("Starting Ignition service...");
                RigSingleton.Instance.EgnServer.ServiceController.Start("Ignition");
            }
        }

        private static string GetAvailableVersion()
        {
            try
            {
                return Md5Folder(ConfigDir);
            }
            catch (Exception) { return null; }
        }

        private static string GetAppliedVersion()
        {
            try
            {
                var readAllText = File.ReadAllText(VersionFile);
                return readAllText;
            }
            catch (Exception) { return ""; }
        }
        
        private static void UpdateThrottleVersion()
        {
            var version = GetAvailableVersion();
            File.WriteAllText(VersionFile, version);
        }

        public static void ForceThrottleCheck()
        {
            File.WriteAllText(VersionFile, "");
        }

        public static string Md5Folder(string path)
        {
            var files = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories).OrderBy(p => p).ToList();
            var md5 = MD5.Create();
            for (var i = 0; i < files.Count; i++)
            {
                var file = files[i];
                // hash path
                var relativePath = file.Substring(path.Length + 1);
                var pathBytes = Encoding.UTF8.GetBytes(relativePath.ToLower());
                md5.TransformBlock(pathBytes, 0, pathBytes.Length, pathBytes, 0);
                // hash contents
                var contentBytes = File.ReadAllBytes(file);
                if (i == files.Count - 1)
                    md5.TransformFinalBlock(contentBytes, 0, contentBytes.Length);
                else
                    md5.TransformBlock(contentBytes, 0, contentBytes.Length, contentBytes, 0);
            }
            return BitConverter.ToString(md5.Hash).Replace("-", "").ToLower();
        }
    }
}