using System;
using System.ServiceProcess;
using Common;
using Common.Models.Reporting;

namespace Batch.Checks
{
    public class IgorRigInstalled 
    {
        public bool Check(EgnServer egnServer)
        {
            try
            {
                var serviceController = egnServer.ServiceController.GetServiceHandle("Igor");
                var serviceControllerStatus = serviceController.Status;
                if (serviceControllerStatus == ServiceControllerStatus.Stopped)
                {
                    EsiLog.HardError(egnServer, "Service Igor was 'Stopped'", "Internal");
                    //serviceController.Start();
                }

            }
            catch (Exception e)
            {
                if (e.Message.StartsWith("Service Igor was not found on computer"))
                {
                    EsiLog.HardError(egnServer, "Service Igor was not found on computer.", "Internal");
                    IgorService.Install(egnServer);
                    return false;
                }
            }
            return true;
        }
    }
}