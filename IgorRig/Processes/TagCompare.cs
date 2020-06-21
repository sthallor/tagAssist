using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Common;
using Common.Controllers;
using Common.Database;
using Common.Models.Ignition;
using Common.Models.Models;
using FileHelpers;
using IgorRig.Misc;
using log4net;
using Newtonsoft.Json;

namespace IgorRig.Processes
{
    public class TagCompare
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private static readonly TimeSpan RepeatCheckEvery = TimeSpan.FromDays(1);
        private static readonly TimeSpan SkipCheckTime = TimeSpan.FromHours(1);
        private static string message = "TagCompare Complete 🏁\n";
        private static bool sendmesg;
        private static readonly List<string> SourcesThatWork = new List<string> 
            { "ADR Pilot", "IgnitionACE", "RigHistorian", "T-RigHistorian",
                "Cameron Kepware", "IEC Kepware", "IEC769", "IEC774", "IEC776" };
        private static readonly List<string> Disconnected = new List<string> 
            { "Cameron Kepware", "IEC Kepware", "IEC769", "IEC774", "IEC776" }; // Not getting live OpcUaTags from these. Just hard coded csv tag exports...

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
                    // Checks
                    Directory.CreateDirectory(@"C:\Installs\IgorConfig\Output");
                    Directory.CreateDirectory(@"C:\Installs\IgorConfig\Common");
                    if (RigSingleton.Instance.EgnServer.IgnitionController.GetOpcServers()
                        .Where(x => x.Status != "Connected" && !Disconnected.Contains(x.Name)).ToList().Any())
                    {
                        Log.Warn("Not all OpcServers are in connected state to begin comparison.");
                        Thread.Sleep(SkipCheckTime);
                        continue;
                    }
                    var configFile = Singleton.Instance.DebugMode ?
                        $@"\\cal0-vp-ace01\share\IgorConfig\Output\HistorianConfig{RigSingleton.Instance.EgnServer.RigNumber}.json" :
                        $@"C:\Installs\IgorConfig\{RigSingleton.Instance.EgnServer.RigNumber}\HistorianConfig.json";
                    if (!File.Exists(configFile))
                    {
                        var msg = "Historian configuration does not exist for TagCompare";
                        Log.Warn(msg);
                        RigSingleton.Instance.SendMessage(msg);
                        Thread.Sleep(SkipCheckTime);
                        continue;
                    }
                    if (ConfigDb.GetSplitterTags().Any())
                    {
                        const string msg = "Some tags are still set to splitter history provider. Manually update/remove.";
                        Log.Warn(msg);
                        RigSingleton.Instance.SendMessage(msg);
                        Thread.Sleep(SkipCheckTime);
                        continue;
                    }
                    // Main process
                    RigSingleton.Instance.ReGetHistorianConfig();
                    UpdateIgnitionTags();
                    UpdateOpcUa();
                    WriteOutputFile();
                    foreach (var opcUaServer in RigSingleton.Instance.GetHistorianConfig().OpcUaServers.Where(x=> SourcesThatWork.Contains(x.Name)))
                    {
                        ProcessFolders(opcUaServer);
                        ProcessTags(opcUaServer);
                    }

