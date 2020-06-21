using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Common.Models.Reporting;
using IgorEnterprise.Process;
using log4net;

namespace IgorEnterprise.Misc
{
    public class FileToRigCopy
    {
        private readonly FileInfo _fileInfo;
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        public EgnServer EgnServer;
        public FileToRigCopy(string file)
        {
            _fileInfo = new FileInfo(file);
            try
            {
                var index = file.IndexOf("rig", StringComparison.Ordinal);
                var rigNumber = file.Substring(index + 3, file.IndexOf(".", index, StringComparison.Ordinal) - index - 3);
                EgnServer = RoboCopyMain.EgnServers.FirstOrDefault(x => x.RigNumber == rigNumber);
            }
            catch (Exception)
            {
                Log.Error($"Failed to initialize FileToRigCopy for file {file}");
            }
        }

        public void Copy()
        {
            ExecuteCommand($@"robocopy {_fileInfo.Directory} \\{EgnServer.Server}\c$\Installs\IgorConfig\{_fileInfo.Directory?.Name} *rig{EgnServer.RigNumber}* /mov /w:5 /r:2");
        }

        public static void ExecuteCommand(string command)
        {
            var process = new System.Diagnostics.Process
            {
                StartInfo =
                {
                    UseShellExecute = false,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    FileName = "cmd.exe",
                    Arguments = $"/C {command}"
                }
            };
            process.Start();
            process.WaitForExit();
        }
    }
}