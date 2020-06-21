using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Management;
using Batch.FactoryStuff;
using Common;
using Common.Extensions;
using Common.Models.Reporting;

namespace Batch.Checks.Pre
{
    public class ServerDiskSpace : IPreCheck
    {
        private static readonly List<string> DrivesToCheck = new List<string> { "DriveLetter = 'C:'", "DriveLetter = 'E:'" };
        private static readonly string[] WarnErrorThreshold = "1800,1200".Split(',');

        public bool Check()
        {
            SpaceCheck(new EgnServer { Server = "CAL0-VP-ACE01" });
            SpaceCheck(new EgnServer { Server = "CAL0-VP-ACE02" });
            SpaceCheck(new EgnServer { Server = "CAL0-VU-ACE01" });
            return true;
        }

        private static void SpaceCheck(EgnServer egnServer)
        {
            var path = new ManagementPath {NamespacePath = @"root\cimv2", Server = egnServer.Server};
            var scope = new ManagementScope(path);
            foreach (var condition in DrivesToCheck)
            {
                string[] selectedProperties = {"FreeSpace", "Capacity"};
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
                            var message = $"{condition} {freeSpace.BytesToString()} free of {capacity.BytesToString()} ({egnServer.Server})";
                            var mbFree = freeSpace / 1024 / 1024;

                            if (mbFree < ulong.Parse(WarnErrorThreshold[1]))
                                EsiLog.HardError(egnServer, message, "Internal");
                            else if (mbFree < ulong.Parse(WarnErrorThreshold[0]))
                                EsiLog.Warn(egnServer, message, "Internal");
                            else
                                EsiLog.Info(egnServer, message);
                        }
                    }
                }
                catch (Exception) { /* ignored */ }
            }
        }
    }
}