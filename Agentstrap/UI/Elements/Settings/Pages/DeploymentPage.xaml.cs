using Agentstrap.UI.ViewModels.Settings;

namespace Agentstrap.UI.Elements.Settings.Pages
{
    public partial class DeploymentPage
    {
        public DeploymentPage()
        {
            DataContext = new DeploymentViewModel();
            InitializeComponent();
        }
    }
}
