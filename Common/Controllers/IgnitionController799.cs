using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Web;
using Common.Models.Ignition;
using Common.Models.Models;
using Common.Models.Reporting;
using HtmlAgilityPack;
using log4net;
using Newtonsoft.Json;

namespace Common.Controllers
{
    public class IgnitionController799 : IIgnitionController
    {
        private readonly EgnServer _enEgnServer;
        private CookieContainer _cookies;
        private static bool _alreadyAttemptedModuleRestart;
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public IgnitionController799(EgnServer enEgnServer)
        {
            _enEgnServer = enEgnServer;
            Login();
        }

        private void Login()
        {
            try
            {
                var requestUriString = GetUrl(_enEgnServer.Server, "/main/web/signin");
                var http = WebRequest.Create(requestUriString) as HttpWebRequest;

                http.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
                http.UserAgent = "Mozilla/5.0 (Windows NT 6.1; Win64; x64; rv:63.0) Gecko/20100101 Firefox/63.0";
                http.Headers["Accept-Encoding"] = "gzip, deflate";
                http.Headers["Accept-Language"] = "en-US,en;q=0.5";

                Debug.Assert(http != null, "http != null");
                http.AllowAutoRedirect = true;
                http.MaximumAutomaticRedirections = 100;
                http.CookieContainer = new CookieContainer();
                var webResponse = (HttpWebResponse) http.GetResponse();

                _cookies = http.CookieContainer;

                var htmlText = new StreamReader(webResponse.GetResponseStream()).ReadToEnd();
                var hookPoint = htmlText.IndexOf("IFormSubmitListener-sign~in~form");
                var start = htmlText.LastIndexOf("?", hookPoint);
                var end = htmlText.IndexOf(".", start);
                var length = end - start + 1;
                var substring = htmlText.Substring(start, length);
                var url = GetUrl(_enEgnServer.Server, $"/main/web/signin{substring}IFormSubmitListener-sign~in~form");
                http = WebRequest.Create(url) as HttpWebRequest;
                http.Method = "POST";
                http.ContentType = "application/x-www-form-urlencoded";
                http.CookieContainer = _cookies;
                var postData = "id3_hf_0=&username=admin&password=password&login=Login";
                var dataBytes = Encoding.UTF8.GetBytes(postData);
                http.ContentLength = dataBytes.Length;
                using (var postStream = http.GetRequestStream())
                {
                    postStream.Write(dataBytes, 0, dataBytes.Length);
                }
            }
            catch (Exception)
            {
            }
        }

        public bool IsLoggedIn()
        {
            return _cookies != null;
        }

        private static string GetUrl(string host, string path)
        {
            return $"http://{host}:8088{path}";
        }

        public string GetHtmlContent(string path)
        {
            try
            {
                var s = GetUrl(_enEgnServer.Server, path);
                var http = WebRequest.Create(s) as HttpWebRequest;
                http.Timeout = 1000 * 60 * 3;
                http.CookieContainer = _cookies;
                var webResponse = http.GetResponse() as HttpWebResponse;
                var text = new StreamReader(webResponse.GetResponseStream()).ReadToEnd();
                return text;
            }
            catch (Exception e)
            {
                try
                {
                    Log.Error(e.Message);
                    // TODO: This is kind of gross/hacky...
                    Log.Warn("Stopping Ignition service");
                    _enEgnServer.ServiceController.Stop("Ignition");
                    Thread.Sleep(TimeSpan.FromMinutes(1));
                    Log.Warn("Starting Ignition service");
                    _enEgnServer.ServiceController.Start("Ignition");
                    Thread.Sleep(TimeSpan.FromMinutes(2));
                    Login();
                    var s = GetUrl(_enEgnServer.Server, path);
                    var http = WebRequest.Create(s) as HttpWebRequest;
                    http.Timeout = 1000 * 60 * 3;
                    http.CookieContainer = _cookies;
                    var webResponse = http.GetResponse() as HttpWebResponse;
                    var text = new StreamReader(webResponse.GetResponseStream()).ReadToEnd();
                    return text;
                }
                catch (Exception)
                {
                    return "";
                }
            }
        }

        public string DeleteHtmlContent(string path)
        {
            try
            {
                var s = GetUrl(_enEgnServer.Server, path);
                var http = WebRequest.Create(s) as HttpWebRequest;
                http.Timeout = 1000 * 60 * 3;
                http.CookieContainer = _cookies;
                http.Method = "DELETE";
                var webResponse = http.GetResponse() as HttpWebResponse;
                var text = new StreamReader(webResponse.GetResponseStream()).ReadToEnd();
                return text;
            }
            catch (Exception)
            {
                return "";
            }
        }

