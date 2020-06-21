using System;
using System.ServiceProcess;
using Common.Models.Reporting;

namespace Common.Controllers
{
    public class ServiceController
    {
        private readonly EgnServer _egnServer;
        private System.ServiceProcess.ServiceController _serviceController;

        public ServiceController(EgnServer egnServer)
        {
            _egnServer = egnServer;
        }

        public void Start(string service)
        {
            var serviceController = GetServiceHandle(service);
            if (serviceController.Status != ServiceControllerStatus.Running)
            {
                serviceController.Start();
                serviceController.WaitForStatus(ServiceControllerStatus.Running);
            }
        }

        public void Stop(string service)
        {
            var serviceController = GetServiceHandle(service);
            if (serviceController.Status != ServiceControllerStatus.Stopped)
            {
                serviceController.Stop();
                serviceController.WaitForStatus(ServiceControllerStatus.Stopped);
            }
        }

        public System.ServiceProcess.ServiceController GetServiceHandle(string serviceName)
        {
            if (_serviceController == null)
                try
                {
                    if (_egnServer.Server == null)
                    {
                        _serviceController = new System.ServiceProcess.ServiceController(serviceName);
                    }
                    else
                    {
                        _serviceController = new System.ServiceProcess.ServiceController(serviceName, _egnServer.Server);
                    }
                }
                catch (Exception)
                {
                    EsiLog.HardError(_egnServer, $"Unable to get handle for service {serviceName}.  Does the user running this process have local admin?", "Internal");
                    Environment.Exit(-1);
                }
            return _serviceController;
        }
    }
}