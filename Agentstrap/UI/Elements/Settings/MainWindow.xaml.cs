using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

using Wpf.Ui.Controls;
using Wpf.Ui.Controls.Interfaces;
using Wpf.Ui.Mvvm.Contracts;

using Agentstrap.UI.Elements.Settings.Pages;
using Agentstrap.UI.ViewModels.Settings;

namespace Agentstrap.UI.Elements.Settings
{
    public partial class MainWindow : INavigationWindow
    {
        private Models.Persistable.WindowState _state =>
            App.State.Prop.SettingsWindow;

        private bool _launchingRoblox;

        public MainWindow(bool showAlreadyRunningWarning)
        {
            var viewModel = new MainWindowViewModel();

            viewModel.RequestSaveNoticeEvent +=
                (_, _) => SettingsSavedSnackbar.Show();

            viewModel.RequestCloseWindowEvent +=
                (_, _) => Close();

            viewModel.RequestLaunchRobloxEvent +=
                (_, _) =>
                {
                    try
                    {
                        _launchingRoblox = true;

                        App.Settings.Save();
                        App.State.Save();
                        App.FastFlags.Save();

                        bool realtime =
                            String.Equals(
                                App.Settings.Prop.RobloxProcessPriority,
                                "Realtime",
                                StringComparison.OrdinalIgnoreCase
                            );

                        var startInfo = new ProcessStartInfo
                        {
                            FileName = Paths.Process,
                            Arguments = "-player",
                            UseShellExecute = true
                        };

                        if (realtime)
                        {
                            startInfo.Verb = "runas";

                            App.Logger.WriteLine(
                                "MainWindow::LaunchRoblox",
                                "Realtime priority selected, requesting administrator privileges"
                            );
                        }

                        Process.Start(startInfo);

                        Close();
                    }
                    catch (Win32Exception ex) when (ex.NativeErrorCode == 1223)
                    {
                        _launchingRoblox = false;

                        App.Logger.WriteLine(
                            "MainWindow::LaunchRoblox",
                            "Administrator elevation was cancelled"
                        );
                    }
                    catch (Exception ex)
                    {
                        _launchingRoblox = false;

                        App.Logger.WriteException(
                            "MainWindow::LaunchRoblox",
                            ex
                        );

                        Frontend.ShowMessageBox(
                            "Roblox could not be launched.\n\n" +
                            ex.Message,
                            MessageBoxImage.Error
                        );
                    }
                };

            DataContext = viewModel;

            InitializeComponent();

            App.Logger.WriteLine(
                "MainWindow",
                "Initializing settings window"
            );

            if (showAlreadyRunningWarning)
                ShowAlreadyRunningSnackbar();

            LoadState();
        }

        public void LoadState()
        {
            if (_state.Left > SystemParameters.VirtualScreenWidth)
                _state.Left = 0;

            if (_state.Top > SystemParameters.VirtualScreenHeight)
                _state.Top = 0;

            if (_state.Width > 0)
                Width = _state.Width;

            if (_state.Height > 0)
                Height = _state.Height;

            if (_state.Left > 0 && _state.Top > 0)
            {
                WindowStartupLocation =
                    WindowStartupLocation.Manual;

                Left = _state.Left;
                Top = _state.Top;
            }
        }

        private async void ShowAlreadyRunningSnackbar()
        {
            await Task.Delay(500);

            AlreadyRunningSnackbar.Show();
        }

        #region INavigationWindow methods

        public Frame GetFrame() =>
            RootFrame;

        public INavigation GetNavigation() =>
            RootNavigation;

        public bool Navigate(Type pageType) =>
            RootNavigation.Navigate(pageType);

        public void SetPageService(IPageService pageService) =>
            RootNavigation.PageService = pageService;

        public void ShowWindow() =>
            Show();

        public void CloseWindow() =>
            Close();

        #endregion INavigationWindow methods

