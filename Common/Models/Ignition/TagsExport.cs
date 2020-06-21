using FileHelpers;

namespace Common.Models.Ignition
{
    [DelimitedRecord(","), IgnoreFirst(2)]
    public class TagsExport
    {
        [FieldQuoted]
        public string Path;
        [FieldQuoted]
        public string Name;
        [FieldQuoted]
        public string Owner;
        public string TagType;
        public string DataType;
        public string Value;
        public string Enabled;
        [FieldQuoted]
        public string AccessRights;
        [FieldQuoted]
        public string OPCServer;
        [FieldQuoted]
        public string OPCItemPath;
    }
}