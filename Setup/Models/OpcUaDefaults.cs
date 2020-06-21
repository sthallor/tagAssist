﻿using System.Collections.Generic;

namespace Setup.Models
{
    public class OpcUaDefaults
    {
        public string Name { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public string Port { get; set; }
        public string Path { get; set; }
        public List<HistoryProvider> HistoryProviders { get; set; }
    }
}