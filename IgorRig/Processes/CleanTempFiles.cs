using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using IgorRig.Misc;
using log4net;

namespace IgorRig.Processes
{
    public class CleanTempFiles 
    {
        private static readonly TimeSpan RepeatCheckEvery = TimeSpan.FromDays(1);
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

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
                    //Path, Days old before removal.
                    var directories = new List<Tuple<string, int>>
                    {
                        new Tuple<string, int>(@"C:\Analytics\Rcode\WellData_RealTime\RTclassify", -5)
                    };
                    foreach (var directory in directories)
                    {
                        DeleteOldFilesInDir(directory);
                    }
                    Thread.Sleep(RepeatCheckEvery);
                } while (true);
            }
            catch (Exception e)
            {
                Log.Error(e.Message);
                Log.Error(e.ToString());
                RigSingleton.Instance.SendMessage($"CleanTempFiles process abend. {e.Message}");
            }
        }

        private static void DeleteOldFilesInDir(Tuple<string, int> directory)
        {
            if (!Directory.Exists(directory.Item1)) return;
            var files = Directory.GetFiles(directory.Item1);
            long bytesDeleted = 0;
            foreach (var file in files)
            {
                var fi = new FileInfo(file);
                if (fi.LastAccessTime >= DateTime.Now.AddDays(directory.Item2)) continue;
                try
                {
                    fi.Delete();
                    bytesDeleted += fi.Length;
                }
                catch (Exception) { /* ignored */ }
            }
            Log.Info($"Removed {bytesDeleted.BytesToString()} from temp directory.");
        }
    }
}