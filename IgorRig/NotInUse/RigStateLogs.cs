using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using IgorRig.Misc;
using log4net;

namespace IgorRig.NotInUse
{
    public class RigStateLogs
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private static readonly TimeSpan RepeatCheckEvery = TimeSpan.FromMinutes(30);
        private const string Format = "yyyy-MM-dd hh:mm:ss";

        public static void Run()
        {
            if (!RigSingleton.Instance.GetHistorianConfig().HistEnvironment.RealTimeRigState) return;
            Log.Info("Starting real time rig state classifier process...");
            var thread = new Thread(Check);
            thread.Start();
        }

        private static void Check()
        {
            Thread.Sleep(10000);
            try
            {
                do
                {
                    var readAllLines = ReadLinesReverse(@"C:\Analytics\Rcode\WellData_RealTime\RTclassify\rig_log.txt");
                    foreach (var line in readAllLines)
                    {
                        if (line.Contains("FATAL ERROR in dbuild ... missing predictor variables ..."))
                        {
                            Log.Error("Rig State log shows 'FATAL ERROR in dbuild ... missing predictor variables ...'");
                            return;
                        }
                        try
                        {
                            var date = line.Substring(0, 19);
                            DateTime.ParseExact(date, Format, CultureInfo.InvariantCulture);
                            Log.Info($"Rig State Log shows successful classification {date}");
                            return;
                        }
                        catch (Exception) { /* ignored */ }
                    }
                    Log.Warn("Finished reading file and rig state is non-determined. Maybe start of log?");
                    Thread.Sleep(RepeatCheckEvery);
                } while (true);
            }
            catch (Exception e)
            {
                Log.Error(e.Message);
                Log.Error(e.ToString());
                RigSingleton.Instance.SendMessage($"RigStateLog process abend. {e.Message}");
            }
        }

        public static List<string> ReadLinesReverse(string path)
        {
            var list = new List<string>();
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 0x1000, FileOptions.SequentialScan))
            using (var sr = new StreamReader(fs, Encoding.UTF8))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    list.Add(line);
                }
            }
            list.Reverse();
            return list;
        }
    }
}