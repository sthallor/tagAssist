using System;
using Opc.Ua;

namespace Setup.Models
{
    public class OpcTagInfo
    {
        public OpcTagInfo()
        {
        }

        public OpcTagInfo(ReferenceDescription reference, NodeId attrib14)
        {
            NodeId = reference.NodeId.ToString();
            DisplayName = reference.DisplayName.Text;
            BrowseName = reference.BrowseName.Name;
            DataType = GetDataType((uint) attrib14.Identifier);
        }

        public string NodeId { get; set; }
        public string BrowseName { get; set; }
        public string DisplayName { get; set; }
        public string DataType { get; set; }

        private string GetDataType(uint dataType)
        {
            switch (dataType)
            {
                case 1:
                    return "Boolean";
                case 2:
                    return "Int1";
                case 4:
                    return "Int2";
                case 6:
                    return "Int4";
                case 8:
                    return "Int8";
                case 10:
                    return "Float4";
                case 11:
                    return "Float8";
                case 12:
                    return "String";
                default:
                    Console.WriteLine(dataType);
                    throw new NotImplementedException();
            }
        }
    }
}