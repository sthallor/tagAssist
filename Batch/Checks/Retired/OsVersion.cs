using System;
using System.IO;
using System.Management;
using Common.Models.Reporting;

namespace Batch.Checks.Retired
{
    public class OsVersion
    {
        public bool Check(EgnServer egnServer)
        {
            try
            {
                var path = new ManagementPath {NamespacePath = @"root\cimv2", Server = egnServer.Server};
                var scope = new ManagementScope(path);
                var query = new ObjectQuery("SELECT * FROM Win32_OperatingSystem");
                string version = null;
                string caption = null;

                var search = new ManagementObjectSearcher(scope, query);

                foreach (ManagementObject obj in search.Get())

                {
                    version = obj["Version"].ToString();
                    caption = obj["Caption"].ToString();
                }
                File.AppendAllText(@"C:\Temp\OsVersion.csv", $"{egnServer.RigNumber},{egnServer.Server},{caption},{version}\n");
                Console.WriteLine($"{egnServer.RigNumber}   {egnServer.Server}  {caption}   {version}");
            }
            catch (Exception)
            {
                File.AppendAllText(@"C:\Temp\OsVersion.csv", $"{egnServer.RigNumber},{egnServer.Server},,\n");
                Console.WriteLine($"{egnServer.RigNumber}   {egnServer.Server}");
            }
            return true;
        }
    }
}