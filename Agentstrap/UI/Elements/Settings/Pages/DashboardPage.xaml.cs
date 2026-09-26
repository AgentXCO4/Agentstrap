using Agentstrap.UI.ViewModels.Settings;

namespace Agentstrap.UI.Elements.Settings.Pages
{
    public partial class DashboardPage
    {
        public DashboardPage()
        {
            DataContext = new DashboardViewModel();
            InitializeComponent();
        }
    }
}
