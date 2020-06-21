using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Threading.Tasks;
using Common.Models.Models;
using log4net;
using Opc.Ua;
using Opc.Ua.Client;

namespace Common.Controllers
{
    public class OpcUaClientNew
    {
        public readonly HistorianConfig HistorianConfig;
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private static readonly List<string> SourcesThatWork = new List<string> { "ADR Pilot", "IgnitionACE", "RigHistorian", "T-RigHistorian" };

        public OpcUaClientNew(HistorianConfig historianConfig)
        {
            HistorianConfig = historianConfig;
        }

        public void Run()
        {
            foreach (var server in HistorianConfig.OpcUaServers.Where(x=> SourcesThatWork.Contains(x.Name)))
            {
                if (!PingHost(server.Host))
                    server.Host = server.Ip;
                server.OpcTags = new List<OpcTagInfo>();
                UserIdentity userIdentity;
                if (!string.IsNullOrEmpty(server.GetUserName()))
                {
                    Log.Info($"Using UserName:{server.GetUserName()}  Password:{server.GetPassword()}");
                    userIdentity = new UserIdentity(server.GetUserName(), server.GetPassword());
                }
                else
                {
                    Log.Info("Using AnonymousIdentityToken");
                    userIdentity = new UserIdentity(new AnonymousIdentityToken());
                }
                var endpointDescription = $@"opc.tcp://{server.Host}:{server.GetPort()}{server.GetPath()}";
                Log.Info($"{server.Name} {endpointDescription}");

                var config = new ApplicationConfiguration
                {
                    ApplicationName = "Ensign",
                    ApplicationType = ApplicationType.Client,
                    SecurityConfiguration = new SecurityConfiguration
                        {ApplicationCertificate = new CertificateIdentifier(), AutoAcceptUntrustedCertificates = true},
                    ClientConfiguration = new ClientConfiguration()
                };
                config.Validate(ApplicationType.Client);
                config.CertificateValidator.CertificateValidation += (s, e) =>
                {
                    e.Accept = e.Error.StatusCode == StatusCodes.BadCertificateUntrusted;
                };

                using (Session session = Session.Create(config, new ConfiguredEndpoint(null, new EndpointDescription(endpointDescription)), true, "", 60000, userIdentity, null).Result)
                {
                    session.Browse(null, null, ObjectIds.ObjectsFolder, 0u, BrowseDirection.Forward, ReferenceTypeIds.HierarchicalReferences,
                        true, (uint) NodeClass.Variable | (uint) NodeClass.Object | (uint) NodeClass.Method, out _, out var references);
                    var ourRootStart = new List<ReferenceDescription>();

                    var omronyStuff = new List<string> { "T-RigHistorian" };
                    if (omronyStuff.Contains(server.Name))
                    {
                        ourRootStart.Add(references.FirstOrDefault(x => x.DisplayName.Text == "Trinidad Omron OPC"));
                    }

                    var edgeStuff = new List<string> {"ADR Pilot", "IgnitionACE"};
                    if (edgeStuff.Contains(server.Name))
                    {
                        var tempNode = references.FirstOrDefault(x => x.DisplayName.Text == "Configured Tags");
                        session.Browse(null, null, ExpandedNodeId.ToNodeId(tempNode.NodeId, session.NamespaceUris), 0u,
                            BrowseDirection.Forward, ReferenceTypeIds.HierarchicalReferences, true,
                            (uint) NodeClass.Variable | (uint) NodeClass.Object | (uint) NodeClass.Method, out _,
                            out references);
                        ourRootStart.Add(references.FirstOrDefault(x => x.DisplayName.Text == "s1500"));
                    }
                    var historianStuff = new List<string> {"RigHistorian"};
                    if (historianStuff.Contains(server.Name))
                    {
                        var tempNode = references.FirstOrDefault(x => x.DisplayName.Text == "Devices");
                        session.Browse(null, null, ExpandedNodeId.ToNodeId(tempNode.NodeId, session.NamespaceUris), 0u,
                            BrowseDirection.Forward, ReferenceTypeIds.HierarchicalReferences, true,
                            (uint)NodeClass.Variable | (uint)NodeClass.Object | (uint)NodeClass.Method, out _,
                            out references);
                        try
                        {
                            tempNode = references.FirstOrDefault(x => x.DisplayName.Text == "[Tunnel]");
                            session.Browse(null, null, ExpandedNodeId.ToNodeId(tempNode.NodeId, session.NamespaceUris), 0u,
                                BrowseDirection.Forward, ReferenceTypeIds.HierarchicalReferences, true,
                                (uint)NodeClass.Variable | (uint)NodeClass.Object | (uint)NodeClass.Method, out _,
                                out references);
                        }
                        catch (Exception) // Apparently case matters and need a better fix here. I think can probably just tolower the displaynametext 
                        {
                            tempNode = references.FirstOrDefault(x => x.DisplayName.Text == "[tunnel]");
                            session.Browse(null, null, ExpandedNodeId.ToNodeId(tempNode.NodeId, session.NamespaceUris), 0u,
                                BrowseDirection.Forward, ReferenceTypeIds.HierarchicalReferences, true,
                                (uint)NodeClass.Variable | (uint)NodeClass.Object | (uint)NodeClass.Method, out _,
                                out references);
                        }
                        tempNode = references.FirstOrDefault(x => x.DisplayName.Text == "CIMPLICITY");
                        session.Browse(null, null, ExpandedNodeId.ToNodeId(tempNode.NodeId, session.NamespaceUris), 0u,
                            BrowseDirection.Forward, ReferenceTypeIds.HierarchicalReferences, true,
                            (uint)NodeClass.Variable | (uint)NodeClass.Object | (uint)NodeClass.Method, out _,
                            out references);
                        tempNode = references.FirstOrDefault(x => x.DisplayName.Text == "ENSIGN_AC_RIG") ??
                                   references.FirstOrDefault(x => x.DisplayName.Text == "ENSIGN_CAL");
                        ourRootStart.Add(tempNode);
                    }
                    GetValue(session, ourRootStart, server);
                }
                Log.Info($"Found {server.OpcTags.Count} OpcTags for {server.Name}");
            }
        }

