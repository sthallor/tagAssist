using System.Data;

namespace Common.Models.TagCompare
{
    public class OpcUaServer
    {
        private readonly string _description;

        public OpcUaServer(IDataRecord reader)
        {
            Name = (string)reader[0];
            _description = (string)reader[1];
        }

        public OpcUaServer()
        {
        }

        public string Name { get; set; }
        public string Rig { get; set; }

        //Remove the port suffix sometimes included in the description.
        public string Description => _description.Replace(":49320", "").Replace(":4096", "");

        public string Username()
        {
            if (Name.Contains("Cameron"))
                return "Ensign";
            if (Description == "148-peco-dcc-hmi-117")
                return "OPCUAUSER";
            if (Description == "157-peco-dcc-hmi-9")
                return "OPCUAUSER";
            if (Description == "119-peco-9.ensign.int")
                return "OPCUAUSER";
            if (Description == "162-peco-dcc-ipc-91.ensign.int")
                return "OPCUAUSER";
            if (Description == "150-peco-dcc-hmi1-205.ensign.int")
                return "OPCUAUSER";
            return "opcuauser";
        }

        public string Password()
        {
            if (Name.Contains("Cameron Kepware"))
                return "jceGG9WS#QcBZ2s";
            return "password";
        }

        public string Port()
        {
            if (Name.Contains("Cameron"))
                return "49320";
            if(Rig == "T226" || Rig == "T701")
                return "49320";
            return "4096";
        }

        public string Path()
        {
            if (Name.Contains("Cameron"))
                return "";
            if(Rig == "T226" || Rig == "T701")
                return "";
            return "/iaopcua/None";
        }
    }
}