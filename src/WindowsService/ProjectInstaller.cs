using System.ComponentModel;
using System.Configuration.Install;

namespace DR.Marvin.WindowsService
{
    /// <inheritdoc />
    [RunInstaller(true)]
    public partial class ProjectInstaller : Installer
    {
        /// <inheritdoc />
        public ProjectInstaller()
        {
            InitializeComponent();
        }
        private void serviceProcessInstaller1_AfterInstall(object sender, InstallEventArgs e)
        {

        }
    }
}
