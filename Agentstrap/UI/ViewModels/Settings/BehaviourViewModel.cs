namespace Agentstrap.UI.ViewModels.Settings
{
    public class BehaviourViewModel : NotifyPropertyChangedViewModel
    {
        public bool ConfirmLaunches
        {
            get => App.Settings.Prop.ConfirmLaunches;
            set => App.Settings.Prop.ConfirmLaunches = value;
        }

        public bool BackgroundUpdates
        {
            get => App.Settings.Prop.BackgroundUpdatesEnabled;
            set => App.Settings.Prop.BackgroundUpdatesEnabled = value;
        }

        public bool IsRobloxInstallationMissing =>
            !App.IsPlayerInstalled && !App.IsStudioInstalled;

        public bool ForceRobloxReinstallation
        {
            get => App.State.Prop.ForceReinstall || IsRobloxInstallationMissing;
            set => App.State.Prop.ForceReinstall = value;
        }

        public IReadOnlyList<string> ProcessPriorityOptions { get; } = new[]
        {
            "Realtime",
            "High",
            "Above normal",
            "Normal",
            "Below normal"
        };

        public string RobloxProcessPriority
        {
            get => App.Settings.Prop.RobloxProcessPriority;
            set
            {
                if (App.Settings.Prop.RobloxProcessPriority == value)
                    return;

                App.Settings.Prop.RobloxProcessPriority = value;
                App.Settings.Save();
            }
        }
    }
}