        private void GetValue(Session session, List<ReferenceDescription> references, OpcUaServerConfig server)
        {
            var count = 0;
            foreach (var reference in references)
            {
                count += 1;
                if (count % 10 == 0) {Console.Write(".");}

                if (reference.NodeClass != NodeClass.Variable)
                {
                    session.Browse(null, null, ExpandedNodeId.ToNodeId(reference.NodeId, session.NamespaceUris), 0u,
                        BrowseDirection.Forward,
                        ReferenceTypeIds.HierarchicalReferences, true,
                        (uint) NodeClass.Variable | (uint) NodeClass.Object | (uint) NodeClass.Method, out _,
                        out var nextRefs);
                    GetValue(session, nextRefs.ToList(), server);
                }
                else
                {
                    var nodesToRead = new ReadValueIdCollection();
                    var readValueId = new ReadValueId {NodeId = (NodeId) reference.NodeId, AttributeId = 14};
                    nodesToRead.Add(readValueId);
                    session.Read(null, 0, TimestampsToReturn.Neither, nodesToRead, out var values, out _);
                    var attrib14 = (NodeId) values[0].Value;
                    var opcTagInfo = new OpcTagInfo(reference, attrib14);
                    var ignoreList = server.GetIgnoreList();
                    if (ignoreList?.FirstOrDefault(x => x == opcTagInfo.NodeId) == null)
                    {
                        server.OpcTags.Add(opcTagInfo);
                    }
                }
            }
        }
        public static bool PingHost(string nameOrAddress)
        {
            var pingable = false;
            Ping pinger = null;

            try
            {
                pinger = new Ping();
                var reply = pinger.Send(nameOrAddress);
                pingable = reply.Status == IPStatus.Success;
            }
            catch (PingException)
            {
                // Discard PingExceptions and return false;
            }
            finally
            {
                pinger?.Dispose();
            }

            return pingable;
        }

    }
}