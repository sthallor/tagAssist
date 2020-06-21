using System;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Reflection;
using System.Threading;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using log4net;

namespace DeployRealTimeRigState.Misc
{
    public static class Utility
    {
        private static string WmiLog = "wmi.log";
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);


        public static void ZipFilesMatchingPattern(string outPathname, string fileSourcePath, string searchPattern)
        {
            var fsOut = File.Create(outPathname);
            var zipStream = new ZipOutputStream(fsOut);
            zipStream.SetLevel(9);
            var folderOffset = fileSourcePath.Length + (fileSourcePath.EndsWith("\\") ? 0 : 1);
            var files = Directory.GetFiles(fileSourcePath, searchPattern);

            foreach (var filename in files)
            {
                var fi = new FileInfo(filename);
                var entryName = filename.Substring(folderOffset);
                entryName = ZipEntry.CleanName(entryName);
                var newEntry = new ZipEntry(entryName)
                {
                    DateTime = fi.LastWriteTime,
                    Size = fi.Length
                };
                zipStream.PutNextEntry(newEntry);
                var buffer = new byte[4096];
                using (var streamReader = File.OpenRead(filename))
                {
                    StreamUtils.Copy(streamReader, zipStream, buffer);
                }
                zipStream.CloseEntry();
            }

            zipStream.IsStreamOwner = true; // Makes the Close also Close the underlying stream
            zipStream.Close();
        }

        public static void CreateZip(string outPathname, string folderName)
        {
            var fsOut = File.Create(outPathname);
            var zipStream = new ZipOutputStream(fsOut);
            zipStream.SetLevel(9);
            var folderOffset = folderName.Length + (folderName.EndsWith("\\") ? 0 : 1);
            CompressFolder(folderName, zipStream, folderOffset);
            zipStream.IsStreamOwner = true;
            zipStream.Close();
        }

        private static void CompressFolder(string path, ZipOutputStream zipStream, int folderOffset)
        {
            var files = Directory.GetFiles(path);
            foreach (var filename in files)
            {
                var fi = new FileInfo(filename);
                var entryName = filename.Substring(folderOffset);
                entryName = ZipEntry.CleanName(entryName);
                var newEntry = new ZipEntry(entryName)
                {
                    DateTime = fi.LastWriteTime,
                    Size = fi.Length
                };
                zipStream.PutNextEntry(newEntry);
                var buffer = new byte[4096];
                using (var streamReader = File.OpenRead(filename))
                {
                    StreamUtils.Copy(streamReader, zipStream, buffer);
                }
                zipStream.CloseEntry();
            }
            var folders = Directory.GetDirectories(path);
            foreach (var folder in folders)
            {
                CompressFolder(folder, zipStream, folderOffset);
            }
        }
        public static void CloneDirectory(string root, string dest)
        {
            foreach (var directory in Directory.GetDirectories(root))
            {
                string dirName = Path.GetFileName(directory);
                if (!Directory.Exists(Path.Combine(dest, dirName)))
                {
                    Directory.CreateDirectory(Path.Combine(dest, dirName));
                }
                CloneDirectory(directory, Path.Combine(dest, dirName));
            }

            foreach (var file in Directory.GetFiles(root))
            {
                if(!file.Contains("Thumbs.db"))
                    File.Copy(file, Path.Combine(dest, Path.GetFileName(file)), true);
            }
        }

        public static void Execute(string command, string server)
        {
            try
            {
                var connectionOptions = new ConnectionOptions();
                connectionOptions.Impersonation = ImpersonationLevel.Impersonate;
                connectionOptions.EnablePrivileges = true;

                var objectGetOptions = new ObjectGetOptions();
                var managementPath = new ManagementPath("Win32_Process");
                var managementScope = new ManagementScope($@"\\{server}\ROOT\CIMV2", connectionOptions);
                
                managementScope.Connect();
                var methodOptions = new InvokeMethodOptions(null, TimeSpan.MaxValue);
                var processClass = new ManagementClass(managementScope, managementPath, objectGetOptions);
                ManagementBaseObject inParams = processClass.GetMethodParameters("Create");
                inParams["CommandLine"] = "cmd /c (cd " + GetHomeDirectory() + ") & " + command + " > c:\\" + WmiLog;
                ManagementBaseObject outParams = processClass.InvokeMethod("Create", inParams, methodOptions);
                Debug.Assert(outParams != null, "outParams != null");
                var processId = (uint)outParams["processId"];
                if (processId != 0)
                {
                    while (true) { try { Process.GetProcessById((int)processId, server); Thread.Sleep(1000); } catch (Exception) { break; } }
                    Thread.Sleep(1000);
                }
            }
            catch (Exception e)
            {
                Log.Error(e.Message);
            }
        }
        private static string GetHomeDirectory()
        {
            return "C:\\";
        }
    }
}