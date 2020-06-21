using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using log4net;
using Newtonsoft.Json;
using Opc.Ua;
using Opc.Ua.Client;
using Setup.Models;

namespace Setup.Controllers
{
    public class OpcUaClientNew
    {
        public readonly HistorianConfig HistorianConfig;
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public OpcUaClientNew()
        {
            HistorianConfig = Singleton.Instance.GetHistorianConfig();
        }

        public void Run()
        {
            foreach (var server in HistorianConfig.OpcUaServers)
            {
                server.OpcTags = new List<OpcTagInfo>();
                var userIdentity = new UserIdentity(server.GetUserName(), server.GetPassword());
                var endpointDescription = $@"opc.tcp://{server.Host}:{server.GetPort()}{server.GetPath()}";
                Log.Info(endpointDescription);

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

                using (var session = Session.Create(config, new ConfiguredEndpoint(null, new EndpointDescription(endpointDescription)), true, "", 60000, userIdentity, null))
                {
                    session.Browse(null, null, ObjectIds.ObjectsFolder, 0u, BrowseDirection.Forward, ReferenceTypeIds.HierarchicalReferences,
                        true, (uint) NodeClass.Variable | (uint) NodeClass.Object | (uint) NodeClass.Method, out _, out var references);
                    var ourRootStart = new List<ReferenceDescription>();
                    if (server.Name.ToLower().Contains("adr") || server.Name.ToLower().Contains("ace") || server.Name.ToLower().Contains("edge"))
                    {
                        var tempNode = references.FirstOrDefault(x => x.DisplayName.Text == "Configured Tags");
                        session.Browse(null, null, ExpandedNodeId.ToNodeId(tempNode.NodeId, session.NamespaceUris), 0u,
                            BrowseDirection.Forward, ReferenceTypeIds.HierarchicalReferences, true,
                            (uint) NodeClass.Variable | (uint) NodeClass.Object | (uint) NodeClass.Method, out _,
                            out references);
                        ourRootStart.Add(references.FirstOrDefault(x => x.DisplayName.Text == "s1500"));
                    }

                    if (server.Name.ToLower().Contains("historian"))
                        ourRootStart.Add(references.FirstOrDefault(x => x.DisplayName.Text == "Devices"));
                    GetValue(session, ourRootStart, server);
                }
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
                    server.OpcTags.Add(new OpcTagInfo(reference, attrib14));
                }
            }
        }
    }
}