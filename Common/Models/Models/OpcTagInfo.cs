using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using log4net;
using Opc.Ua;

namespace Common.Models.Models
{
    public class OpcTagInfo
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        public OpcTagInfo()
        {
        }

        public override string ToString()
        {
            return BrowseName == "VALUE" ? NodeId : BrowseName;
        }

        public OpcTagInfo(ReferenceDescription reference, NodeId attrib14)
        {
            NodeId = reference.NodeId.ToString();
            //TODO: This should happen based on type...
            if (NodeId.Contains("[tunnel]"))
                NodeId = NodeId.Replace("ns=1;s=", "");
            DisplayName = reference.DisplayName.Text;
            BrowseName = reference.BrowseName.Name;
            DataType = GetDataType(attrib14);
        }

        public string NodeId { get; set; }
        public string BrowseName { get; set; }
        public string DisplayName { get; set; }
        public string DataType { get; set; }

        private string GetDataType(NodeId dataType)
        {
            var uDataType = (uint)dataType.Identifier;
            switch (uDataType)
            {
                case 1:
                    return "Boolean";
                case 2:
                    return "Int1";
                case 3:
                    return "Int8";
                case 4:
                    return "Int2";
                case 5:
                    return "Int4";
                case 6:
                    return "Int4";
                case 7:
                    return "Int8";
                case 8:
                    return "Int8";
                case 10:
                    return "Float4";
                case 11:
                    return "Float8";
                case 12:
                    return "String";
                case 13:
                    return "String";
                default:
                    Log.Error($"Failed to map datatype. uDataType: {uDataType}");
                    Log.Error($"NodeId {NodeId}");
                    Log.Error($"DisplayName {DisplayName}");
                    Log.Error($"BrowseName {BrowseName}");
                    throw new NotImplementedException($"{NodeId} DataType {uDataType} has no mapping.");
            }
        }

        public static string GetDataType(string csvExportDataType)
        {
            switch (csvExportDataType)
            {
                case "0":
                    return "Int1";
                case "1":
                    return "Int2";
                case "2":
                    return "Int4";
                case "3":
                    return "Int8";
                case "4":
                    return "Float4";
                case "5":
                    return "Float8";
                case "6":
                    return "Boolean";
                case "7":
                    return "String";
                case "8":
                    return "DateTime";
                case "17":
                    return "Int1Array";
                case "18":
                    return "Int2Array";
                case "11":
                    return "Int4Array";
                case "12":
                    return "Int8Array";
                case "19":
                    return "Float4Array";
                case "13":
                    return "Float8Array";
                case "14":
                    return "BooleanArray";
                case "15":
                    return "StringArray";
                case "9":
                    return "DataSet";
                default:
                    Log.Error($"Failed to map datatype. uDataType: {csvExportDataType}");
                    throw new NotImplementedException($"DataType {csvExportDataType} has no mapping.");
            }
        }