        public string PutHtmlContent(string path)
        {
            try
            {
                var s = GetUrl(_enEgnServer.Server, path);
                var http = WebRequest.Create(s) as HttpWebRequest;
                http.Timeout = 1000 * 60 * 3;
                http.CookieContainer = _cookies;
                http.Method = "PUT";
                var webResponse = http.GetResponse() as HttpWebResponse;
                var text = new StreamReader(webResponse.GetResponseStream()).ReadToEnd();
                return text;
            }
            catch (Exception)
            {
                return "";
            }
        }

        public int GetPruneAge()
        {
            var doc = new HtmlDocument();
            var htmlContent = GetHtmlContent("/main/web/config/tags.history");
            doc.LoadHtml(htmlContent);
            var tableBody = doc.DocumentNode.SelectSingleNode("//tbody");
            //First row is probably splitter. Fifth table detail is the action section that has a single action (edit) and anchor link.
            var action = tableBody.SelectSingleNode("tr").SelectNodes("td")[5].SelectSingleNode("div")
                .SelectSingleNode("a");
            var editUrl = "/main/web/config" + action.GetAttributeValue("href", "").Substring(1);
            var content = GetHtmlContent(editUrl);
            doc.LoadHtml(content);
            var pruneAgeValue = doc.DocumentNode.SelectNodes("//table")[2]
                .SelectSingleNode("tbody")
                .SelectNodes("tr")[1]
                .SelectNodes("td")[1]
                .SelectSingleNode("input")
                .GetAttributeValue("value", "");
            return int.Parse(pruneAgeValue);
        }