                    if (sendmesg)
                    {
                        RigSingleton.Instance.SendMessage(message);
                        RigSingleton.Instance.EgnServer.ServiceController.Stop("Ignition");
                        Thread.Sleep(TimeSpan.FromMinutes(2));
                        RigSingleton.Instance.EgnServer.ServiceController.Start("Ignition");
                        TagThrottle.ForceThrottleCheck();
                        sendmesg = false;
                    }
                    Log.Info("Finished TagCompare.");
                    Thread.Sleep(RepeatCheckEvery);
                } while (true);
            }
            catch (Exception e)
            {
                Log.Error(e.Message);
                Log.Error(e);
                RigSingleton.Instance.SendMessage($"Failed TagCompare {RigSingleton.Instance.EgnServer.RigNumber} {e.Message}");
            }
        }

        private static void ProcessTags(OpcUaServerConfig opcUaServer)
        {
            if (opcUaServer.OpcTags == null || opcUaServer.OpcTags.Count == 0)
            {
                Log.Warn($"Did not find any OpcTags for {opcUaServer.Name}");
                return;
            }
            foreach (var historyProvider in opcUaServer.GetHistoryProviders())
            {
                var insertTags = new List<OpcTagInfo>();
                var deleteTags = new List<IgnitionData>();
                foreach (var opcTagInfo in opcUaServer.OpcTags.ToList())
                {
                    var ignitionTag = opcUaServer.Name == "RigHistorian" ?
                        historyProvider.IgnitionTags.FirstOrDefault(x => x.OpcItemPath.EndsWith(opcTagInfo.NodeId)) :
                        historyProvider.IgnitionTags.FirstOrDefault(x => x.OpcItemPath == opcTagInfo.NodeId);

                    if (ignitionTag == null)
                    {
                        var tagList = GetTagList(opcUaServer, historyProvider);
                        var folderList = GetMonitoredFolderList(opcUaServer, historyProvider);
                        if (tagList == null && folderList == null)
                        {
                            Log.Info($"AllRule: Insert into Ignition {opcTagInfo.NodeId}");
                            insertTags.Add(opcTagInfo);
                        }

                        if (tagList?.FirstOrDefault(x => x == opcTagInfo.NodeId) != null)
                        {
                            Log.Info($"TagList: Insert into Ignition {opcTagInfo.NodeId}");
                            insertTags.Add(opcTagInfo);
                        }

                        var ignitionFolder = OpcTagInfo.GetPath(opcTagInfo, opcUaServer, historyProvider);
                        if (folderList == null) continue;
                        foreach (var folder in folderList)
                        {
                            if (ignitionFolder.ToLower() == folder.ToLower().TrimEnd('/'))
                            {
                                Log.Info($"FolderList: Insert into Ignition {opcTagInfo.NodeId}");
                                insertTags.Add(opcTagInfo);
                                break;
                            }
                            if (!ignitionFolder.ToLower().StartsWith(folder.ToLower()) || ignitionFolder.ToLower() == folder.ToLower().Substring(0, folder.Length-1)) continue;
                            Log.Info($"FolderList: Insert into Ignition {opcTagInfo.NodeId}");
                            insertTags.Add(opcTagInfo);
                            break;
                        }
                    }
                }

                foreach (var ignitionTag in historyProvider.IgnitionTags.ToList())
                {
                    var opcTag = opcUaServer.OpcTags.FirstOrDefault(x => x.NodeId == ignitionTag.OpcItemPath);
                    if (opcTag == null) { opcTag = opcUaServer.OpcTags.FirstOrDefault(x => x.NodeId == ignitionTag.OpcItemPath.Replace("ns=1;s=", "")); }

                    if (opcTag == null)
                    {
                        deleteTags.Add(ignitionTag);
                        continue;
                    }

                    var tagList = GetTagList(opcUaServer, historyProvider);
                    var folderList = GetMonitoredFolderList(opcUaServer, historyProvider);

                    if (tagList != null && tagList.FirstOrDefault(x => x == ignitionTag.OpcItemPath) == null)
                    {
                        Log.Info($"Not found in Taglist: Delete from Ignition {ignitionTag.OpcItemPath}");
                        deleteTags.Add(ignitionTag);
                    }
                    if (folderList == null) continue;
                    var foundMatch = false;
                    foreach (var folder in folderList)
                    {
                        // If it is beneath this directory or if it is IN this directory.
                        if (ignitionTag.Path.ToLower().StartsWith(folder.ToLower()) || ignitionTag.Path.ToLower() == folder.ToLower().Substring(0, folder.Length - 1))
                        {
                            foundMatch = true;
                            break;
                        }
                    }
                    if (!foundMatch)
                    {
                        Log.Info($"Not found in FolderList: Delete from Ignition {ignitionTag.OpcItemPath}");
                        deleteTags.Add(ignitionTag);
                    }

                }
                DoTagsNow(insertTags, deleteTags, historyProvider, opcUaServer);
            }
        }

        private static void ProcessFolders(OpcUaServerConfig opcUaServer)
        {
            if (opcUaServer.OpcTags == null || opcUaServer.OpcTags.Count == 0) return;
            var ignFolders = GetIgnFolders(opcUaServer);
            var opcFolders = GetOpcFolders(opcUaServer);
            var delete = GetExcept(ignFolders, opcFolders);
            var insert = GetExcept(opcFolders, ignFolders);

            var insertz = new List<IgnitionFolder>();
            var subsetFolderList = false;
            foreach (var historyProvider in opcUaServer.GetHistoryProviders())
            {
                var monitoredFolderList = GetMonitoredFolderList(opcUaServer, historyProvider);
                if (monitoredFolderList != null && monitoredFolderList.Count > 0)
                {
                    subsetFolderList = true;
                }

                foreach (var ignitionFolder in insert)
                {
                    try
                    {
                        foreach (var monitoredFolder in monitoredFolderList)
                        {
                            if ((ignitionFolder.Path + "/" + ignitionFolder.Name).StartsWith(monitoredFolder))
                            {
                                insertz.Add(ignitionFolder);
                            }
                        }
                    }
                    catch (Exception) { /* Ignored */ }
                }
            } // replace insert with insertz if there was ever anything in monitored folder list?

            if (subsetFolderList)
            {
                insert = insertz;
            }

            if (insert.Count > 0)
            {
                sendmesg = true;
                message += $"✅ 📁 {opcUaServer.Name}\n";
                foreach (var folder in insert.Take(100))
                {
                    message += $"▫️ {folder.Path}/{folder.Name}\n";
                    Log.Info($"Insert folder {folder.Path} {folder.Name}");
                }

                if (insert.Count > 100)
                {
                    message += "▫️ More than 100 folders added. Review log for details.\n";
                }
            }

            if (delete.Count > 0)
            {
                sendmesg = true;
                message += $"❎ 📁 {opcUaServer.Name}\n";
                foreach (var folder in delete.Take(100))
                {
                    message += $"▫️ {folder.Path}/{folder.Name}\n";
                    Log.Info($"Delete folder {folder.Path} {folder.Name}");
                }

                if (delete.Count > 100)
                {
                    message += "▫️ More than 100 folders deleted. Review log for details.\n";
                }
            }

            DoFoldersNow(delete, insert);
        }

        private static void WriteOutputFile()
        {
            try
            {
                var rs = JsonConvert.SerializeObject(RigSingleton.Instance.GetHistorianConfig(), Formatting.Indented,
                    new JsonSerializerSettings {NullValueHandling = NullValueHandling.Ignore});
                Directory.CreateDirectory(@"C:\Installs\IgorConfig\Output\");
                File.WriteAllText($@"C:\Installs\IgorConfig\Output\HistorianConfig{RigSingleton.Instance.EgnServer.RigNumber}.json", rs);
            }
            catch (Exception) { /* ignored */ }
        }

        private static List<IgnitionFolder> GetExcept(List<IgnitionFolder> ignFolders, List<IgnitionFolder> opcFolders)
        {
            var ignitionFolders = new List<IgnitionFolder>();
            foreach (var ignitionFolder in ignFolders)
            {
                var firstOrDefault = opcFolders.FirstOrDefault(x => x.Name.ToLower() == ignitionFolder.Name.ToLower() && x.Path.ToLower() == ignitionFolder.Path.ToLower());
                if(firstOrDefault == null)
                    ignitionFolders.Add(ignitionFolder);
            }
            return ignitionFolders;
        }

        private static void DoFoldersNow(List<IgnitionFolder> delete, List<IgnitionFolder> insert)
        {
            foreach (var folder in delete)
            {
                ConfigDb.DeleteFolder(folder);
            }

            foreach (var folder in insert)
            {
                ConfigDb.CreateFolder(folder);
            }
        }

        private static void DoTagsNow(List<OpcTagInfo> insertTags, List<IgnitionData> deleteTags, HistoryProvider historyProvider, OpcUaServerConfig opcUaServer)
        {
            if (insertTags.Count > 0)
            {
                sendmesg = true;
                message += $"✅ 🏷️ {opcUaServer.Name} {historyProvider.Name}\n";
                foreach (var opcTagInfo in insertTags)
                {
                    ConfigDb.CreateTag(opcTagInfo, historyProvider, opcUaServer);
                }
                foreach (var opcTagInfo in insertTags.Take(100))
                {
                    message += $"▫️ {OpcTagInfo.GetPath(opcTagInfo, opcUaServer, historyProvider)}/{opcTagInfo.DisplayName}\n";
                }

                if (insertTags.Count > 100)
                {
                    message += "▫️ More than 100 tags added. Review log for details.\n";
                }
            }

            if (deleteTags.Count > 0)
            {
                var deleteCount = 0;
                message += $"❎ 🏷️ {opcUaServer.Name} {historyProvider.Name}\n";
                foreach (var tag in deleteTags)
                {
                    if (ConfigDb.DeleteTag(tag))
                    {
                        sendmesg = true;
                        deleteCount += 1;
                        if(deleteCount <= 100)
                            message += $"▫️ {tag.Path}/{tag.Name}\n";
                    }
                }
                if (deleteCount > 100)
                {
                    message += "▫️ More than 100 tags deleted. Review log for details.\n";
                }
            }
        }

        private static List<IgnitionFolder> GetOpcFolders(OpcUaServerConfig opcUaServer)
        {
            var ignitionFolders = new List<IgnitionFolder>();

            foreach (var historyProvider in opcUaServer.GetHistoryProviders())
            {
                var distinctPath = opcUaServer.OpcTags.Select(x => OpcTagInfo.GetPath(x, opcUaServer, historyProvider)).Distinct().ToList();
                foreach (var opcPath in distinctPath)
                {
                    var closure = opcPath;
                    do
                    {
                        if (opcPath == historyProvider.RootFolder && !opcPath.Contains("/"))
                            continue;
                        var name = closure.Split('/').Last();
                        var indexOf = closure.IndexOf("/" + name, StringComparison.Ordinal);
                        var path = closure.Substring(0, indexOf);
                        closure = path;
                        if (path == historyProvider.RootFolder && name == "")
                            continue;
                        ignitionFolders.Add(new IgnitionFolder {Name = name, Path = path});
                    } while (closure.Count(x => x == '/') > 0);
                }
            }

            var folders = new List<IgnitionFolder>();
            foreach (var ignitionFolder in ignitionFolders)
            {
                var folder = folders.FirstOrDefault(x =>
                    x.Name.ToLower() == ignitionFolder.Name.ToLower() &&
                    x.Path.ToLower() == ignitionFolder.Path.ToLower());
                if (folder == null)
                {
                    folders.Add(ignitionFolder);
                }
            }
            return folders;
        }

        private static List<IgnitionFolder> GetIgnFolders(OpcUaServerConfig opcUaServer)
        {
            var ignitionFolders = new List<IgnitionFolder>();
            foreach (var historyProvider in opcUaServer.GetHistoryProviders())
            {
                string query;
                if (historyProvider.RootFolder.Contains("/"))
                {
                    query = $@"select Name, Path from sqltag where tagtype = 'Folder' and path like '{historyProvider.RootFolder}%' or
(path = '{historyProvider.RootFolder.Split('/')[0]}' and name = '') or (path = '{historyProvider.RootFolder.Split('/')[0]}' and name = '{historyProvider.RootFolder.Split('/')[1]}')";
                }
                else
                {
                    query = $@"select Name, Path from sqltag where tagtype = 'Folder' and path like '{historyProvider.RootFolder}%'";
                }
                ignitionFolders.AddRange(ConfigDb.GetFolders(query));
            }
            return ignitionFolders;
        }

        private static List<string> GetTagList(OpcUaServerConfig opcUaServer, HistoryProvider historyProvider)
        {
            if (historyProvider.TagList != null)
                return historyProvider.TagList;
            var defaults = Singleton.Instance.GetOpcUaDefaults().FirstOrDefault(x => x.Name == opcUaServer.Name);
            var provider = defaults?.HistoryProviders.FirstOrDefault(x => x.Name == historyProvider.Name); // && x.ScanClass == historyProvider.ScanClass
            return provider?.TagList;
        }

        private static List<string> GetMonitoredFolderList(OpcUaServerConfig opcUaServer,
            HistoryProvider historyProvider)
        {
            if (historyProvider.FolderList != null)
                return historyProvider.FolderList;
            var defaults = Singleton.Instance.GetOpcUaDefaults().FirstOrDefault(x => x.Name == opcUaServer.Name);
            var provider = defaults?.HistoryProviders.FirstOrDefault(x => x.Name == historyProvider.Name); // && x.ScanClass == historyProvider.ScanClass
            return provider?.FolderList;
        }

        private static void UpdateIgnitionTags()
        {
            foreach (var opcUaServer in RigSingleton.Instance.GetHistorianConfig().OpcUaServers.Where(x => SourcesThatWork.Contains(x.Name)))
            {
                foreach (var historyProvider in opcUaServer.GetHistoryProviders())
                {
                    historyProvider.IgnitionTags = ConfigDb.GetTagData(opcUaServer, historyProvider);
                }
            }
        }

        private static void UpdateOpcUa()
        {
            if (RigSingleton.Instance.GetHistorianConfig().OpcUaServers.Any(x => x.OpcTags != null)) return;
            var historianConfig = RigSingleton.Instance.GetHistorianConfig();
            var opcUaClientNew = new OpcUaClientNew(historianConfig);
            try
            {
                opcUaClientNew.Run();
            }
            catch (Exception e)
            {
                Log.Error(e.Message);
                Log.Error(e);
                if (!e.Message.Contains("BadRequestTimeout") && !e.Message.Contains("Error establishing a connection"))
                    RigSingleton.Instance.SendMessage($"Failed TagCompare {RigSingleton.Instance.EgnServer.RigNumber} {e.Message}");
            }

            // This is a weird hack until I figure out how to connect to Cameron Kepware
            DoCameronKepware();
            DoIECKepware();
            DoIEC769();
            DoIEC774();
            DoIEC776();
        }

        private static void DoCameronKepware()
        {
            var opcUaServerConfig = RigSingleton.Instance.GetHistorianConfig().OpcUaServers
                .FirstOrDefault(x => x.Name == "Cameron Kepware");
            if (opcUaServerConfig != null)
            {
                opcUaServerConfig.OpcTags = new List<OpcTagInfo>();
                var engine = new FileHelperEngine<TagsExport>();
                var result = engine.ReadFile(@"C:\Installs\IgorConfig\Common\CameronKepwareTagExport.csv");
                foreach (var tagsExport in result.Where(x => x.OPCServer == "Cameron Kepware"))
                {
                    var opcTagInfo = new OpcTagInfo
                    {
                        NodeId = tagsExport.OPCItemPath,
                        DisplayName = tagsExport.Name,
                        BrowseName = tagsExport.Name,
                        DataType = OpcTagInfo.GetDataType(tagsExport.DataType)
                    };
                    opcUaServerConfig.OpcTags.Add(opcTagInfo);
                }
                Log.Info($"Loaded {result.Length} Cameron Kepware tags from csv tag export file.");
            }
        }
        private static void DoIECKepware()
        {
            var opcUaServerConfig = RigSingleton.Instance.GetHistorianConfig().OpcUaServers
                .FirstOrDefault(x => x.Name == "IEC Kepware");
            if (opcUaServerConfig != null)
            {
                opcUaServerConfig.OpcTags = new List<OpcTagInfo>();
                var engine = new FileHelperEngine<TagsExport>();
                var result = engine.ReadFile(@"C:\Installs\IgorConfig\Common\IECKepwareTagExport.csv");
                foreach (var tagsExport in result.Where(x => x.OPCServer == "IEC Kepware"))
                {
                    var opcTagInfo = new OpcTagInfo
                    {
                        NodeId = tagsExport.OPCItemPath,
                        DisplayName = tagsExport.Name,
                        BrowseName = tagsExport.Name,
                        DataType = OpcTagInfo.GetDataType(tagsExport.DataType)
                    };
                    opcUaServerConfig.OpcTags.Add(opcTagInfo);
                }
                Log.Info($"Loaded {result.Length} IEC Kepware tags from csv tag export file.");
            }
        }
        private static void DoIEC769()
        {
            var opcUaServerConfig = RigSingleton.Instance.GetHistorianConfig().OpcUaServers
                .FirstOrDefault(x => x.Name == "IEC769");
            if (opcUaServerConfig != null)
            {
                opcUaServerConfig.OpcTags = new List<OpcTagInfo>();
                var engine = new FileHelperEngine<TagsExport>();
                var result = engine.ReadFile(@"C:\Installs\IgorConfig\Common\IEC769TagExport.csv");
                foreach (var tagsExport in result.Where(x => x.OPCServer == "IEC769"))
                {
                    var opcTagInfo = new OpcTagInfo
                    {
                        NodeId = tagsExport.OPCItemPath,
                        DisplayName = tagsExport.Name,
                        BrowseName = tagsExport.Name,
                        DataType = OpcTagInfo.GetDataType(tagsExport.DataType)
                    };
                    opcUaServerConfig.OpcTags.Add(opcTagInfo);
                }
                Log.Info($"Loaded {result.Length} IEC769 tags from csv tag export file.");
            }
        }
        private static void DoIEC774()
        {
            var opcUaServerConfig = RigSingleton.Instance.GetHistorianConfig().OpcUaServers
                .FirstOrDefault(x => x.Name == "IEC774");
            if (opcUaServerConfig != null)
            {
                opcUaServerConfig.OpcTags = new List<OpcTagInfo>();
                var engine = new FileHelperEngine<TagsExport>();
                var result = engine.ReadFile(@"C:\Installs\IgorConfig\Common\IEC774TagExport.csv");
                foreach (var tagsExport in result.Where(x => x.OPCServer == "IEC774"))
                {
                    var opcTagInfo = new OpcTagInfo
                    {
                        NodeId = tagsExport.OPCItemPath,
                        DisplayName = tagsExport.Name,
                        BrowseName = tagsExport.Name,
                        DataType = OpcTagInfo.GetDataType(tagsExport.DataType)
                    };
                    opcUaServerConfig.OpcTags.Add(opcTagInfo);
                }
                Log.Info($"Loaded {result.Length} IEC774 tags from csv tag export file.");
            }
        }
        private static void DoIEC776()
        {
            var opcUaServerConfig = RigSingleton.Instance.GetHistorianConfig().OpcUaServers
                .FirstOrDefault(x => x.Name == "IEC776");
            if (opcUaServerConfig != null)
            {
                opcUaServerConfig.OpcTags = new List<OpcTagInfo>();
                var engine = new FileHelperEngine<TagsExport>();
                var result = engine.ReadFile(@"C:\Installs\IgorConfig\Common\IEC776TagExport.csv");
                foreach (var tagsExport in result.Where(x => x.OPCServer == "IEC776"))
                {
                    var opcTagInfo = new OpcTagInfo
                    {
                        NodeId = tagsExport.OPCItemPath,
                        DisplayName = tagsExport.Name,
                        BrowseName = tagsExport.Name,
                        DataType = OpcTagInfo.GetDataType(tagsExport.DataType)
                    };
                    opcUaServerConfig.OpcTags.Add(opcTagInfo);
                }
                Log.Info($"Loaded {result.Length} IEC776 tags from csv tag export file.");
            }
        }
    }
}