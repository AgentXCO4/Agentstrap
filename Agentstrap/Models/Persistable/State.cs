namespace Agentstrap.Models.Persistable
{
    public class State
    {
        public bool PromptWebView2Install { get; set; } = true;

        public bool ForceReinstall { get; set; } = false;

        public WindowState SettingsWindow { get; set; } = new();

        public string LastRobloxLaunchStatus { get; set; } = "No launch recorded";

        public string LastRobloxLaunchDetails { get; set; } = "Launch Roblox to see the latest result.";

        public DateTime? LastRobloxLaunchTimeUtc { get; set; }
    }
}

