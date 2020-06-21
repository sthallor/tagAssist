using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Common.Controllers;
using Common.Models.Models;
using IgorRig.Misc;
using log4net;

namespace IgorRig.Processes
{
    public class IgnitionConfigSetter
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private const string ConfigFile = @"IgnitionConfig.json";
        private const string ConfigPath = @"C:\Installs\IgorConfig\Common\";
        public static readonly string VersionFile = @"C:\Program Files\Igor\IgnitionConfigVersion.txt";
        private static readonly TimeSpan RepeatCheckEvery = TimeSpan.FromMinutes(15);
        private static bool _restartRequired;
        private static IIgnitionController _controller;
        private static readonly IgnitionConfig IgnitionConfig = IgnitionConfig.GetConfig();

        public static void Run()
        {
            var thread = new Thread(Check);
            thread.Start();
        }

        public static void Check()
        {
            Thread.Sleep(TimeSpan.FromMinutes(2));
            do
            {
                try
                {
                    if (GetAvailableVersion() != null && GetAppliedVersion() != GetAvailableVersion())
                    {
                        Log.Info($"GetAvailableVersion() {GetAvailableVersion()} GetAppliedVersion() {GetAppliedVersion()}");
                        RigSingleton.Instance.EgnServer.Init();
                        _controller = RigSingleton.Instance.EgnServer.IgnitionController;
                        DoTheThing(_controller.SetGatewayName());
                        DoTheThing(_controller.SetDbTranslation());
                        DoTheThing(_controller.WebSandfEndpoint());
                        DoTheThing(_controller.CreateDbConnection());
                        DoTheThing(_controller.CreateSplitter());
                        DoTheThing(_controller.SetDataPruning());
                        DoTheThing(DoModules());
                        UpdateConfigVersion();
                        //TODO: Apply license?
                        //TODO: Upload .proj file and edit description to include rig
                    }
                }
                catch (Exception e)
                {
                    Log.Error(e);
                    RigSingleton.Instance.SendMessage($"Failed IgnitionConfigSetter {e}");
                }

                if (_restartRequired)
                {
                    Log.Info("Stopping Ignition service.");
                    RigSingleton.Instance.EgnServer.ServiceController.Stop("Ignition");
                    Thread.Sleep(TimeSpan.FromMinutes(2));

                    Log.Info("Starting Ignition service.");
                    RigSingleton.Instance.EgnServer.ServiceController.Start("Ignition");
                    Thread.Sleep(TimeSpan.FromMinutes(2));
                    _restartRequired = false;
                }
                Thread.Sleep(RepeatCheckEvery);
            } while (true);
        }

        private static void DoTheThing(string message)
        {
            if (!string.IsNullOrEmpty(message))
            {
                Log.Info(message);
                RigSingleton.Instance.SendMessage(message);
            }
        }

        private static string DoModules()
        {
            var message = "";
            const string installedModules = @"C:\Program Files\Inductive Automation\Ignition\user-lib\modules";
            var allowedModules = new List<string>
            {
                "Azure-Injector-signed.modl",
                "ensign-websf-7.9.modl",
                "Modbus Driver v2-module.modl",
                "OPC-UA-module.modl",
                "Tag Historian-module.modl",
                "UDP and TCP Drivers-module.modl",
                "Vision-module.modl"
            };

            if (!File.Exists(@"C:\Program Files\Inductive Automation\Ignition\user-lib\modules\ensign-websf-7.9.modl"))
            {
                if (message == "") message = "Config Modules";
                message += "\nInstalling module: ensign-websf-7.9.modl";
                File.Copy(@"C:\Installs\modules\ensign-websf-7.9.modl",
                    @"C:\Program Files\Inductive Automation\Ignition\user-lib\modules\ensign-websf-7.9.modl");
                _restartRequired = true;
            }

            foreach (var path in Directory.EnumerateFiles(installedModules, "*", SearchOption.AllDirectories))
            {
                if (!IgnitionConfig.Modules.RemoveModules.Contains(Path.GetFileName(path))) continue;
                _restartRequired = true;
                RigSingleton.Instance.EgnServer.ServiceController.Stop("Ignition");
                if (message == "") message = "Config Modules";
                message += $"\nRemoving module: {Path.GetFileName(path)}";
                Log.Info($"Removing module {Path.GetFileName(path)}");
                File.Delete(path);
            }

            var modules = _controller.GetModules();
            foreach (var module in modules)
            foreach (var action in module.Actions)
            {
                if (action.Split(';')[0] == "install")
                {
                    if (message == "") message = "Config Modules";
                    message += $"\nMust manually accept license for module {module.Name}";
                }
            }
            return message;
        }

        private static string GetAvailableVersion()
        {
            try
            {
                return Md5Folder(ConfigPath);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static string GetAppliedVersion()
        {
            try
            {
                var readAllText = File.ReadAllText(VersionFile);
                return readAllText;
            }
            catch (Exception)
            {
                return "";
            }
        }

        public static string Md5Folder(string path)
        {
            // Hard coding just the one file...
            var files = Directory.GetFiles(path, ConfigFile, SearchOption.AllDirectories).OrderBy(p => p)
                .ToList(); // "*.*"
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

        private static void UpdateConfigVersion()
        {
            var version = GetAvailableVersion();
            File.WriteAllText(VersionFile, version);
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
    }
}