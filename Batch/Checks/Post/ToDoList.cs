using System;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;
using Batch.FactoryStuff;
using Common;
using Common.Database;
using Common.Models.Reporting;

namespace Batch.Checks.Post
{
    public class ToDoList : IPostCheck
    {
        public bool Check()
        {
            using (var igorDb = new IgorDb())
            {
                var rigList = igorDb.ToDoList.Select(x => x.Rig).Distinct().ToList();
                var egnServers = ReportingDb.GetEgnServers().Where(x=> rigList.Contains(x.RigNumber));
                Parallel.ForEach(egnServers, egnServer =>
                {
                    egnServer.Init();
                    if (egnServer.IgnitionController.IsLoggedIn())
                    {
                        foreach (var toDo in igorDb.ToDoList.Where(x=> x.Rig == egnServer.RigNumber))
                        {
                            EsiLog.HardError(egnServer, $"[ToDoItem:{toDo.Id}] {toDo.Message}", "Internal");
                        }
                    }
                });

                var servers = igorDb.ToDoList.Where(x => string.IsNullOrEmpty(x.Rig) && !x.Server.Contains(":")).Select(x => x.Server).Distinct().ToList();
                Parallel.ForEach(servers, server =>
                {
                    try
                    {
                        var ping = new Ping();
                        var pingReply = ping.Send(server);
                        if (pingReply?.Status == IPStatus.Success)
                        {
                            foreach (var toDo in igorDb.ToDoList.Where(x => x.Server == server))
                            {
                                EsiLog.HardError(new EgnServer {Server = server}, $"[ToDoItem:{toDo.Id}] ({server}) {toDo.Message}", "Internal");
                            }
                        }
                    }
                    catch (Exception) {/*Ignored*/}
                });

                var list = igorDb.ToDoList.Where(x => string.IsNullOrEmpty(x.Rig) && x.Server.Contains(":")).Select(x => x.Server).Distinct().ToList();
                Parallel.ForEach(list, server =>
                {
                    using (var tcpClient = new TcpClient())
                    {
                        try
                        {
                            var hostname = server.Split(':')[0];
                            var port = int.Parse(server.Split(':')[1]);
                            tcpClient.Connect(hostname, port);
                            foreach (var toDo in igorDb.ToDoList.Where(x => x.Server == server))
                            {
                                EsiLog.HardError(new EgnServer { Server = server }, $"[ToDoItem:{toDo.Id}] ({server}) {toDo.Message}", "Internal");
                            }
                        }
                        catch (Exception) {/*Ignored*/}
                    }
                });
            }
            return true;
        }
    }
}