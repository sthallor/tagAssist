using System;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Threading;
using Common;
using Common.Database;
using Common.Models.Igor;
using Common.Models.Reporting;

namespace Batch.Checks
{
    public class IgorService  
    {
        public bool Check(EgnServer server)
        {
            try
            {
                if (!server.IgnitionController.IsLoggedIn())
                    return false;
                var installedVersion = GetIgorVersion(server);
                var directory = Directory.GetDirectories(@"\\CAL0-VP-TFS01\Drops\ACE.IgorRigService") //All builds
                    .OrderByDescending(x => x.ToString()).First(); // Latest directory
                var latestVersion = decimal.Parse(directory.Split('\\').Last());
                if (installedVersion == latestVersion)
                    return true;

                var serviceController = server.ServiceController.GetServiceHandle("Igor");
                if (serviceController.Status != ServiceControllerStatus.Stopped)
                {
                    serviceController.Stop();
                    serviceController.WaitForStatus(ServiceControllerStatus.Stopped);
                    Thread.Sleep(TimeSpan.FromMinutes(2));
                }
                EsiLog.Info(server, $"Rig running Igor version: {installedVersion}. Latest version is {latestVersion}.");
                Install(server, directory);
                serviceController.Start();
                SetVersion(server, latestVersion);
            }
            catch (Exception e)
            {
                if(e.Message.StartsWith("Cannot open Service Control Manager")) return false;
                EsiLog.Info(server, "Failed to do IgorService check");
                Console.WriteLine(e);
                return false;
            }
            return true;
        }

        public static void Install(EgnServer server)
        {
            var directory = Directory.GetDirectories(@"\\CAL0-VP-TFS01\Drops\ACE.IgorRigService") //All builds
                .OrderByDescending(x => x.ToString()).First(); // Latest directory
            try
            {
                Install(server, directory);
            }
            catch (Exception) { /* ignored */ }
        }

        private static void Install(EgnServer server, string directory)
        {
            var targetPath = $@"\\{server.Server}\c$\Program Files\Igor";
            var directoryInfo = new DirectoryInfo(targetPath);
            if (!directoryInfo.Exists)
                Directory.CreateDirectory(targetPath);
            var files = Directory.GetFiles(directory);
            Directory.CreateDirectory(targetPath + @"\x64");
            Directory.CreateDirectory(targetPath + @"\x86");
            File.Copy(directory + @"\x64\SQLite.Interop.dll", $@"\\{server.Server}\c$\Program Files\Igor\x64\SQLite.Interop.dll", true);
            File.Copy(directory + @"\x86\SQLite.Interop.dll", $@"\\{server.Server}\c$\Program Files\Igor\x86\SQLite.Interop.dll", true);

            foreach (var file in files)
            {
                if(file.Contains(".exe.config"))
                    continue;
                var fileName = Path.GetFileName(file);
                var destFile = Path.Combine(targetPath, fileName);
                File.Copy(file, destFile, true);
            }
            File.WriteAllText($@"{targetPath}\IgorRig.exe.config", $@"<?xml version=""1.0"" encoding=""utf-8"" ?>
<configuration>
   <configSections>
    <!-- For more information on Entity Framework configuration, visit http://go.microsoft.com/fwlink/?LinkID=237468 -->
    <section name=""entityFramework"" type=""System.Data.Entity.Internal.ConfigFile.EntityFrameworkSection, EntityFramework, Version=6.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"" requirePermission=""false"" />
  </configSections>
<appSettings>
<!-- Freespace threshhold for Warning/Error. Amount in MB-->
<add key=""FreeSpace"" value=""3000,2000"" />
<add key=""Server"" value=""{server.Server}"" />
<add key=""Rig"" value=""{server.RigNumber}"" />
</appSettings>
   <startup>
    <supportedRuntime version=""v4.0"" sku="".NETFramework,Version=v4.6.1"" />
  </startup>
  <runtime>
    <assemblyBinding xmlns=""urn:schemas-microsoft-com:asm.v1"">
      <dependentAssembly>
        <assemblyIdentity name=""Newtonsoft.Json"" publicKeyToken=""30ad4fe6b2a6aeed"" culture=""neutral"" />
        <bindingRedirect oldVersion=""0.0.0.0-12.0.0.0"" newVersion=""12.0.0.0"" />
      </dependentAssembly>
      <dependentAssembly>
        <assemblyIdentity name=""System.Data.SQLite"" publicKeyToken=""db937bc2d44ff139"" culture=""neutral""/>
        <bindingRedirect oldVersion=""0.0.0.0-1.0.111.0"" newVersion=""1.0.111.0""/>
      </dependentAssembly>
    </assemblyBinding>
  </runtime>
  <entityFramework>
    <defaultConnectionFactory type=""System.Data.Entity.Infrastructure.LocalDbConnectionFactory, EntityFramework"">
      <parameters>
        <parameter value=""mssqllocaldb"" />
      </parameters>
    </defaultConnectionFactory>
    <providers>
      <provider invariantName=""System.Data.SqlClient"" type=""System.Data.Entity.SqlServer.SqlProviderServices, EntityFramework.SqlServer"" />
      <provider invariantName=""System.Data.SQLite.EF6"" type=""System.Data.SQLite.EF6.SQLiteProviderServices, System.Data.SQLite.EF6"" />
    </providers>
  </entityFramework>
  <system.data>
    <DbProviderFactories>
      <remove invariant=""System.Data.SQLite.EF6"" />
      <add name=""SQLite Data Provider (Entity Framework 6)"" invariant=""System.Data.SQLite.EF6"" description="".NET Framework Data Provider for SQLite (Entity Framework 6)"" type=""System.Data.SQLite.EF6.SQLiteProviderFactory, System.Data.SQLite.EF6"" />
    <remove invariant=""System.Data.SQLite"" /><add name=""SQLite Data Provider"" invariant=""System.Data.SQLite"" description="".NET Framework Data Provider for SQLite"" type=""System.Data.SQLite.SQLiteFactory, System.Data.SQLite"" /></DbProviderFactories>
  </system.data>
</configuration>");
        }

        private static void SetVersion(EgnServer server, decimal last)
        {
            using (var db = new IgorDb())
            {
                var rigIgorServiceVersion = db.IgorVersion.Find(server.RigNumber);
                rigIgorServiceVersion.Version = last;
                db.SaveChanges();
            }
        }

        private static decimal GetIgorVersion(EgnServer server)
        {
            RigIgorServiceVersion rigIgorServiceVersion;
            using (var db = new IgorDb())
            {
                rigIgorServiceVersion = db.IgorVersion.Find(server.RigNumber);
                if (rigIgorServiceVersion == null)
                {
                    rigIgorServiceVersion = new RigIgorServiceVersion
                        {Rig = server.RigNumber, Server = server.Server, Version = 0};
                    db.IgorVersion.Add(rigIgorServiceVersion);
                }

                db.SaveChanges();
            }
            return rigIgorServiceVersion.Version;
        }
    }
}