        private void Window_PreviewKeyDown(
            object sender,
            System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.K
                && System.Windows.Input.Keyboard.Modifiers == System.Windows.Input.ModifierKeys.Control)
            {
                SettingsSearchBox.Focus();
                SettingsSearchBox.SelectAll();
                e.Handled = true;
            }
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            RootNavigation.Navigate(typeof(DashboardPage));
        }

        private void NavigationButton_Checked(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded || sender is not RadioButton item || item.Tag is not string route)
                return;

            Type pageType;
            string title;

            switch (route)
            {
                case "dashboard":
                    pageType = typeof(DashboardPage);
                    title = "Overview";
                    break;
                case "integrations":
                    pageType = typeof(IntegrationsPage);
                    title = Strings.Menu_Integrations_Title;
                    break;
                case "deployments":
                    pageType = typeof(DeploymentPage);
                    title = "Deployments";
                    break;
                case "mods":
                    pageType = typeof(ModsPage);
                    title = Strings.Menu_Mods_Title;
                    break;
                case "fastflags":
                    pageType = typeof(FastFlagsPage);
                    title = Strings.Menu_FastFlags_Title;
                    break;
                case "appearance":
                    pageType = typeof(AppearancePage);
                    title = Strings.Menu_Appearance_Title;
                    break;
                case "shortcuts":
                    pageType = typeof(ShortcutsPage);
                    title = Strings.Common_Shortcuts;
                    break;
                case "agentstrap":
                    pageType = typeof(BloxstrapPage);
                    title = "Agentstrap settings";
                    break;
                default:
                    return;
            }

            CurrentSectionTitle.Text = title;
            RootNavigation.Navigate(pageType);
        }

        private void SettingsSearchBox_TextChanged(
            object sender,
            TextChangedEventArgs e)
        {
            if (SidebarNavigationPanel is null)
                return;

            string query = SettingsSearchBox.Text.Trim();

            foreach (RadioButton item in SidebarNavigationPanel.Children.OfType<RadioButton>())
            {
                string label = item.ToolTip?.ToString() ?? string.Empty;
                string route = item.Tag?.ToString() ?? string.Empty;
                string keywords = route switch
                {
                    "dashboard" => "launch status roblox installed priority logs repair",
                    "integrations" => "activity discord rich presence tracking",
                    "deployments" => "deployment channel update roblox priority process realtime high normal below matchmaker server place id",
                    "mods" => "skybox modifications fonts sounds cursors",
                    "fastflags" => "fast flags configuration editor",
                    "appearance" => "theme color language bootstrapper",
                    "shortcuts" => "desktop start menu shortcuts",
                    "agentstrap" => "agentstrap analytics updates export data",
                    _ => string.Empty
                };

                item.Visibility = string.IsNullOrWhiteSpace(query)
                    || label.Contains(query, StringComparison.OrdinalIgnoreCase)
                    || route.Contains(query, StringComparison.OrdinalIgnoreCase)
                    || keywords.Contains(query, StringComparison.OrdinalIgnoreCase)
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
        }

        private void WpfUiWindow_Closing(
            object sender,
            CancelEventArgs e)
        {
            if (
                App.FastFlags.Changed ||
                App.PendingSettingTasks.Any()
            )
            {
                var result = Frontend.ShowMessageBox(
                    Strings.Menu_UnsavedChanges,
                    MessageBoxImage.Warning,
                    MessageBoxButton.YesNo
                );

                if (result != MessageBoxResult.Yes)
                {
                    e.Cancel = true;
                    _launchingRoblox = false;
                    return;
                }
            }

            _state.Width = Width;
            _state.Height = Height;

            _state.Top = Top;
            _state.Left = Left;

            App.State.Save();
        }

        private void WpfUiWindow_Closed(
            object sender,
            EventArgs e)
        {
            if (_launchingRoblox)
                return;

            if (App.LaunchSettings.TestModeFlag.Active)
                LaunchHandler.LaunchRoblox(
                    LaunchMode.Player
                );
            else
                App.SoftTerminate();
        }
    }
}
