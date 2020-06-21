using System.ComponentModel;
using System.Configuration.Install;

namespace IgorEnterprise.Misc
{
    [RunInstaller(true)]
    public partial class ProjectInstaller : Installer
    {
        public ProjectInstaller()
        {
            InitializeComponent();
        }
    }
}