        public static string GetPath(OpcTagInfo opcTagInfo, OpcUaServerConfig opcUaServer, HistoryProvider historyProvider)
        {
            var configuredSources = new List<string> { "ADR Pilot", "IgnitionACE", "RigHistorian", "T-RigHistorian",
                "Cameron Kepware", "IEC Kepware", "IEC769", "IEC774", "IEC776" };
            if (!configuredSources.Contains(opcUaServer.Name))
                return "";
            if (opcUaServer.Name == "ADR Pilot" || opcUaServer.Name == "IgnitionACE")
            {
                if (!opcTagInfo.NodeId.Contains("/"))
                    return FixReturn(historyProvider.RootFolder);
                return FixReturn(historyProvider.RootFolder + "/" + opcTagInfo.NodeId.Substring(
                           opcTagInfo.NodeId.IndexOf("]") + 1,
                           opcTagInfo.NodeId.LastIndexOf("/") - 1 - opcTagInfo.NodeId.IndexOf("]")));
            }

            if (opcUaServer.Name == "RigHistorian")
            {
                var temp = opcTagInfo.NodeId.Substring(opcTagInfo.NodeId.LastIndexOf("\\") + 1).Replace(".VALUE", "");
                if (!temp.Contains("."))
                    return FixReturn(historyProvider.RootFolder);
                temp = temp.Substring(0, temp.LastIndexOf("."));
                return FixReturn(historyProvider.RootFolder + "/" + temp.Replace(".", "/"));
            }

            if (opcUaServer.Name == "T-RigHistorian")
            {
                var temp = opcTagInfo.NodeId.Substring(opcTagInfo.NodeId.IndexOf(".") + 1);
                if (!temp.Contains("."))
                    return FixReturn(historyProvider.RootFolder);
                temp = temp.Substring(0, temp.LastIndexOf("."));
                return FixReturn(historyProvider.RootFolder + "/" + temp.Replace(".", "/"));
            }
            if (opcUaServer.Name == "Cameron Kepware")
            {
                var temp = opcTagInfo.NodeId.Substring(opcTagInfo.NodeId.LastIndexOf("=") + 1);
                if (!temp.Contains("."))
                    return FixReturn(historyProvider.RootFolder);
                temp = temp.Substring(0, temp.LastIndexOf("."));
                return FixReturn(historyProvider.RootFolder + "/" + temp.Replace(".", "/"));
            }
            if (opcUaServer.Name == "IEC Kepware")
            {
                var temp = opcTagInfo.NodeId.Substring(opcTagInfo.NodeId.LastIndexOf("=") + 6);
                if (!temp.Contains("."))
                    return FixReturn(historyProvider.RootFolder);
                temp = temp.Substring(0, temp.LastIndexOf("."));
                return FixReturn(historyProvider.RootFolder + "/" + temp.Replace(".", "/"));
            }
            if (opcUaServer.Name == "IEC769")
            {
                var temp = opcTagInfo.NodeId.Substring(opcTagInfo.NodeId.LastIndexOf("=") + 16);
                if (!temp.Contains("."))
                    return FixReturn(historyProvider.RootFolder);
                temp = temp.Substring(0, temp.LastIndexOf("."));
                return FixReturn(historyProvider.RootFolder + "/" + temp.Replace(".", "/"));
            }
            if (opcUaServer.Name == "IEC774")
            {
                var temp = opcTagInfo.NodeId.Substring(opcTagInfo.NodeId.LastIndexOf("=") + 16);
                if (!temp.Contains("."))
                    return FixReturn(historyProvider.RootFolder);
                temp = temp.Substring(0, temp.LastIndexOf("."));
                return FixReturn(historyProvider.RootFolder + "/" + temp.Replace(".", "/"));
            }
            if (opcUaServer.Name == "IEC776")
            {
                var temp = opcTagInfo.NodeId.Replace("ns=2;s=EDR.ADR.", "adr.")
                    .Replace("ns=2;s=Pace.TD Historian.", "td_historian.");
                temp = temp.Substring(0, temp.LastIndexOf("."));
                return FixReturn(historyProvider.RootFolder + "/" + temp.Replace(".", "/"));
            }
            return "";
        }

        private static string FixReturn(string returnString)
        {
            return returnString.Replace("%", "").Replace("+", "").Replace("#", "").Replace("&", "");
        }

        public static string GetName(OpcTagInfo opcTagInfo, OpcUaServerConfig opcUaServer)
        {
            var configuredSources = new List<string> { "ADR Pilot", "IgnitionACE", "RigHistorian", "T-RigHistorian",
                "Cameron Kepware", "IEC Kepware", "IEC769", "IEC774", "IEC776" };
            if (!configuredSources.Contains(opcUaServer.Name)) return "";
            opcTagInfo.DisplayName = opcTagInfo.DisplayName.Replace("%", "").Replace("+", "").Replace("#", "").Replace("&", "");
            if (opcUaServer.Name == "ADR Pilot" || opcUaServer.Name == "IgnitionACE" || opcUaServer.Name == "T-RigHistorian" ||
                opcUaServer.Name == "Cameron Kepware" || opcUaServer.Name == "IEC Kepware" || 
                opcUaServer.Name == "IEC769" || opcUaServer.Name == "IEC774" || opcUaServer.Name == "IEC776")
            {
                return opcTagInfo.DisplayName;
            }
            if (opcUaServer.Name == "RigHistorian")
            {
                var temp = opcTagInfo.NodeId.Substring(opcTagInfo.NodeId.LastIndexOf("\\") + 1).Replace(".VALUE", "");
                return temp.Contains(".") ? temp.Split('.').LastOrDefault() : temp;
            }
            return "";
        }
    }
}