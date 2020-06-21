using System;
using System.IO;
using Batch.FactoryStuff;
using Common;
using Common.Extensions;
using Common.Models.Reporting;

namespace Batch.Checks.Pre
{
    public class CleanTempFiles : IPreCheck
    {

        public bool Check()
        {
            var egnServer = new EgnServer {Server = "cal0-vp-ace02"};
            var files = Directory.GetFiles($@"\\{egnServer.Server}\c$\Windows\Temp");
            ulong bytesDeleted = 0;
            foreach (var file in files)
            {
                var fi = new FileInfo(file);
                if (fi.LastAccessTime >= DateTime.Now.AddDays(-2)) continue;
                try
                {
                    fi.Delete();
                    bytesDeleted += (ulong)fi.Length;
                }
                catch (Exception) { /* ignored */ }
            }
            EsiLog.Info(egnServer, $"Removed {bytesDeleted.BytesToString()} from temp directory.");
            return true;
        }
    }
}