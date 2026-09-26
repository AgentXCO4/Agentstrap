using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using Agentstrap.AppData;
using CommunityToolkit.Mvvm.Input;

namespace Agentstrap.UI.ViewModels.Settings
{
    public class DashboardViewModel : NotifyPropertyChangedViewModel
    {
        private static RobloxPlayerData PlayerData => new();

        public string RobloxInstallStatus =>
            File.Exists(PlayerData.ExecutablePath) ? "Installed" : "Not detected";

        public string RobloxInstallDetails =>
            File.Exists(PlayerData.ExecutablePath)
                ? "Roblox Player files are ready for Agentstrap."
                : "Roblox Player will be installed when you launch it.";

        public string ProcessPriority =>
            App.Settings.Prop.RobloxProcessPriority;

        public string LastLaunchStatus =>
            App.State.Prop.LastRobloxLaunchStatus;

        public string LastLaunchDetails =>
            App.State.Prop.LastRobloxLaunchDetails;

        public string LastLaunchTime =>
            App.State.Prop.LastRobloxLaunchTimeUtc is DateTime timestamp
                ? timestamp.ToLocalTime().ToString("g")
                : "No previous launch";

        public ICommand RefreshInstallStatusCommand =>
            new RelayCommand(RefreshInstallStatus);

        public ICommand OpenAgentstrapLogsCommand =>
            new RelayCommand(OpenAgentstrapLogs);

        public ICommand OpenRobloxLogsCommand =>
            new RelayCommand(OpenRobloxLogs);

        public ICommand RepairRobloxLinkCommand =>
            new RelayCommand(RepairRobloxLink);

        private void RefreshInstallStatus()
        {
            OnPropertyChanged(nameof(RobloxInstallStatus));
            OnPropertyChanged(nameof(RobloxInstallDetails));
        }

        private static void OpenAgentstrapLogs()
        {
            OpenFolder(Paths.Logs);
        }

        public static void OpenRobloxLogs()
        {
            string path = Path.Combine(Paths.LocalAppData, "Roblox", "logs");

            if (!Directory.Exists(path))
            {
                Frontend.ShowMessageBox(
                    "Roblox has not created a log folder yet.",
                    MessageBoxImage.Information
                );

                return;
            }

            OpenFolder(path);
        }

        private static void OpenFolder(string path)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = path,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                Frontend.ShowMessageBox(
                    $"Could not open the folder.\n\n{ex.Message}",
                    MessageBoxImage.Error
                );
            }
        }

        public static void RepairRobloxLink()
        {
            WindowsRegistry.RegisterPlayer(
                Paths.Process,
                "-player \"%1\""
            );

            App.Logger.WriteLine(
                "Dashboard",
                $"Registered Roblox links to {Paths.Process}"
            );

            Frontend.ShowMessageBox(
                "Roblox links will now open with Agentstrap.",
                MessageBoxImage.Information
            );
        }
    }
}
