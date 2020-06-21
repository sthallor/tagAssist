using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Reflection;

namespace Batch.FactoryStuff
{
    public static class CheckFactory
    {
        private static List<IEgnCheck> _egnChecks = new List<IEgnCheck>();
        private static List<IPreCheck> _preChecks = new List<IPreCheck>();
        private static List<IPostCheck> _postChecks = new List<IPostCheck>();
        public static IEnumerable<IEgnCheck> GetEgnChecks()
        {
            if (_egnChecks.Count == 0)
            {
                _egnChecks = GetAvailableChecks(typeof(IEgnCheck)).Cast<IEgnCheck>().ToList();
            }
            return _egnChecks;
        }
        public static IEnumerable<IPreCheck> GetPreChecks()
        {
            if (_preChecks.Count == 0)
            {
                _preChecks = GetAvailableChecks(typeof(IPreCheck)).Cast<IPreCheck>().ToList();
            }
            return _preChecks;
        }
        public static IEnumerable<IPostCheck> GetPostChecks()
        {
            if (_postChecks.Count == 0)
            {
                _postChecks = GetAvailableChecks(typeof(IPostCheck)).Cast<IPostCheck>().ToList();
            }
            return _postChecks;
        }
        private static IEnumerable<object> GetAvailableChecks(Type type)
        { 
            var ignoreList = ConfigurationManager.AppSettings["IgnoreChecks"].Replace(" ", "").Split(',').Where(x=> !string.IsNullOrEmpty(x));
            var allAvailableChecks = Assembly.GetExecutingAssembly().GetTypes().Where(x => type.IsAssignableFrom(x) && x.IsClass);
            return allAvailableChecks.Where(x => !ignoreList.Contains(x.Name)).ToList().Select(Activator.CreateInstance);
        }
    }
}