        public string SetGatewayName()
        {
            var message = "";
            var form = GetFormWithDefaults("/main/web/config/system.settings", out var formActionUrl);
            var gatewayName = $"Rig {_enEgnServer.RigNumber}";
            if (gatewayName != form["category-table:1:field:1:editor"])
            {
                var rs = JsonConvert.SerializeObject(form, Formatting.Indented,
                    new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
                Log.Warn(rs);
                message = "Update Gateway Settings.";
                message += $"\nSystem Name. Old: {form["category-table:1:field:1:editor"]} New: {gatewayName}";
                form["category-table:1:field:1:editor"] = gatewayName;
                PostForm(formActionUrl, form);
            }
            return message;
        }

        public string SetDbTranslation()
        {
            var ignitionConfig = IgnitionConfig.GetConfig();
            var message = "";

            var htmlContent = GetHtmlContent("/main/web/config/database.drivers");
            var doc = new HtmlDocument();
            doc.LoadHtml(htmlContent);
            var translatorTab = doc.DocumentNode.SelectNodes("//a")
                .FirstOrDefault(x => x.Attributes["href"].Value.Contains("contents-tab-2-tablink"))
                ?.Attributes["href"].Value.Substring(1);
            htmlContent = GetHtmlContent("/main/web/config" + translatorTab);
            doc.LoadHtml(htmlContent);
            var mySqlRow = doc.DocumentNode.SelectNodes("//tr").FirstOrDefault(x => x.OuterHtml.Contains("MYSQL"));
            var editButton = mySqlRow.SelectNodes("td")[1].SelectSingleNode("div").SelectNodes("a")
                .FirstOrDefault(x => x.InnerText.Contains("edit")).Attributes["href"].Value.Substring(1);
            var form = GetFormWithDefaults("/main/web/config" + editButton, out var formActionUrl);
            if (ignitionConfig.Databases.Translators.MYSQL.DataTypeMappingString != form["category-table:2:field:9:editor"])
            {
                message = $"Update Databases/Translators/MYSQL/DataTypeMapping\nString Old: {form["category-table:2:field:9:editor"]} New: {ignitionConfig.Databases.Translators.MYSQL.DataTypeMappingString}";
                form["category-table:2:field:9:editor"] = ignitionConfig.Databases.Translators.MYSQL.DataTypeMappingString;
                PostForm(formActionUrl, form);
            }
            return message;
        }

        public string CreateDbConnection()
        {
            var htmlContent = GetHtmlContent("/main/web/config/database.connections");
            if (!htmlContent.Contains("No Database Connections")) return "";
            var doc = new HtmlDocument();
            doc.LoadHtml(htmlContent);
            var actionLink = doc.DocumentNode.SelectNodes("//a")
                .FirstOrDefault(x => x.Attributes["href"].Value.Contains("action-1-link"))
                ?.Attributes["href"].Value.Substring(1);
            var dbChoiceForm = GetFormWithDefaults("/main/web/config" + actionLink, out var formActionUrl);
            //TODO find the 'mysql' choice. don't assume it'll be zero.
            dbChoiceForm["choice"] = "0";
            var postForm = PostForm(formActionUrl, dbChoiceForm);
            doc.LoadHtml(postForm);
            var dbConnectionForm = GetFormWithDefaults(doc, out formActionUrl);
            var connection = IgnitionConfig.GetConfig().Databases.Connections.MYSQL;
            dbConnectionForm["category-table:1:field:1:editor"] = connection.Name;
            dbConnectionForm["category-table:1:field:4:editor"] = connection.ConnectUrl;
            dbConnectionForm["category-table:1:field:5:editor"] = connection.Username;
            dbConnectionForm["category-table:1:field:6:cell1:password1"] = Utility.DecryptString(connection.Password);
            dbConnectionForm["category-table:1:field:6:cell2:password2"] = Utility.DecryptString(connection.Password);
            PostForm(formActionUrl, dbConnectionForm);
            return "Config Database Connections\nCreate local mysql connection";
        }

        public string WebSandfEndpoint()
        {
            var ignitionConfig = IgnitionConfig.GetConfig();
            var htmlContent = GetHtmlContent("/main/web/config/websf.websf_remote");
            Log.Debug("----");
            Log.Debug(htmlContent);
            Log.Debug("----");
            var message = "";
            foreach (var endpoint in ignitionConfig.WebStoreAndForward.Endpoints)
            {
                if (htmlContent.Contains(endpoint.Name))
                {
                    message += DoSandFUpdate(endpoint, htmlContent);
                }
                else
                {
                    message += DoSandFCreate(endpoint, htmlContent);
                }
            }
            return message;
        }

        private string DoSandFUpdate(Endpoint endpoint, string htmlContent)
        {
            var message = "";
            var doc = new HtmlDocument();
            doc.LoadHtml(htmlContent);
            foreach (var tableBody in doc.DocumentNode.SelectNodes("//tbody"))
            foreach (var row in tableBody.SelectNodes("tr"))
            {
                var tableData = row.SelectNodes("td");
                if (tableData != null && tableData[0].InnerText == endpoint.Name)
                {
                    var actionList = GetActionList(tableData[2]);
                    var actionUrl = actionList.FirstOrDefault(x => x.StartsWith("edit"))?.Split(';')[1];
                    doc.LoadHtml(GetHtmlContent(actionUrl));
                    var endpointConfig = GetFormWithDefaults("/main/web/config" + actionUrl, out var formActionUrl);
                    if (endpoint.RemoteEndpoint != endpointConfig["category-table:2:field:1:editor"])
                    {
                        if (message == "") message = "Config Web S&F Endpoint";
                        if (!message.Contains($"\nUpdate endpoint: {endpoint.Name}")) message += $"\nUpdate endpoint: {endpoint.Name}";
                        message += $"\nRemoteEndpoint Old:{endpointConfig["category-table:2:field:1:editor"]} New:{endpoint.RemoteEndpoint}";
                        endpointConfig["category-table:2:field:1:editor"] = endpoint.RemoteEndpoint;
                    }
                    if (endpoint.ForwardFrequencyUnits != null && GetForwardFrequencyUnitCode(endpoint.ForwardFrequencyUnits) != endpointConfig["category-table:2:field:3:editor"])
                    {
                        if (message == "") message = "Config Web S&F Endpoint";
                        if (!message.Contains($"\nUpdate endpoint: {endpoint.Name}")) message += $"\nUpdate endpoint: {endpoint.Name}";
                        message += $"\nForwardFrequencyUnits Old:{GetForwardFrequencyUnitDesc(endpointConfig["category-table:2:field:3:editor"])} New:{endpoint.ForwardFrequencyUnits}";
                        endpointConfig["category-table:2:field:3:editor"] = GetForwardFrequencyUnitCode(endpoint.ForwardFrequencyUnits);
                    }
                    if (endpoint.ForwardFrequencyTime != null && endpoint.ForwardFrequencyTime != int.Parse(endpointConfig["category-table:2:field:2:editor"]))
                    {
                        if (message == "") message = "Config Web S&F Endpoint";
                        if (!message.Contains($"\nUpdate endpoint: {endpoint.Name}")) message += $"\nUpdate endpoint: {endpoint.Name}";
                        message += $"\nForwardFrequencyTime Old:{endpointConfig["category-table:2:field:2:editor"]} New:{endpoint.ForwardFrequencyTime}";
                        endpointConfig["category-table:2:field:2:editor"] = endpoint.ForwardFrequencyTime.ToString();
                    }
                    if(message.Contains($"\nUpdate endpoint: {endpoint.Name}"))
                        PostForm(formActionUrl, endpointConfig);
                }
            }
            return message;
        }
        private string DoSandFCreate(Endpoint endpoint, string htmlContent)
        {
            var message = "";
            if (message == "") message = "Config Web S&F Endpoint";
            message += $"\nCreate endpoint: {endpoint.Name}";
            var doc = new HtmlDocument();
            doc.LoadHtml(htmlContent);
            var actionLink = doc.DocumentNode.SelectNodes("//a")
                .FirstOrDefault(x => x.Attributes["href"].Value.Contains("action-1-link"))
                ?.Attributes["href"].Value.Substring(1);
            var endpointConfig = GetFormWithDefaults("/main/web/config" + actionLink, out var formActionUrl);
            endpointConfig["category-table:1:field:1:editor"] = endpoint.Name;
            endpointConfig["category-table:2:field:1:editor"] = endpoint.RemoteEndpoint;
            if (endpoint.ForwardFrequencyTime != null)
                endpointConfig["category-table:2:field:2:editor"] = endpoint.ForwardFrequencyTime.ToString();

            if (endpoint.ForwardFrequencyUnits != null)
            {
                endpointConfig["category-table:2:field:3:editor"] = GetForwardFrequencyUnitCode(endpoint.ForwardFrequencyUnits);
            }
            PostForm(formActionUrl, endpointConfig);
            return message;
        }

        private string GetForwardFrequencyUnitCode(string units)
        {
            string result;
            switch (units)
            {
                case "Milliseconds":
                    result = "0";
                    break;
                case "Seconds":
                    result = "1";
                    break;
                case "Minutes":
                    result = "2";
                    break;
                case "Hours":
                    result = "3";
                    break;
                case "Days":
                    result = "4";
                    break;
                case "Weeks":
                    result = "5";
                    break;
                case "Months":
                    result = "6";
                    break;
                case "Years":
                    result = "7";
                    break;
                default:
                    throw new NotImplementedException();
            }
            return result;
        }
        private string GetForwardFrequencyUnitDesc(string units)
        {
            string result;
            switch (units)
            {
                case "0":
                    result = "Milliseconds";
                    break;
                case "1":
                    result = "Seconds";
                    break;
                case "2":
                    result = "Minutes";
                    break;
                case "3":
                    result = "Hours";
                    break;
                case "4":
                    result = "Days";
                    break;
                case "5":
                    result = "Weeks";
                    break;
                case "6":
                    result = "Months";
                    break;
                case "7":
                    result = "Years";
                    break;
                default:
                    throw new NotImplementedException();
            }
            return result;
        }


        public string CreateSplitter()
        {
            var ignitionConfig = IgnitionConfig.GetConfig();
            var htmlContent = GetHtmlContent("/main/web/config/tags.history");
            var doc = new HtmlDocument();
            doc.LoadHtml(htmlContent);
            var htmlTable = doc.DocumentNode.SelectSingleNode("//table");
            if (htmlTable.InnerHtml.Contains(ignitionConfig.Tags.Splitter.Name)) return "";
            var actionLink = doc.DocumentNode.SelectNodes("//a")
                .FirstOrDefault(x => x.Attributes["href"].Value.Contains("action-1-link"))
                ?.Attributes["href"].Value.Substring(1);
            var providerChoice = GetFormWithDefaults("/main/web/config" + actionLink, out var formActionUrl);
            providerChoice["choice"] = "SplittingProvider";
            var postForm = PostForm(formActionUrl, providerChoice);
            doc.LoadHtml(postForm);
            var historyForm = GetFormWithDefaults(doc, out formActionUrl);
            historyForm["category-table:1:field:1:editor"] = ignitionConfig.Tags.Splitter.Name;
            historyForm["category-table:2:field:1:editor"] = GetSelectValue(postForm, "category-table:2:field:1:editor", ignitionConfig.Tags.Splitter.FirstConnection);
            historyForm["category-table:2:field:2:editor"] = GetSelectValue(postForm, "category-table:2:field:2:editor", ignitionConfig.Tags.Splitter.SecondConnection);
            PostForm(formActionUrl, historyForm);
            return "Config History Provider\nCreate splitter";
        }

        private string GetSelectValue(string html, string selectName, string text)
        {
            var start = html.IndexOf($"<select name=\"{ selectName}\">");
            var end = html.IndexOf("</select>", start);
            var thing = html.Substring(start, end - start).Split('\n').ToList().FirstOrDefault(x => x.Contains("value") && x.Contains(text));
            start = thing.IndexOf("\"")+1;
            end = thing.IndexOf("\">", start);
            var value = thing.Substring(start, end - start);
            return value;
        }

        public string SetDataPruning()
        {
            var message = "";
            var htmlContent = GetHtmlContent("/main/web/config/tags.history");
            var doc = new HtmlDocument();
            doc.LoadHtml(htmlContent);
            var actionLink = doc.DocumentNode.SelectNodes("//a")
                .FirstOrDefault(x => x.Attributes["href"].Value.Contains("rows-0-actions-primary"))
                ?.Attributes["href"].Value.Substring(1);
            var historySettings = GetFormWithDefaults("/main/web/config" + actionLink, out var formActionUrl);
            // Only if one of the four expected settings is not set will we initialize the prune age value.
            // This is in case it has been adjusted due to disk constraints and doesn't match the default of 30.
            if (historySettings["category-table:2:field:3:editor"] != "4" || historySettings["category-table:3:field:1:editor"] != "on" ||
                historySettings["category-table:3:field:3:editor"] != "4" || historySettings["category-table:2:field:2:editor"] != "1")
            {
                if (historySettings["category-table:3:field:2:editor"] != "30")
                {
                    if (message == "") message = "Config Tag History (local)";
                    message += $"\nPrune Age Old: {historySettings["category-table:3:field:2:editor"]} New: 30";
                    historySettings["category-table:3:field:2:editor"] = "30";
                }
            }

            if (historySettings["category-table:2:field:2:editor"] != "1")
            {
                if (message == "") message = "Config Tag History (local)";
                message += $"\nPartition Length Old: {historySettings["category-table:2:field:2:editor"]} New: 1";
                historySettings["category-table:2:field:2:editor"] = "1";
            }
            if (historySettings["category-table:2:field:3:editor"] != "4")
            {
                if (message == "") message = "Config Tag History (local)";
                message += $"\nPartition Units Old: {historySettings["category-table:2:field:3:editor"]} New: Days";
                historySettings["category-table:2:field:3:editor"] = "4"; //TODO: don't hard code; find days.
            }
            if (historySettings["category-table:3:field:1:editor"] != "on")
            {
                if (message == "") message = "Config Tag History (local)";
                message += $"\nEnable Data Pruning Old: {historySettings["category-table:3:field:1:editor"]} New: on";
                historySettings["category-table:3:field:1:editor"] = "on"; 
            }
            if (historySettings["category-table:3:field:3:editor"] != "4")
            {
                if (message == "") message = "Config Tag History (local)";
                message += $"\nPrune Age Units Old: {historySettings["category-table:3:field:3:editor"]} New: Days";
                historySettings["category-table:3:field:3:editor"] = "4"; //TODO: don't hard code; find days.
            }

            if (!string.IsNullOrEmpty(message))
            {
                PostForm(formActionUrl, historySettings);
            }
            return message;
        }

        public void SetModuleQuarantine()
        {
            var htmlContent = GetHtmlContent("/main/web/config/system.modules");
            var doc = new HtmlDocument();
            doc.LoadHtml(htmlContent);
            var actions = doc.DocumentNode.SelectNodes("//a");
            foreach (var action in actions)
            {
                var actionAttribute = action.Attributes["href"]?.Value;
                if (actionAttribute != null && actionAttribute.Contains("actions-primary") &&
                    actionAttribute.Contains("quarantine"))
                {
                    htmlContent =
                        GetHtmlContent("/main/web/config" + actionAttribute.Substring(1)); // Gets us to confirm page
                    doc = new HtmlDocument();
                    doc.LoadHtml(htmlContent);
                    var confirmLink = doc.DocumentNode.SelectNodes("//a")
                        .FirstOrDefault(x => x.Attributes["href"].Value.Contains("confirm"))
                        ?.Attributes["href"].Value.Substring(1);
                    htmlContent = GetHtmlContent("/main/web/config" + confirmLink);
                    throw new NotImplementedException(); //TODO: On next install try and fix this.
                    //Pretty sure we just need to visit these two links. Chrome showed 'get' only; not form posting.
                    //http://egn21-vp-hist01:8088/main/web/config/system.modules?13-3.ILinkListener-config~contents-button
                    //http://egn21-vp-hist01:8088/main/web/config/system.modules?16-4.ILinkListener-config~contents-installButton
                    //    var confirmForm = GetFormWithDefaults("/main/web/config" + confirmLink, out var formActionUrl);
                    //    confirmForm["config-contents:checkbox"] = "";
                }
            }
        }

        private NameValueCollection GetFormWithDefaults(string url, out string formActionUrl)
        {
            var htmlContent = GetHtmlContent(url);
            var doc = new HtmlDocument();
            doc.LoadHtml(htmlContent);
            var nodes = doc.DocumentNode.SelectNodes("//input");
            var form = doc.DocumentNode.SelectSingleNode("//form");
            formActionUrl = "/main/web/config" +
                            form.Attributes.FirstOrDefault(x => x.Name == "action")?.Value.Substring(1);

            var outgoingQueryString = HttpUtility.ParseQueryString(string.Empty);
            foreach (var variable in nodes)
            {
                var name = variable.Attributes["name"]?.Value;
                var value = variable.Attributes["value"]?.Value;
                if (variable.Attributes["checked"]?.Value == "checked")
                    value = "on";
                if (name != null)
                    outgoingQueryString.Add(name, value);
            }

            var selectNodes = doc.DocumentNode.SelectNodes("//select");
            if (selectNodes == null) return outgoingQueryString;
            foreach (var selectNode in selectNodes)
            {
                var collection = selectNode.SelectNodes("//option");
                foreach (var variable in collection)
                    if (variable.Attributes["selected"]?.Value == "selected")
                    {
                        var name = selectNode.Attributes["name"]?.Value;
                        var value = variable.Attributes["value"]?.Value;
                        outgoingQueryString.Add(name, value);
                        break;
                    }
            }

            return outgoingQueryString;
        }

        private NameValueCollection GetFormWithDefaults(HtmlDocument doc, out string formActionUrl)
        {
            var nodes = doc.DocumentNode.SelectNodes("//input");
            var form = doc.DocumentNode.SelectSingleNode("//form");
            formActionUrl = "/main/web/config" +
                            form.Attributes.FirstOrDefault(x => x.Name == "action")?.Value.Substring(1);

            var outgoingQueryString = HttpUtility.ParseQueryString(string.Empty);
            foreach (var variable in nodes)
            {
                var name = variable.Attributes["name"]?.Value;
                var value = variable.Attributes["value"]?.Value;
                if (variable.Attributes["checked"]?.Value == "checked")
                    value = "on";
                if (name != null)
                    outgoingQueryString.Add(name, value);
            }

            var selectNodes = doc.DocumentNode.SelectNodes("//select");
            if (selectNodes == null) return outgoingQueryString;
            foreach (var selectNode in selectNodes)
            {
                var collection = selectNode.SelectNodes("//option");
                foreach (var variable in collection)
                    if (variable.Attributes["selected"]?.Value == "selected")
                    {
                        var name = selectNode.Attributes["name"]?.Value;
                        var value = variable.Attributes["value"]?.Value;
                        outgoingQueryString.Add(name, value);
                        break;
                    }
            }

            return outgoingQueryString;
        }

        public List<StoreAndForwardQuarantine> GetStoreAndForwardQuarantines()
        {
            var quarantines = new List<StoreAndForwardQuarantine>();
            try
            {
                var htmlContent = GetHtmlContent("/main/data/status/store_forward");
                var ignitionLicense = JsonConvert.DeserializeObject<IgnitionStoreForward>(htmlContent);
                foreach (var store in ignitionLicense.stores.Where(x => x.quarantined > 0))
                {
                    var storeAndForwardQuarantine = new StoreAndForwardQuarantine
                        {DataType = store.storeName, TxnCount = store.quarantined};
                    quarantines.Add(storeAndForwardQuarantine);
                    //DeleteHtmlContent("/main/data/status/store_forward_unquarantine_id/local/1");
                }
            }
            catch (Exception)
            {
                /* ignored */
            }

            return quarantines;
        }

        public List<Modules> GetModules()
        {
            var modules = new List<Modules>();
            try
            {
                var doc = new HtmlDocument();
                var htmlContent = GetHtmlContent("/main/web/config/system.modules");
                if (string.IsNullOrEmpty(htmlContent))
                    return modules;
                doc.LoadHtml(htmlContent);
                foreach (var tableBody in doc.DocumentNode.SelectNodes("//tbody"))
                foreach (var row in tableBody.SelectNodes("tr"))
                {
                    var tableData = row.SelectNodes("td");
/*
                    if (tableData[0].InnerText == "Web Store and Forward")
                    {
                        var actionList = GetActionList(tableData[4]);
                    }

*/
                    if (tableData != null)
                    {
                        var item = new Modules
                        {
                            Name = tableData[0].InnerText,
                            Version = tableData[1].InnerText,
                            Description = tableData[2].InnerText,
                            License = tableData[3].InnerText,
                            State = tableData[4].InnerText.Replace("\r", string.Empty).Replace("\t", string.Empty)
                                .Replace("\n", string.Empty)
                        };
                        try
                        {
                            item.Actions = GetActionList(tableData[5]);
                        }
                        catch (Exception)
                        {
                            item.Actions = GetActionList(tableData[4]);
                        }

                        modules.Add(item);
                    }
                }
            }
            catch (Exception)
            {
            }

            return modules;
        }

        private List<string> GetActionList(HtmlNode node)
        {
            var strActionList = new List<string>();
            var actions = node.SelectSingleNode("div").SelectNodes("a")
                .Where(x => x.InnerText != string.Empty && x.InnerText != "More").ToList();
            foreach (var action in actions)
                strActionList.Add(action.InnerText + ";" + "/main/web/config" +
                                  action.GetAttributeValue("href", "").Substring(1));
            try
            {
                var moreActions = node.SelectSingleNode("div").SelectSingleNode("div").SelectSingleNode("ul")
                    .SelectNodes("li").Where(x => x.InnerText != string.Empty).ToList();
                foreach (var htmlNode in moreActions)
                {
                    var href = "/main/web/config" +
                               htmlNode.SelectSingleNode("a").GetAttributeValue("href", "").Substring(1);
                    var text = htmlNode.SelectSingleNode("a").InnerText.Replace("\r", string.Empty)
                        .Replace("\t", string.Empty).Replace("\n", string.Empty).Replace("&nbsp;", " ");
                    strActionList.Add(text + ";" + href);
                }
            }
            catch (Exception)
            {
                /* Ignored */
            }

            return strActionList;
        }


        public List<DatabaseConnection> GetDatabaseConnections()
        {
            var databaseConnections = new List<DatabaseConnection>();
            try
            {
                var doc = new HtmlDocument();
                var htmlContent = GetHtmlContent("/main/web/config/database.connections");
                doc.LoadHtml(htmlContent);
                foreach (var tableBody in doc.DocumentNode.SelectNodes("//tbody"))
                foreach (var row in tableBody.SelectNodes("tr"))
                {
                    var tableData = row.SelectNodes("td");
                    databaseConnections.Add(new DatabaseConnection
                    {
                        Name = tableData[0].InnerText,
                        Description = tableData[1].InnerText.Replace("&nbsp;", string.Empty),
                        JdbcDriver = tableData[2].InnerText,
                        Translator = tableData[3].InnerText,
                        Status = tableData[4].InnerText
                    });
                }
            }
            catch (Exception)
            {
            }

            return databaseConnections;
        }

        public void InvokeModuleAction(string moduleName, string action)
        {
            EsiLog.Info(_enEgnServer, $"Invoking {action} of module {moduleName}.");
            var module = GetModules().FirstOrDefault(x => x.Name == moduleName);
            if (module != null)
            {
                var actionUrl = module.Actions.FirstOrDefault(x => x.StartsWith(action))?.Split(';')[1];
                var doc = new HtmlDocument();
                doc.LoadHtml(GetHtmlContent(actionUrl));
                var confirmAnchor = doc.DocumentNode.SelectSingleNode(@"//div[@class=""dialog-body""]")
                    .SelectSingleNode(@"//a[@class=""primary block button""]");
                var confirmUrl = "/main/web/config" + confirmAnchor.GetAttributeValue("href", "").Substring(1);
                var htmlContent = GetHtmlContent(confirmUrl);
/*
                if (htmlContent.Contains("Add Certificate and Install Module"))
                {
                    var installUrl = confirmUrl.Replace("-confirm", "installButton");
                    var content = GetHtmlContent(installUrl);
                }
*/
            }
        }

        public void InvokeOpcUaAction(string opcServerName, string action)
        {
            EsiLog.Info(_enEgnServer, $"Invoking action '{action}' of OPC Server '{opcServerName}'.");
            var opcServer = GetOpcServers().FirstOrDefault(x => x.Name == opcServerName);
            if (opcServer != null)
                try
                {
                    var endpointUrl = opcServer.Actions.FirstOrDefault(x => x.StartsWith(action)).Split(';')[1];
                    NameValueCollection discoverForm;
                    //Click 'endpoint', fill form with click 'Discover'
                    var discoverActionUrl = FormAction(GetHtmlContent(endpointUrl), out discoverForm, 0);
                    var endpointChoicesContent = PostForm(discoverActionUrl, discoverForm);
                    if (endpointChoicesContent.Contains("ConnectTimeoutException"))
                    {
                        EsiLog.Error(_enEgnServer,
                            $"Failed to rediscover endpoint {opcServerName}: ConnectTimeoutException");
                        return;
                    }

                    NameValueCollection endpointChoicesForm;
                    // Select first endpoint and click 'next'
                    var nextActionUrl = FormAction(endpointChoicesContent, out endpointChoicesForm, 1);
                    var newSettingsPage = PostForm(nextActionUrl, endpointChoicesForm);

                    NameValueCollection saveSettingsForm;
                    // Click 'save settings'
                    var saveActionUrl = FormAction(newSettingsPage, out saveSettingsForm, 0);
                    var confirmContent = PostForm(saveActionUrl, saveSettingsForm);
                    var successMessage = "Successfully updated OPC Server";
                    if (confirmContent.Contains(successMessage))
                        EsiLog.Info(_enEgnServer, $"{successMessage} {opcServerName}");
                }
                catch (Exception)
                {
                    EsiLog.Error(_enEgnServer,
                        $"Failed to rediscover endpoint {opcServerName}: ConnectTimeoutException");
                }
        }

        private string PostForm(string formActionUrl, NameValueCollection outgoingQueryString)
        {
            var http = WebRequest.Create(GetUrl(_enEgnServer.Server, formActionUrl)) as HttpWebRequest;
            http.Method = "POST";
            http.ContentType = "application/x-www-form-urlencoded";
            http.CookieContainer = _cookies;
            var dataBytes = Encoding.UTF8.GetBytes(outgoingQueryString.ToString());
            http.ContentLength = dataBytes.Length;
            using (var postStream = http.GetRequestStream())
            {
                postStream.Write(dataBytes, 0, dataBytes.Length);
            }

            var webResponse = http.GetResponse();
            var htmlContent = new StreamReader(webResponse.GetResponseStream()).ReadToEnd();
            return htmlContent;
        }

        private static string FormAction(string htmlContent, out NameValueCollection outgoingQueryString, int id)
        {
            //HtmlNode.ElementsFlags.Remove("form");
            var doc = new HtmlDocument();
            doc.LoadHtml(htmlContent);
            var htmlNodeCollection = doc.DocumentNode.SelectNodes("//form");
            var form = htmlNodeCollection[id];
            //var form = doc.GetElementbyId(id);
            var formAction = "/main/web/config" +
                             form.Attributes.FirstOrDefault(x => x.Name == "action")?.Value.Substring(1);
            var nodes = form.SelectNodes("//input");
            outgoingQueryString = HttpUtility.ParseQueryString(string.Empty);

            // I have no idea why when two froms are on a page that this thing doesn't just get inputs from the form I specify!!
            var htmlNodes = id == 2 ? nodes.Skip(3).ToList() : nodes.ToList();
            var saveFormOmitFromSubmission = new List<string>
            {
                //TODO: Enabled must be set here...
                "category-table:1:field:3:editor",
                "category-table:2:field:2:editor",
                "category-table:2:field:2:checkbox",
                "category-table:2:field:2:cell1:password1",
                "category-table:2:field:2:cell2:password2",
                "adv"
            };
            foreach (var variable in htmlNodes)
            {
                var name = variable.Attributes["name"]?.Value;
                var value = variable.Attributes["value"]?.Value;
                if (id == 1 && name == "endpoint-choice" && value == "1")
                    continue;
                if (id == 1 && saveFormOmitFromSubmission.Contains(name))
                    continue;
                if (id == 0 && name == "category-table:1:field:4:editor")
                    value = "on";
                if (name != null)
                    outgoingQueryString.Add(name, value);
            }

            return formAction;
        }

        public void InvokeQuarantineAction(string dataType, string action)
        {
            // First retry the quarantined items.
            var htmlContent = GetHtmlContent($"/main/data/status/store_forward_detail/{dataType}");
            var detail = JsonConvert.DeserializeObject<QuarantineDetail>(htmlContent);
            foreach (var item in detail.quarantinedItems)
                DeleteHtmlContent($"/main/data/status/store_forward_unquarantine_id/{dataType}/{item.id}");
            Thread.Sleep(10000);
            //If that doesn't work then delete them.
            /*htmlContent = GetHtmlContent($"/main/data/status/store_forward_detail/{dataType}");
            detail = JsonConvert.DeserializeObject<QuarantineDetail>(htmlContent);
            foreach (var item in detail.quarantinedItems)
            {
                DeleteHtmlContent($"/main/data/status/store_forward_delete_id/{dataType}/{item.id}");
            }*/
        }

        public List<OpcServer> GetOpcServers()
        {
            var opcServers = new List<OpcServer>();
            try
            {
                var doc = new HtmlDocument();
                doc.LoadHtml(GetHtmlContent("/main/web/config/opc.connections"));
                var node = doc.DocumentNode.SelectSingleNode("//tbody");
                foreach (var row in node.SelectNodes("tr"))
                {
                    var tableData = row.SelectNodes("td");
                    var opcServer = new OpcServer
                    {
                        EgnHost = _enEgnServer.Server,
                        RigNumber = _enEgnServer.RigNumber,
                        Name = tableData[0].InnerText,
                        Description = tableData[2].InnerText,
                        Status = tableData[4].InnerText,
                        Actions = GetActionList(tableData[5])
                    };
                    opcServers.Add(opcServer);
                }
            }
            catch (Exception)
            {
                /* ignored */
            }

            return opcServers;
        }

        public string GetLicenseInfo()
        {
            try
            {
                var htmlContent = GetHtmlContent("/main/data/status/license");
                return htmlContent;
            }
            catch (Exception)
            {
            }

            return null;
        }

        public void RestartModules()
        {
            if (_alreadyAttemptedModuleRestart)
                return;
            try
            {
                _alreadyAttemptedModuleRestart = true;
                InvokeModuleAction("OPC-UA", "restart");
                InvokeModuleAction("Web Store and Forward", "restart");
            }
            catch (Exception)
            {
                // ignored
            }
        }

        public void ReDiscoverEndpoints()
        {
            var opcServers = GetOpcServers(); //.Where(x => x.Status == "Connected");
            foreach (var opcServer in opcServers) InvokeOpcUaAction(opcServer.Name, "endpoint");
        }
    }
}