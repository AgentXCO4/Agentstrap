using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;

namespace Agentstrap.UI.ViewModels.Settings
{
    public class DeploymentViewModel : NotifyPropertyChangedViewModel
    {
        private readonly AsyncRelayCommand _checkDeploymentCommand;
        private readonly AsyncRelayCommand _findAndLaunchServerCommand;
        private string _deploymentChannel = App.Settings.Prop.RobloxDeploymentChannel;
        private string _agentstrapMatchmakerStatus = "Enter an experience ID or link, then find a public server.";
        private string _deploymentStatus = "Blank follows Roblox's existing channel selection.";
        private bool _isBusy;

        public DeploymentViewModel()
        {
            _checkDeploymentCommand = new AsyncRelayCommand(CheckDeploymentAsync, () => !IsBusy);
            _findAndLaunchServerCommand = new AsyncRelayCommand(FindAndLaunchServerAsync, () => !IsBusy);
        }

        public string DeploymentChannel
        {
            get => _deploymentChannel;
            set
            {
                if (_deploymentChannel == value)
                    return;
                _deploymentChannel = value;
                OnPropertyChanged(nameof(DeploymentChannel));
            }
        }

        public string DeploymentStatus
        {
            get => _deploymentStatus;
            private set
            {
                if (_deploymentStatus == value)
                    return;
                _deploymentStatus = value;
                OnPropertyChanged(nameof(DeploymentStatus));
            }
        }

        public ICommand CheckDeploymentCommand => _checkDeploymentCommand;

        public ICommand ApplyDeploymentChannelCommand => new RelayCommand(ApplyDeploymentChannel);

        public ICommand ClearDeploymentChannelCommand => new RelayCommand(ClearDeploymentChannel);

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

        public bool ForceRobloxReinstallation
        {
            get => App.State.Prop.ForceReinstall || IsRobloxInstallationMissing;
            set => App.State.Prop.ForceReinstall = value;
        }

        public bool IsRobloxInstallationMissing => !App.IsPlayerInstalled && !App.IsStudioInstalled;

        public IReadOnlyList<string> ProcessPriorityOptions { get; } = new[]
        {
            "Realtime", "High", "Above normal", "Normal", "Below normal"
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
                OnPropertyChanged(nameof(RobloxProcessPriority));
            }
        }

        public ICommand OpenRobloxLogsCommand =>
            new RelayCommand(DashboardViewModel.OpenRobloxLogs);

        public ICommand RepairRobloxLinkCommand =>
            new RelayCommand(DashboardViewModel.RepairRobloxLink);

        public bool AgentstrapMatchmakerEnabled
        {
            get => App.Settings.Prop.AgentstrapMatchmakerEnabled;
            set => App.Settings.Prop.AgentstrapMatchmakerEnabled = value;
        }

        public string AgentstrapMatchmakerPlaceId
        {
            get => App.Settings.Prop.AgentstrapMatchmakerPlaceId;
            set => App.Settings.Prop.AgentstrapMatchmakerPlaceId = value;
        }

        public bool AgentstrapMatchmakerPreferLowPopulation
        {
            get => App.Settings.Prop.AgentstrapMatchmakerPreferLowPopulation;
            set => App.Settings.Prop.AgentstrapMatchmakerPreferLowPopulation = value;
        }

        public IReadOnlyList<int> MinimumFreeSlotOptions { get; } = new[] { 1, 2, 3, 5, 10 };

        public int AgentstrapMatchmakerMinimumFreeSlots
        {
            get => MinimumFreeSlotOptions.Contains(App.Settings.Prop.AgentstrapMatchmakerMinimumFreeSlots)
                ? App.Settings.Prop.AgentstrapMatchmakerMinimumFreeSlots
                : 1;
            set => App.Settings.Prop.AgentstrapMatchmakerMinimumFreeSlots = Math.Clamp(value, 1, 50);
        }

        public string AgentstrapMatchmakerStatus
        {
            get => _agentstrapMatchmakerStatus;
            private set
            {
                if (_agentstrapMatchmakerStatus == value)
                    return;
                _agentstrapMatchmakerStatus = value;
                OnPropertyChanged(nameof(AgentstrapMatchmakerStatus));
            }
        }

