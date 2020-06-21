using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Setup.Models;

namespace Setup.Strategy.Folder
{
    class IgnitionAce : IFolderStrategy
    {
        public List<string> Id => new List<string> {"IgnitionACE"};


        public List<IgnitionFolder> GetOpcFolders(OpcUaServer opcServer)
        {
            //List<IgnitionData> opcServerData = new OpcUaClient().Run(opcServer);
            //string sz = JsonConvert.SerializeObject(opcServerData);
            var opcServerData = JsonConvert.DeserializeObject<List<IgnitionData>>(File.ReadAllText(@"C:\Temp\OpcData.json"));
            var ignitionFolders = new List<IgnitionFolder> {new IgnitionFolder {Name = "s1500", Path = ""}};
            var distinctPath = opcServerData.Select(x => x.GetPath()).Distinct().ToList();
            foreach (var opcPath in distinctPath)
            {
                var closure = opcPath;
                do
                {
                    var name = closure.Split('/').Last();
                    var path = closure.Substring(0, closure.IndexOf("/" + name, StringComparison.Ordinal));
                    ignitionFolders.Add(new IgnitionFolder { Name = name, Path = path });
                    closure = path;
                } while (closure.Count(f => f == '/') > 0);
            }
            return ignitionFolders.Distinct().ToList();
        }

        public List<IgnitionFolder> GetIgnFolders(OpcUaServer opcServer)
        {
            var connector = new IgnitionConfigConnector(new EgnServer { Server = "localhost", RigNumber = "000" });
            var ignitionFolders = connector.GetFolders(@"select Name, Path from sqltag where tagtype = 'Folder' and path like 's1500%'");
            ignitionFolders.Add(new IgnitionFolder { Name = "s1500", Path = "" });
            return ignitionFolders;
        }
    }
}