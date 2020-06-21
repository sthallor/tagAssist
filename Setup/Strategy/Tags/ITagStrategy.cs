using System.Collections.Generic;
using Common.Models.TagCompare;

namespace Setup.Strategy.Tags
{
    public interface ITagStrategy
    {
        List<string> Id { get; }
        List<IgnitionData> GetOpcTags(OpcUaServer opcServer);
        List<IgnitionData> GetIgnTags(OpcUaServer opcServer);
    }
}