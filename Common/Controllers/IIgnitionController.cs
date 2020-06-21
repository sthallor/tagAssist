using System.Collections.Generic;
using Common.Models.Ignition;

namespace Common.Controllers
{
    public interface IIgnitionController
    {
        List<StoreAndForwardQuarantine> GetStoreAndForwardQuarantines();
        List<Modules> GetModules();
        List<DatabaseConnection> GetDatabaseConnections();
        void InvokeModuleAction(string moduleName, string action);
        void InvokeOpcUaAction(string opcServerName, string action);
        void InvokeQuarantineAction(string dataType, string action);
        List<OpcServer> GetOpcServers();
        string GetLicenseInfo();
        void RestartModules();
        void ReDiscoverEndpoints();
        bool IsLoggedIn();
        string GetHtmlContent(string path);
        string DeleteHtmlContent(string path);
        string PutHtmlContent(string path);
        int GetPruneAge();
        string SetGatewayName();
        string SetDbTranslation();
        string CreateDbConnection();
        string WebSandfEndpoint();
        string CreateSplitter();
        string SetDataPruning();
        void SetModuleQuarantine();
    }
}