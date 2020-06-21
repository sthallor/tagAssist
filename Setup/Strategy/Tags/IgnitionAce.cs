using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Common.Database;
using Common.Models.Reporting;
using Common.Models.TagCompare;
using Newtonsoft.Json;

namespace Setup.Strategy.Tags
{
    class IgnitionAce : ITagStrategy
    {
        public List<string> Id => new List<string> {"IgnitionACE"};
        public List<IgnitionData> GetOpcTags(OpcUaServer opcServer)
        {
            //List<IgnitionData> opcServerData = new OpcUaClient().Run(opcServer);
            //string sz = JsonConvert.SerializeObject(opcUaClient);
            var opcServerData = JsonConvert.DeserializeObject<List<IgnitionData>>(File.ReadAllText(@"C:\Temp\OpcData.json"));
            return opcServerData;
        }

        public List<IgnitionData> GetIgnTags(OpcUaServer opcServer)
        {
            var connector = new IgnitionConfigConnector(new EgnServer { Server = "localhost", RigNumber = "000" });
            var tagData = connector.GetTagData(opcServer);
            return tagData.ToList();
        }
    }
}