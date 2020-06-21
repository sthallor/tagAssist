using System;
using System.Collections.Generic;
using System.Linq;

namespace Common.Models.Models
{
    public class OpcUaServerConfig
    {
        public string Name { get; set; }
        public override string ToString()
        {
            return Name;
        }

        // These variables need to be public due to deserialization
        public string Host { get; set; }
        public string Ip { get; set; }
        public string UserName { get; set; } 
        public string Password { get; set; }
        public string Port { get; set; }
        public string Path { get; set; }
        public string License { get; set; }
        public List<string> IgnoreList { get; set; }
        public List<OpcTagInfo> OpcTags { get; set; }
        public List<HistoryProvider> HistoryProviders { get; set; }

        public List<HistoryProvider> GetHistoryProviders()
        {
            if (HistoryProviders != null) return HistoryProviders;
            var defaults = Singleton.Instance.GetOpcUaDefaults().FirstOrDefault(x => x.Name == Name);
            return defaults?.HistoryProviders;
        }
        public List<string> GetIgnoreList()
        {
            if (IgnoreList != null) return IgnoreList;
            var defaults = Singleton.Instance.GetOpcUaDefaults().FirstOrDefault(x => x.Name == Name);
            return defaults?.IgnoreList;
        }
        public string GetUserName()
        {
            if (UserName != null) return UserName;

            var defaults = Singleton.Instance.GetOpcUaDefaults().FirstOrDefault(x => x.Name == Name);
            if(defaults == null) throw new NotImplementedException();
            return defaults.UserName;
        }
        public string GetPassword()
        {
            if (Password != null) return Password;

            var defaults = Singleton.Instance.GetOpcUaDefaults().FirstOrDefault(x => x.Name == Name);
            if (defaults == null) throw new NotImplementedException();
            return defaults.Password;
        }
        public string GetPort()
        {
            if (Port != null) return Port;

            var defaults = Singleton.Instance.GetOpcUaDefaults().FirstOrDefault(x => x.Name == Name);
            if (defaults == null) throw new NotImplementedException();
            return defaults.Port;
        }
        public string GetPath()
        {
            if (Path != null) return Path;

            var defaults = Singleton.Instance.GetOpcUaDefaults().FirstOrDefault(x => x.Name == Name);
            if (defaults == null) throw new NotImplementedException();
            return defaults.Path;
        }
    }
}