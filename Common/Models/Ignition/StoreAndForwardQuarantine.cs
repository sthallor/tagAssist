using System.Collections.Generic;

namespace Common.Models.Ignition
{
    public class StoreAndForwardQuarantine
    {
        public string DataType { get; set; }
        public string Problem { get; set; }
        public int TxnCount { get; set; }
        public List<string> Actions { get; set; }
    }
}