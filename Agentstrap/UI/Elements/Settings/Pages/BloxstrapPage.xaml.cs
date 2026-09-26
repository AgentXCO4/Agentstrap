using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Agentstrap.UI.ViewModels.Settings;

namespace Agentstrap.UI.Elements.Settings.Pages
{
    /// <summary>
    /// Interaction logic for BloxstrapPage.xaml
    /// </summary>
    public partial class BloxstrapPage
    {
        public BloxstrapPage()
        {
            DataContext = new BloxstrapViewModel();
            InitializeComponent();
        }
    }
}

