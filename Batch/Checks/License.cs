using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Batch.FactoryStuff;
using Common;
using Common.Models.Ignition;
using Common.Models.Reporting;
using Newtonsoft.Json;

namespace Batch.Checks
{
    public class License : IEgnCheck
    {
        public bool Check(EgnServer egnServer)
        {
            try
            {
                if (egnServer.Server == "cal0-vp-ace01") return true;
                if (!egnServer.IgnitionController.IsLoggedIn()) return false;
                var licenseInfo = egnServer.IgnitionController.GetLicenseInfo();
                var config = egnServer.GetHistorianConfig();

                IgnitionLicense ignitionLicense = null;
                try { ignitionLicense = JsonConvert.DeserializeObject<IgnitionLicense>(licenseInfo); }
                catch (Exception) { /* Ignore */ }
                if (egnServer.Server.Contains("-ig-") || egnServer.Server.Contains("-peco-"))
                {
                    var licenseStr = ignitionLicense == null ? licenseInfo.Substring(licenseInfo.IndexOf("<td class=\"c2 detail\">") + 22, 7) : ignitionLicense.primary.key;
                    var opcUaServerConfig = config.OpcUaServers.FirstOrDefault(x => egnServer.Server.StartsWith(x.Host));
                    if (opcUaServerConfig.License != licenseStr)
                    {
                        opcUaServerConfig.License = licenseStr;
                        try
                        {
                            var rs = JsonConvert.SerializeObject(config, Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
                            File.WriteAllText($@"\\cal0-vp-ace01\share\IgorConfig\{egnServer.RigNumber}\HistorianConfig.json", rs);
                        }
                        catch (Exception) { /* ignored */ }
                    }
                    return true;
                }


                if (ignitionLicense == null) return false;
                Singleton.Instance.LicenseKeys.Add(ignitionLicense.primary.key);
                EsiLog.Info(egnServer, $"Ignition gateway with license key {ignitionLicense.primary.key} is correctly licensed.");

                if (config.HistEnvironment.License != ignitionLicense.primary.key)
                {
                    config.HistEnvironment.License = ignitionLicense.primary.key;
                    try
                    {
                        var rs = JsonConvert.SerializeObject(config, Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
                        File.WriteAllText($@"\\cal0-vp-ace01\share\IgorConfig\{egnServer.RigNumber}\HistorianConfig.json", rs);
                    }
                    catch (Exception) { /* ignored */ }
                }

                var trialJson = egnServer.IgnitionController.GetHtmlContent(@"/main/data/status/trial");
                var trial = JsonConvert.DeserializeObject<Trial>(trialJson);
                if (trial.trialState != "NoneInDemo")
                {
                    var modsWeDontCareAbout = new List<string> {"Azure Injector", "MQTT Transmission", "MQTT Engine", "Vision"};
                    var modules = egnServer.IgnitionController.GetModules().Where(x =>
                            x.License == "Trial" && !modsWeDontCareAbout.Contains(x.Name) &&
                            trial.remainingSeconds == 0)
                        .ToList();
                    if (modules.Any(x =>
                        x.License == "Trial" && !modsWeDontCareAbout.Contains(x.Name) && trial.remainingSeconds == 0))
                    {
                        var modulesText = string.Join(",", modules.Select(x => x.Name));
                        EsiLog.HardError(egnServer, $"Ignition gateway is in trial state. Modules: {modulesText}",
                            "Internal");
                    }
                }
            }
            catch (Exception) { /* ignored */ }

            return true;
        }
    }
}