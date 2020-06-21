using System.Collections.Generic;
using Common.Models.TagCompare;

namespace Setup.Strategy.Folder
{
    public interface IFolderStrategy
    {
        List<string> Id { get; }
        List<IgnitionFolder> GetOpcFolders(OpcUaServer opcServer);
        List<IgnitionFolder> GetIgnFolders(OpcUaServer opcServer);
    }
}