        public ICommand FindAndLaunchServerCommand => _findAndLaunchServerCommand;

        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                if (_isBusy != value)
                {
                    _isBusy = value;
                    OnPropertyChanged(nameof(IsBusy));
                    _checkDeploymentCommand.NotifyCanExecuteChanged();
                    _findAndLaunchServerCommand.NotifyCanExecuteChanged();
                }
            }
        }

        private void ApplyDeploymentChannel()
        {
            string channel = DeploymentChannel.Trim();
            if (!IsValidChannel(channel))
            {
                DeploymentStatus = "Channel names can contain only letters, digits, hyphens, and underscores (up to 64 characters).";
                return;
            }

            App.Settings.Prop.RobloxDeploymentChannel = channel;
            App.Settings.Save();
            DeploymentChannel = channel;
            DeploymentStatus = String.IsNullOrEmpty(channel)
                ? "Channel override cleared. Roblox's existing channel selection will be used."
                : $"Channel override saved: {channel}. Explicit launch links still take precedence.";
        }

        private void ClearDeploymentChannel()
        {
            DeploymentChannel = String.Empty;
            ApplyDeploymentChannel();
        }

        private async Task CheckDeploymentAsync()
        {
            string channel = DeploymentChannel.Trim();
            if (!IsValidChannel(channel))
            {
                DeploymentStatus = "Enter a valid channel name before checking it.";
                return;
            }

            if (String.IsNullOrEmpty(channel))
                channel = RobloxInterfaces.Deployment.DefaultChannel;

            IsBusy = true;
            DeploymentStatus = $"Checking the Roblox {channel} deployment…";
            try
            {
                ClientVersion info = await RobloxInterfaces.Deployment.GetInfo(channel);
                DeploymentStatus = $"Channel is available. Roblox build: {info.Version} ({info.VersionGuid}).";
            }
            catch (Exception ex)
            {
                App.Logger.WriteException("DeploymentViewModel::CheckDeploymentAsync", ex);
                DeploymentStatus = $"Could not resolve channel '{channel}': {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task FindAndLaunchServerAsync()
        {
            if (!AgentstrapMatchmakerEnabled)
            {
                AgentstrapMatchmakerStatus = "Enable Agentstrap Matchmaker before searching for servers.";
                return;
            }

            if (!TryGetPlaceId(AgentstrapMatchmakerPlaceId, out long placeId))
            {
                AgentstrapMatchmakerStatus = "Enter a valid Roblox place ID or a roblox.com/games/{id} link.";
                return;
            }

            App.Settings.Save();
            IsBusy = true;
            AgentstrapMatchmakerStatus = "Loading public server options from Roblox…";

            try
            {
                var servers = await FetchPublicServersAsync(placeId);
                int minimumSlots = Math.Clamp(AgentstrapMatchmakerMinimumFreeSlots, 1, 50);
                var available = servers
                    .Where(server => !String.IsNullOrWhiteSpace(server.Id)
                        && server.MaxPlayers > 0
                        && server.MaxPlayers - server.Playing >= minimumSlots)
                    .ToList();

                if (available.Count == 0)
                {
                    AgentstrapMatchmakerStatus = $"No public server found with at least {minimumSlots} open slot(s). Try a lower minimum or search again later.";
                    return;
                }

                PublicServer selected = AgentstrapMatchmakerPreferLowPopulation
                    ? available.OrderBy(server => server.Playing).ThenByDescending(server => server.MaxPlayers).First()
                    : available.OrderByDescending(server => server.Playing).ThenBy(server => server.MaxPlayers - server.Playing).First();

                string launchUri = $"roblox://experiences/start?placeId={placeId}&gameInstanceId={Uri.EscapeDataString(selected.Id)}";
                var startInfo = new ProcessStartInfo
                {
                    FileName = Paths.Process,
                    Arguments = $"-player \"{launchUri}\"",
                    UseShellExecute = true
                };

                Process.Start(startInfo);
                AgentstrapMatchmakerStatus = $"Launch requested for a server with {selected.Playing}/{selected.MaxPlayers} players. Roblox's join result is not reported here.";
            }
            catch (Exception ex)
            {
                App.Logger.WriteException("DeploymentViewModel::FindAndLaunchServerAsync", ex);
                AgentstrapMatchmakerStatus = $"Server search failed: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private static async Task<List<PublicServer>> FetchPublicServersAsync(long placeId)
        {
            var results = new List<PublicServer>();
            string? cursor = null;

            // Keep requests bounded so one search cannot churn through the full server list.
            for (int pageIndex = 0; pageIndex < 3; pageIndex++)
            {
                string url = $"https://games.roblox.com/v1/games/{placeId}/servers/Public?sortOrder=Asc&limit=100";
                if (!String.IsNullOrEmpty(cursor))
                    url += $"&cursor={Uri.EscapeDataString(cursor)}";

                using HttpResponseMessage response = await App.HttpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();
                string json = await response.Content.ReadAsStringAsync();
                PublicServerPage? page = JsonSerializer.Deserialize<PublicServerPage>(json);
                if (page?.Data is null)
                    throw new InvalidDataException("Roblox returned an invalid public server list.");

                results.AddRange(page.Data);
                cursor = page.NextPageCursor;
                if (String.IsNullOrEmpty(cursor))
                    break;
            }

            return results;
        }

        private static bool TryGetPlaceId(string input, out long placeId)
        {
            string value = input.Trim();
            if (Int64.TryParse(value, out placeId) && placeId > 0)
                return true;

            Match match = Regex.Match(value, @"roblox\.com/(?:[a-z]{2}/)?games/(?<id>[0-9]+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            return match.Success && Int64.TryParse(match.Groups["id"].Value, out placeId) && placeId > 0;
        }

        private static bool IsValidChannel(string channel) =>
            String.IsNullOrEmpty(channel)
            || Regex.IsMatch(channel, "^[a-zA-Z0-9_-]{1,64}$", RegexOptions.CultureInvariant);

        private sealed class PublicServerPage
        {
            public PublicServerPage() { }

            [JsonPropertyName("data")]
            public List<PublicServer> Data { get; set; } = new();

            [JsonPropertyName("nextPageCursor")]
            public string? NextPageCursor { get; set; }
        }

        private sealed class PublicServer
        {
            public PublicServer() { }

            [JsonPropertyName("id")]
            public string Id { get; set; } = String.Empty;

            [JsonPropertyName("playing")]
            public int Playing { get; set; }

            [JsonPropertyName("maxPlayers")]
            public int MaxPlayers { get; set; }
        }
    }
}
