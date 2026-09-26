using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;

namespace Agentstrap.UI.Elements.Commands
{
    public partial class CommandConsoleWindow : Window
    {
        private static readonly Regex CommandHead = new(@"^/(?<name>[a-z?]+)(?:\s+(?<args>.*))?$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        private static readonly Regex Username = new(@"^[A-Za-z0-9_]{1,20}$", RegexOptions.CultureInvariant);
        private static readonly Regex TeamName = new(@"^[\p{L}\p{N}_ -]{1,32}$", RegexOptions.CultureInvariant);
        private static readonly Regex EmoteName = new(@"^[A-Za-z0-9_]{1,32}$", RegexOptions.CultureInvariant);

        private IntPtr _robloxWindow;
        private int _robloxProcessId;
        private bool _isRunning;

        public CommandConsoleWindow()
        {
            InitializeComponent();
            Loaded += (_, _) => CommandInput.Focus();
        }

        public void FocusCommandInput()
        {
            Activate();
            CommandInput.Focus();
            CommandInput.CaretIndex = CommandInput.Text.Length;
        }

        private void Log(string message)
        {
            string line = $"[{DateTime.Now.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture)}] {message}";
            DebugLogBox.AppendText(line + Environment.NewLine);
            DebugLogBox.ScrollToEnd();
            App.Logger.WriteLine("CommandConsole", message);
        }

        private void SetStatus(string message) => StatusText.Text = message;

        private static bool TryValidateCommand(string input, out string command, out string error)
        {
            command = "";
            error = "";

            if (input.Any(char.IsControl))
            {
                error = "Command contains a line break or unsupported control character.";
                return false;
            }

            string trimmed = input.Trim();
            if (trimmed.Length == 0 || trimmed[0] != '/')
            {
                error = "Command must start with /.";
                return false;
            }

            command = Regex.Replace(trimmed, @"\s+", " ");
            if (command.Length > 200)
            {
                error = "Command is longer than 200 characters.";
                return false;
            }

            Match match = CommandHead.Match(command);
            if (!match.Success)
            {
                error = "Command format is invalid.";
                return false;
            }

            string name = match.Groups["name"].Value.ToLowerInvariant();
            string args = match.Groups["args"].Success ? match.Groups["args"].Value : "";

            switch (name)
            {
                case "clear":
                case "cls":
                case "console":
                case "help":
                case "?":
                case "version":
                    if (args.Length == 0)
                        return true;
                    break;

                case "e":
                case "emote":
                    if (EmoteName.IsMatch(args))
                        return true;
                    break;

                case "mute":
                case "m":
                case "unmute":
                case "um":
                    if (Username.IsMatch(args))
                        return true;
                    break;

                case "team":
                case "t":
                    if (TeamName.IsMatch(args) && args.Trim().Length > 0)
                        return true;
                    break;

                case "whisper":
                case "w":
                    int separator = args.IndexOf(' ');
                    if (separator > 0
                        && Username.IsMatch(args[..separator])
                        && args[(separator + 1)..].Trim().Length > 0
                        && args[(separator + 1)..].All(c => !char.IsControl(c)))
                    {
                        return true;
                    }
                    break;
            }

            error = $"Unsupported command or invalid arguments: {name}.";
            return false;
        }

        private bool TryGetValidatedCommand(out string command)
        {
            string entered = CommandInput.Text;
            Log($"Command entered: {entered}");

            if (!TryValidateCommand(entered, out command, out string error))
            {
                Log($"✗ Command rejected: {error}");
                SetStatus(error);
                return false;
            }

            Log($"✓ Command validated: {command}");
            SetStatus("Command validated.");
            return true;
        }

        private bool TryFindRoblox()
        {
            Log($"Finding {App.RobloxPlayerAppName}.exe...");
            _robloxWindow = IntPtr.Zero;
            _robloxProcessId = 0;

            Process[] processes;
            try
            {
                processes = Process.GetProcessesByName(App.RobloxPlayerAppName);
            }
            catch (Exception ex)
            {
                Log($"✗ Could not enumerate Roblox processes: {ex.Message}");
                SetStatus("Roblox process lookup failed.");
                return false;
            }

            bool foundRunningProcess = false;
            foreach (Process process in processes)
            {
                using (process)
                {
                    try
                    {
                        process.Refresh();
                        if (process.HasExited)
                            continue;

                        foundRunningProcess = true;
                        IntPtr handle = process.MainWindowHandle;
                        if (handle == IntPtr.Zero || !IsWindow(handle))
                            continue;

                        _robloxWindow = handle;
                        _robloxProcessId = process.Id;
                        Log($"✓ Roblox found. PID: {_robloxProcessId}");
                        Log($"✓ Roblox window: 0x{_robloxWindow.ToInt64():X}");
                        SetStatus($"Roblox found (PID {_robloxProcessId}).");
                        return true;
                    }
                    catch (InvalidOperationException)
                    {
                        // Process exited while it was being inspected; check the next candidate.
                    }
                    catch (System.ComponentModel.Win32Exception ex)
                    {
                        Log($"Could not inspect a Roblox process: {ex.Message}");
                    }
                }
            }

            if (foundRunningProcess)
            {
                Log("✗ Roblox window handle was invalid.");
                SetStatus("Roblox is running but has no valid main window.");
            }
            else
            {
                Log("✗ RobloxPlayerBeta was not found.");
                SetStatus("RobloxPlayerBeta was not found.");
            }

            return false;
        }

        private bool TryFocusRoblox()
        {
            if (_robloxWindow == IntPtr.Zero || !IsWindow(_robloxWindow))
            {
                Log("✗ Roblox window handle was invalid.");
                SetStatus("Find a valid Roblox window first.");
                return false;
            }

            Log("Requesting Roblox foreground focus...");
            ShowWindow(_robloxWindow, SwRestore);

            uint targetThread = GetWindowThreadProcessId(_robloxWindow, out uint targetProcess);
            uint currentThread = GetCurrentThreadId();
            bool attached = targetThread != 0 && targetThread != currentThread
                && AttachThreadInput(currentThread, targetThread, true);

            try
            {
                BringWindowToTop(_robloxWindow);
                SetForegroundWindow(_robloxWindow);

                for (int attempt = 0; attempt < 10; attempt++)
                {
                    if (GetForegroundWindow() == _robloxWindow)
                    {
                        Log("✓ Roblox is the foreground window.");
                        SetStatus($"Roblox focused (PID {targetProcess}).");
                        return true;
                    }

                    Thread.Sleep(50);
                    SetForegroundWindow(_robloxWindow);
                }
            }
            finally
            {
                if (attached)
                    AttachThreadInput(currentThread, targetThread, false);
            }

            Log("✗ Unable to focus Roblox; it did not become the foreground window.");
            SetStatus("Unable to focus Roblox.");
            return false;
        }

        private bool TryCopyCommand(string command)
        {
            Log($"Copying validated command to clipboard: {command}");
            try
            {
                Clipboard.SetText(command);
                string actual = Clipboard.GetText();
                if (!String.Equals(actual, command, StringComparison.Ordinal))
                {
                    Log("✗ Clipboard verification failed.");
                    SetStatus("Clipboard verification failed.");
                    return false;
                }

                Log($"✓ Clipboard contains: {actual}");
                SetStatus("Validated command copied.");
                return true;
            }
            catch (Exception ex)
            {
                Log($"✗ Clipboard operation failed: {ex.Message}");
                SetStatus("Clipboard operation failed.");
                return false;
            }
        }

        private bool TrySendSlash() => SendKey("/", BuildSlashEvents);
        private bool TrySendPaste() => SendKey("Ctrl+V", BuildPasteEvents);
        private bool TrySendEnter() => SendKey("Enter", BuildEnterEvents);

        private bool SendKey(string description, Func<List<KeyboardInput>> buildInputs)
        {
            if (!IsWindow(_robloxWindow) || GetForegroundWindow() != _robloxWindow)
            {
                Log("✗ Roblox is not the foreground window before keyboard input.");
                SetStatus("Focus Roblox before sending keyboard input.");
                return false;
            }

            Log($"→ Sending {description}");
            List<KeyboardInput> inputs;
            try
            {
                inputs = buildInputs();
            }
            catch (Exception ex)
            {
                Log($"✗ Keyboard input failed: {ex.Message}");
                SetStatus("Keyboard input failed.");
                return false;
            }

            INPUT[] nativeInputs = inputs.Select(x => x.ToNative()).ToArray();
            uint sent = SendInput((uint)nativeInputs.Length, nativeInputs, Marshal.SizeOf<INPUT>());
            if (sent != nativeInputs.Length)
            {
                int error = Marshal.GetLastWin32Error();
                Log($"✗ Keyboard input failed: SendInput accepted {sent}/{nativeInputs.Length} events (Win32 error {error}).");
                ReleaseKeysThatMayBeHeld(inputs, sent);
                SetStatus("Keyboard input failed.");
                return false;
            }

            if (GetForegroundWindow() != _robloxWindow)
            {
                Log("✗ Windows accepted the key events, but Roblox lost foreground focus immediately afterward.");
                SetStatus("Roblox lost focus during keyboard input.");
                return false;
            }

            Log($"✓ Windows accepted {sent} {description} key events; Roblox remained foreground.");
            SetStatus($"Sent {description}; Roblox remained foreground.");
            return true;
        }

        private void ReleaseKeysThatMayBeHeld(List<KeyboardInput> inputs, uint sentCount)
        {
            var pressedScans = inputs
                .Take((int)Math.Min(sentCount, (uint)inputs.Count))
                .Where(input => !input.KeyUp)
                .Select(input => input.ScanCode)
                .Distinct()
                .Select(scanCode => new KeyboardInput(scanCode, true).ToNative())
                .ToArray();

            if (pressedScans.Length == 0)
                return;

            uint released = SendInput((uint)pressedScans.Length, pressedScans, Marshal.SizeOf<INPUT>());
            if (released != pressedScans.Length)
            {
                int error = Marshal.GetLastWin32Error();
                Log($"✗ Could not release all possibly held keys ({released}/{pressedScans.Length}; Win32 error {error}).");
            }
        }

        private List<KeyboardInput> BuildSlashEvents()
        {
            uint threadId = GetWindowThreadProcessId(_robloxWindow, out _);
            IntPtr layout = GetKeyboardLayout(threadId);
            short mapped = VkKeyScanEx('/', layout);
            if (mapped == -1)
                throw new InvalidOperationException("The active keyboard layout cannot produce '/'.");

            byte virtualKey = (byte)(mapped & 0xFF);
            byte modifiers = (byte)((mapped >> 8) & 0xFF);
            if ((modifiers & 0x06) != 0)
                throw new InvalidOperationException("Producing '/' requires an unsupported Ctrl or Alt modifier on this layout.");

            var events = new List<KeyboardInput>();
            if ((modifiers & 0x01) != 0)
                events.Add(KeyboardInput.FromVirtualKey(VkShift, false, layout));

            events.Add(KeyboardInput.FromVirtualKey(virtualKey, false, layout));
            events.Add(KeyboardInput.FromVirtualKey(virtualKey, true, layout));

            if ((modifiers & 0x01) != 0)
                events.Add(KeyboardInput.FromVirtualKey(VkShift, true, layout));

            return events;
        }

        private List<KeyboardInput> BuildPasteEvents()
        {
            IntPtr layout = GetKeyboardLayout(GetWindowThreadProcessId(_robloxWindow, out _));
            return new List<KeyboardInput>
            {
                KeyboardInput.FromVirtualKey(VkControl, false, layout),
                KeyboardInput.FromVirtualKey(VkV, false, layout),
                KeyboardInput.FromVirtualKey(VkV, true, layout),
                KeyboardInput.FromVirtualKey(VkControl, true, layout)
            };
        }

        private List<KeyboardInput> BuildEnterEvents()
        {
            IntPtr layout = GetKeyboardLayout(GetWindowThreadProcessId(_robloxWindow, out _));
            return new List<KeyboardInput>
            {
                KeyboardInput.FromVirtualKey(VkReturn, false, layout),
                KeyboardInput.FromVirtualKey(VkReturn, true, layout)
            };
        }

        private async Task RunFullTestAsync()
        {
            if (_isRunning)
            {
                Log("A command test is already running.");
                return;
            }

            _isRunning = true;
            SetControlsEnabled(false);
            bool focusedRoblox = false;

            try
            {
                if (!TryGetValidatedCommand(out string command))
                    return;

                if (!TryFindRoblox())
                    return;

                if (!TryFocusRoblox())
                    return;

                focusedRoblox = true;
                if (!TryCopyCommand(command))
                    return;

                if (!TrySendSlash())
                    return;

                await Task.Delay(350);
                if (!EnsureRobloxForeground())
                    return;

                if (!TrySendPaste())
                    return;

                await Task.Delay(350);
                if (!EnsureRobloxForeground())
                    return;

                if (!TrySendEnter())
                    return;

                Log("✓ FULL TEST FINISHED. Roblox chat delivery cannot be observed by Agentstrap.");
                SetStatus("Input sequence finished; Roblox acceptance is not observable.");
            }
            catch (Exception ex)
            {
                Log($"✗ Command test failed: {ex.Message}");
                SetStatus("Command test failed.");
            }
            finally
            {
                if (focusedRoblox)
                {
                    Activate();
                    FocusCommandInput();
                    Log("Returned focus to the command console.");
                }

                SetControlsEnabled(true);
                _isRunning = false;
            }
        }

        private bool EnsureRobloxForeground()
        {
            if (IsWindow(_robloxWindow) && GetForegroundWindow() == _robloxWindow)
                return true;

            Log("Roblox is no longer foreground; requesting focus again.");
            return TryFocusRoblox();
        }

        private void SetControlsEnabled(bool enabled)
        {
            CommandInput.IsEnabled = enabled;
            RunTestButton.IsEnabled = enabled;
            FindRobloxButton.IsEnabled = enabled;
            FocusRobloxButton.IsEnabled = enabled;
            CopyCommandButton.IsEnabled = enabled;
            SendSlashButton.IsEnabled = enabled;
            SendPasteButton.IsEnabled = enabled;
            SendEnterButton.IsEnabled = enabled;
            FullTestButton.IsEnabled = enabled;
            CloseButton.IsEnabled = enabled;
        }

        private void FocusConsoleAfterSingleStep()
        {
            Activate();
            FocusCommandInput();
        }

        private async void RunTestButton_Click(object sender, RoutedEventArgs e) => await RunFullTestAsync();
        private async void FullTestButton_Click(object sender, RoutedEventArgs e) => await RunFullTestAsync();

        private void FindRobloxButton_Click(object sender, RoutedEventArgs e) => TryFindRoblox();

        private void FocusRobloxButton_Click(object sender, RoutedEventArgs e)
        {
            if (TryFindRoblox())
                TryFocusRoblox();
        }

        private void CopyCommandButton_Click(object sender, RoutedEventArgs e)
        {
            if (TryGetValidatedCommand(out string command))
                TryCopyCommand(command);
        }

        private void SendSlashButton_Click(object sender, RoutedEventArgs e)
        {
            if (TryGetValidatedCommand(out _) && TryFindRoblox() && TryFocusRoblox())
            {
                TrySendSlash();
                FocusConsoleAfterSingleStep();
            }
        }

        private void SendPasteButton_Click(object sender, RoutedEventArgs e)
        {
            if (TryGetValidatedCommand(out string command) && TryFindRoblox() && TryFocusRoblox() && TryCopyCommand(command))
            {
                TrySendPaste();
                FocusConsoleAfterSingleStep();
            }
        }

        private void SendEnterButton_Click(object sender, RoutedEventArgs e)
        {
            if (TryGetValidatedCommand(out _) && TryFindRoblox() && TryFocusRoblox())
            {
                TrySendEnter();
                FocusConsoleAfterSingleStep();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

        private void CommandInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                _ = RunFullTestAsync();
            }
        }

        private const int SwRestore = 9;
        private const uint MapVkToScan = 0;
        private const uint KeyEventScancode = 0x0008;
        private const uint KeyEventKeyup = 0x0002;
        private const ushort VkShift = 0x10;
        private const ushort VkControl = 0x11;
        private const ushort VkV = 0x56;
        private const ushort VkReturn = 0x0D;

        private readonly record struct KeyboardInput(ushort ScanCode, bool KeyUp)
        {
            public static KeyboardInput FromVirtualKey(ushort virtualKey, bool keyUp, IntPtr layout)
            {
                uint scanCode = MapVirtualKeyEx(virtualKey, MapVkToScan, layout);
                if (scanCode == 0)
                    throw new InvalidOperationException($"Could not map virtual key 0x{virtualKey:X2} to a scan code.");

                return new KeyboardInput((ushort)scanCode, keyUp);
            }

            public INPUT ToNative() => new()
            {
                Type = InputKeyboard,
                Data = new InputUnion
                {
                    Keyboard = new KEYBDINPUT
                    {
                        VirtualKey = 0,
                        ScanCode = ScanCode,
                        Flags = KeyEventScancode | (KeyUp ? KeyEventKeyup : 0),
                        Time = 0,
                        ExtraInfo = UIntPtr.Zero
                    }
                }
            };
        }

        private const uint InputKeyboard = 1;

        [StructLayout(LayoutKind.Sequential)]
        private struct INPUT
        {
            public uint Type;
            public InputUnion Data;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct InputUnion
        {
            [FieldOffset(0)] public MOUSEINPUT Mouse;
            [FieldOffset(0)] public KEYBDINPUT Keyboard;
            [FieldOffset(0)] public HARDWAREINPUT Hardware;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MOUSEINPUT
        {
            public int Dx;
            public int Dy;
            public uint MouseData;
            public uint Flags;
            public uint Time;
            public UIntPtr ExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct KEYBDINPUT
        {
            public ushort VirtualKey;
            public ushort ScanCode;
            public uint Flags;
            public uint Time;
            public UIntPtr ExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct HARDWAREINPUT
        {
            public uint Message;
            public ushort ParameterLow;
            public ushort ParameterHigh;
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint inputCount, [In] INPUT[] inputs, int inputSize);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ShowWindow(IntPtr hWnd, int command);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool BringWindowToTop(IntPtr hWnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        [DllImport("kernel32.dll")]
        private static extern uint GetCurrentThreadId();

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, [MarshalAs(UnmanagedType.Bool)] bool attach);

        [DllImport("user32.dll")]
        private static extern IntPtr GetKeyboardLayout(uint threadId);

        [DllImport("user32.dll", EntryPoint = "VkKeyScanExW")]
        private static extern short VkKeyScanEx(char character, IntPtr keyboardLayout);

        [DllImport("user32.dll", EntryPoint = "MapVirtualKeyExW")]
        private static extern uint MapVirtualKeyEx(ushort code, uint mapType, IntPtr keyboardLayout);
    }
}
