using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Common;
using Common.Database;
using Common.Models.Reporting;
using IgorEnterprise.Misc;
using log4net;
using Timeout = IgorEnterprise.Misc.Timeout;

namespace IgorEnterprise.Process
{
    public class RoboCopyMain
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private static readonly TimeSpan RepeatCheckEvery = TimeSpan.FromMinutes(2);
        public static List<EgnServer> EgnServers;

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
                    EgnServers = ReportingDb.GetAllEgnServers();
                    var files = Directory.GetFiles(@"\\cal0-vp-ace01\e$\share\IgorConfig\ConfigDbSQL");
                    Parallel.ForEach(files, async file =>
                    {
                        var fileToRig = new FileToRigCopy(file);
                        try
                        {
                            var b = await Timeout.ForAsync(() => fileToRig.Copy(), TimeSpan.FromMinutes(10));
                            fileToRig.Copy();
                        }
                        catch (Exception) { /* ignored */ }
                    });
                    Thread.Sleep(RepeatCheckEvery);
                } while (true);
            }
            catch (Exception e)
            {
                Log.Error(e.Message);
                Log.Error(e.ToString());
                Singleton.Instance.SendMessage($"🚨 IgorEnterprise RoboCopyMain failed. {e}");
            }
        }
    }
}