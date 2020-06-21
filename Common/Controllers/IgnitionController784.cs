using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using Common.Models.Ignition;
using Common.Models.Reporting;
using HtmlAgilityPack;

namespace Common.Controllers
{
    public class IgnitionController784 : IIgnitionController
    {
        private readonly EgnServer _enEgnServer;
        private CookieContainer _cookies;
        private static bool _alreadyAttemptedModuleRestart;

        public IgnitionController784(EgnServer enEgnServer)
        {
            _enEgnServer = enEgnServer;
            Login();
        }

        private void Login()
        {
            try
            {
                var http = WebRequest.Create(GetUrl(_enEgnServer.Server, "/main/web/login")) as HttpWebRequest;
                Debug.Assert(http != null, "http != null");
                var webResponse = http.GetResponse();

                var htmlText = new StreamReader(webResponse.GetResponseStream()).ReadToEnd();
                var hookPoint = htmlText.IndexOf("IFormSubmitListener-signInForm");
                var start = htmlText.LastIndexOf("?", hookPoint);
                var end = htmlText.IndexOf(".", start);
                var length = end - start + 1;
                var substring = htmlText.Substring(start, length);
                var url = GetUrl(_enEgnServer.Server, $"/main/web/login{substring}IFormSubmitListener-signInForm");

                _cookies = new CookieContainer();
                string jSessionId = $"{webResponse.ResponseUri.AbsolutePath.Split(';')[1]}";
                var strings = jSessionId.Split('=');
                var target = new Uri($"http://{_enEgnServer.Server}/");
                _cookies.Add(new Cookie(strings[0], strings[1]) { Domain = target.Host });

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
            catch (Exception)
            {
                return "";
            }
        }

        public string DeleteHtmlContent(string path)
        {
            throw new NotImplementedException();
        }

        public string PutHtmlContent(string path)
        {
            throw new NotImplementedException();
        }

        public int GetPruneAge()
        {
            return 0;
        }

        public string SetGatewayName()
        {
            throw new NotImplementedException();
        }

        public string SetDbTranslation()
        {
            throw new NotImplementedException();
        }

        public string CreateDbConnection()
        {
            throw new NotImplementedException();
        }

        public string WebSandfEndpoint()
        {
            throw new NotImplementedException();
        }

        public string CreateSplitter()
        {
            throw new NotImplementedException();
        }

        public string SetDataPruning()
        {
            throw new NotImplementedException();
        }

        public void SetModuleQuarantine()
        {
            throw new NotImplementedException();
        }

        public List<StoreAndForwardQuarantine> GetStoreAndForwardQuarantines()
        {
            var quarantines = new List<StoreAndForwardQuarantine>();
            try
            {
                var html1 = GetHtmlContent("/main/web/config/database.sandf");
                var doc1 = new HtmlDocument();
                doc1.LoadHtml(html1);
                // From the store and forward config page. Get the row of tabs. 
                var quarantineControlTab = doc1.DocumentNode.SelectSingleNode(@"//div[@class=""tab-row""]").SelectSingleNode("ul")
                    //Select the href value of the 2nd tab.
                    .SelectNodes("li").Skip(1).FirstOrDefault()?.SelectSingleNode("a").Attributes["href"].Value;
                var content = GetHtmlContent("/main/web/config" + quarantineControlTab?.Substring(1));

                var doc2 = new HtmlDocument();
                doc2.LoadHtml(content);
                foreach (var tableBody in doc2.DocumentNode.SelectNodes("//tbody"))
                {
                    foreach (var row in tableBody.SelectNodes("tr"))
                    {
                        var strActionList = new List<string>();
                        var tableData = row.SelectNodes("td");

                        if (tableData.Count == 4)
                        {
                            var actions = tableData[3].SelectSingleNode("ul").SelectNodes("li").Where(x => x.InnerText != String.Empty).ToList();
                            foreach (var htmlNode in actions)
                            {
                                var href = "/main/web/config" + htmlNode.SelectSingleNode("a").GetAttributeValue("href", "").Substring(1);
                                var text = htmlNode.SelectSingleNode("a").InnerText.Replace("\r", String.Empty).Replace("\t", String.Empty).Replace("\n", String.Empty).Replace("&nbsp;", " ");
                                strActionList.Add(text + ";" + href);
                            }
                        }
                        if (tableData.Count == 4)
                        {
                            quarantines.Add(new StoreAndForwardQuarantine
                            {
                                DataType = tableData[0].InnerText,
                                Problem = tableData[1].InnerText,
                                TxnCount = Convert.ToInt32(tableData[2].InnerText),
                                Actions = strActionList
                            });
                        }
                        else
                        {
                            quarantines.Add(new StoreAndForwardQuarantine
                            {
                                Problem = tableData[0].InnerText
                            });
                        }
                    }
                }

            }
            catch (Exception)
            {// ignored
            }
            return quarantines;
        }

        public List<Modules> GetModules()
        {
            var modules = new List<Modules>();
            var doc = new HtmlDocument();
            var htmlContent = GetHtmlContent("/main/web/config/conf.modules");
            if (string.IsNullOrEmpty(htmlContent))
                return modules;
            doc.LoadHtml(htmlContent);
            foreach (var tableBody in doc.DocumentNode.SelectNodes("//tbody"))
                foreach (var row in tableBody.SelectNodes("tr"))
                {
                    var tableData = row.SelectNodes("td");
                    if(tableData!= null)
                        modules.Add(new Modules
                        {
                            Name = tableData[0].InnerText,
                            Version = tableData[1].InnerText,
                            Description = tableData[2].InnerText,
                            License = tableData[3].InnerText,
                            State = tableData[4].InnerText.Replace("\r", String.Empty).Replace("\t", String.Empty).Replace("\n", String.Empty),
                            Actions = GetActionList(tableData[5])
                        });
                }
            return modules;
        }

        private List<string> GetActionList(HtmlNode node)
        {
            var actions = node.SelectSingleNode("ul").SelectNodes("li").Where(x => x.InnerText != String.Empty).ToList();
            var strActionList = new List<string>();
            foreach (var htmlNode in actions)
            {
                var href = "/main/web/config" + htmlNode.SelectSingleNode("a").GetAttributeValue("href", "").Substring(1);
                var text = htmlNode.SelectSingleNode("a").InnerText.Replace("\r", String.Empty).Replace("\t", String.Empty).Replace("\n", String.Empty).Replace("&nbsp;", " ");
                strActionList.Add(text + ";" + href);
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
                var actionUrl = module.Actions.FirstOrDefault(x => x.StartsWith(action)).Split(';')[1];
                var doc = new HtmlDocument();
                doc.LoadHtml(GetHtmlContent(actionUrl));

                var confirmButton = doc.DocumentNode.SelectSingleNode("//input");
                var confirmStart = confirmButton.OuterHtml.IndexOf("./conf.modules?");
                var confirmEnd = confirmButton.OuterHtml.IndexOf("confirm", confirmStart);
                var confirmUrl = "/main/web/config" + confirmButton.OuterHtml.Substring(confirmStart, confirmEnd - confirmStart).Substring(1) + "confirm";
                GetHtmlContent(confirmUrl);
            }
        }

        public void InvokeOpcUaAction(string opcServerName, string action)
        {
            EsiLog.Info(_enEgnServer, $"Invoking action '{action}' of OPC Server '{opcServerName}'.");
            var opcServer = GetOpcServers().FirstOrDefault(x => x.Name == opcServerName);
            if (opcServer != null)
            {
                try
                {
                    var endpointUrl = opcServer.Actions.FirstOrDefault(x => x.StartsWith(action)).Split(';')[1];
                    NameValueCollection discoverForm;
                    //Click 'endpoint', fill form with click 'Discover'
                    var discoverActionUrl = FormAction(GetHtmlContent(endpointUrl), out discoverForm, 0);
                    var endpointChoicesContent = PostForm(discoverActionUrl, discoverForm);
                    if (endpointChoicesContent.Contains("ConnectTimeoutException"))
                    {
                        EsiLog.Error(_enEgnServer, $"Failed to rediscover endpoint {opcServerName}: ConnectTimeoutException");
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
                    EsiLog.Error(_enEgnServer, $"Failed to rediscover endpoint {opcServerName}: ConnectTimeoutException");
                }
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
            var formAction = "/main/web/config" + form.Attributes.FirstOrDefault(x => x.Name == "action")?.Value.Substring(1);
            HtmlNodeCollection nodes = form.SelectNodes("//input");
            outgoingQueryString = HttpUtility.ParseQueryString(string.Empty);

            // I have no idea why when two froms are on a page that this thing doesn't just get inputs from the form I specify!!
            var htmlNodes = id == 2 ? nodes.Skip(3).ToList() : nodes.ToList();
            var saveFormOmitFromSubmission = new List<string>
            {//TODO: Enabled must be set here...
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
            EsiLog.Info(_enEgnServer, $"Invoking {action} of Store and Forward Quarantine {dataType}.");
            var quarantine = GetStoreAndForwardQuarantines().FirstOrDefault(x => x.DataType == dataType);
            var actionUrl = quarantine.Actions.FirstOrDefault(x => x.StartsWith(action)).Split(';')[1];
            var doc = new HtmlDocument();
            doc.LoadHtml(GetHtmlContent(actionUrl));
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
            }
            return opcServers;
        }

        public string GetLicenseInfo()
        {
            try
            {
                var doc = new HtmlDocument();
                doc.LoadHtml(GetHtmlContent("/main/web/config/system.licensing"));
                var node = doc.DocumentNode.SelectSingleNode("//div[@class='licensing-section license']");
                return node.InnerHtml;
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
            { // ignored
            }
        }

        public void ReDiscoverEndpoints()
        {
            var opcServers = GetOpcServers();//.Where(x => x.Status == "Connected");
            foreach (var opcServer in opcServers)
            {
                InvokeOpcUaAction(opcServer.Name, "endpoint");
            }
        }
    }
}