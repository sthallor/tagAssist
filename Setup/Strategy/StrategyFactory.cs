using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Common.Models.TagCompare;
using Setup.Strategy.Folder;
using Setup.Strategy.Tags;

namespace Setup.Strategy
{
    public static class StrategyFactory
    {
        private static List<IFolderStrategy> _folderStrategies = new List<IFolderStrategy>();
        private static List<ITagStrategy> _tagStrategies = new List<ITagStrategy>();

        public static IFolderStrategy GetFolderStrategy(OpcUaServer server)
        {
            if (_folderStrategies.Count == 0)
            {
                _folderStrategies = GetAvailableStrategies(typeof(IFolderStrategy)).Cast<IFolderStrategy>().ToList();
            }
            return _folderStrategies.FirstOrDefault(strategy => strategy.Id.Contains(server.Name));
        }
        public static ITagStrategy GetTagStrategy(OpcUaServer server)
        {
            if (_tagStrategies.Count == 0)
            {
                _tagStrategies = GetAvailableStrategies(typeof(ITagStrategy)).Cast<ITagStrategy>().ToList();
            }
            return _tagStrategies.FirstOrDefault(strategy => strategy.Id.Contains(server.Name));
        }

        private static IEnumerable<object> GetAvailableStrategies(Type type)
        {
            return Assembly.GetExecutingAssembly().GetTypes()
                .Where(x => type.IsAssignableFrom(x) && x.IsClass)
                .Select(Activator.CreateInstance);
        }

    }


    private static void NewMethod()
    {
        var egnServer = new EgnServer { Server = "localhost", RigNumber = "000" };
        var connector = new IgnitionConfigConnector(egnServer);
        var opcUaServers = connector.GetOpcServers();

        foreach (var opcServer in opcUaServers)
        {
            var folderStrategy = StrategyFactory.GetFolderStrategy(opcServer);
            var opcFolders = folderStrategy.GetOpcFolders(opcServer);
            var ignFolders = folderStrategy.GetIgnFolders(opcServer);

            // Inserts
            var insertFolders = opcFolders.Except(ignFolders);
            foreach (var folder in insertFolders)
            {
                Log.Info($"Create Folder: {folder.Path}/{folder.Name}");
                connector.CreateFolder(folder);
            }

            // Deletes
            var deleteFolders = ignFolders.Except(opcFolders);
            foreach (var folder in deleteFolders)
            {
                Log.Info($"Delete Folder: {folder.Path}/{folder.Name}");
                connector.DeleteFolder(folder);
            }

            var tagStrategy = StrategyFactory.GetTagStrategy(opcServer);
            var opcTags = tagStrategy.GetOpcTags(opcServer);
            var ignTags = tagStrategy.GetIgnTags(opcServer);

            // Inserts
            var insertTags = opcTags.Except(ignTags);
            foreach (var tag in insertTags)
            {
                Log.Info($"Create Tag: {tag.Path}/{tag.Name}");
                connector.CreateTag(tag);
            }
        }
    }

}