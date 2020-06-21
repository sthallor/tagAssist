using System;
using System.IO;
using System.Text.RegularExpressions;
using Common.Controllers;
using Common.Models.Models;
using Newtonsoft.Json;

namespace Common.Models.Reporting
{
    public class EgnServer
    {
        public string Division { get; set; }
        public string RigNumber { get; set; }
        public string Server { get; set; }
        public IIgnitionController IgnitionController;
        public ServiceController ServiceController;
        public string EgnKitNumber
        {
            get
            {//TODO: Hard coding.
                var egnKitNumber = Regex.Match(Server, @"\d+").Value;
                if (Server.Contains("edge")) return "All";
                if (RigNumber == "T701") return "All";
                return egnKitNumber;
            }
        }

        public void Init()
        {
            ServiceController = new ServiceController(this);
            IgnitionController = new IgnitionController784(this);
            if (IgnitionController.IsLoggedIn())
            {//TODO: Hard coding.
                if(!IsRigSideGateway() && Server != "cal0-vp-ace01")
                    EsiLog.HardError(this, "Running old version of Ignition.", "Internal");
            }
            else
            {
                IgnitionController = new IgnitionController799(this);
            }
        }

        public bool IsRigSideGateway()
        {
            return Server.Contains("-peco-") || Server.Contains("-ig-");
        }

        public HistorianConfig GetHistorianConfig()
        {
            try
            {
                var historianConfig = JsonConvert.DeserializeObject<HistorianConfig>(File.ReadAllText($@"\\cal0-vp-ace01\share\IgorConfig\{RigNumber}\HistorianConfig.json"));
                return historianConfig;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}