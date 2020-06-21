using System.ComponentModel;
using System.Configuration.Install;

namespace IgorRigInstaller
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
