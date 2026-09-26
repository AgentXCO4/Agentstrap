using System.Runtime.InteropServices;
using System.Windows.Interop;

using Agentstrap.UI.Elements.Commands;

namespace Agentstrap
{
    /// <summary>
    /// Registers the command console hotkey for the lifetime of the Roblox watcher.
    /// </summary>
    public sealed class CommandConsoleManager : IDisposable
    {
        public const bool IsEnabled = true;

        private const int HotkeyId = 0xA571;
        private const int WmHotkey = 0x0312;
        private const uint ModNoRepeat = 0x4000;
        private const uint VkOem3 = 0xC0;
        private static readonly IntPtr HwndMessage = new(-3);

        private HwndSource? _source;
        private CommandConsoleWindow? _window;
        private bool _hotkeyRegistered;
        private bool _disposed;

        public void Start()
        {
            if (_disposed || _source is not null)
                return;

            if (!App.Current.Dispatcher.CheckAccess())
            {
                App.Current.Dispatcher.Invoke(Start);
                return;
            }

            try
            {
                var parameters = new HwndSourceParameters("AgentstrapCommandConsoleHotkey")
                {
                    ParentWindow = HwndMessage,
                    Width = 0,
                    Height = 0,
                    WindowStyle = 0
                };

                _source = new HwndSource(parameters);
                _source.AddHook(WindowProcedure);

                _hotkeyRegistered = RegisterHotKey(_source.Handle, HotkeyId, ModNoRepeat, VkOem3);
                if (_hotkeyRegistered)
                {
                    App.Logger.WriteLine("CommandConsoleManager", "Registered global backtick hotkey");
                }
                else
                {
                    int error = Marshal.GetLastWin32Error();
                    App.Logger.WriteLine("CommandConsoleManager", $"Could not register global backtick hotkey (Win32 error {error})");
                }
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine("CommandConsoleManager", "Could not initialize the command console hotkey window");
                App.Logger.WriteException("CommandConsoleManager", ex);
                Stop();
            }
        }

        private IntPtr WindowProcedure(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (message == WmHotkey && wParam.ToInt32() == HotkeyId)
            {
                handled = true;
                App.Current.Dispatcher.BeginInvoke(ShowOrToggle);
            }

            return IntPtr.Zero;
        }

        private void ShowOrToggle()
        {
            if (_disposed)
                return;

            if (_window is null)
            {
                _window = new CommandConsoleWindow();
                _window.Closed += (_, _) => _window = null;
            }

            if (_window.IsVisible && _window.IsActive)
            {
                _window.Hide();
                return;
            }

            if (!_window.IsVisible)
                _window.Show();

            _window.Activate();
            _window.FocusCommandInput();
        }

        public void Dispose() => Stop();

        private void Stop()
        {
            if (!App.Current.Dispatcher.CheckAccess())
            {
                App.Current.Dispatcher.Invoke(Stop);
                return;
            }

            if (_disposed)
                return;

            _disposed = true;

            bool hotkeyWasRegistered = _hotkeyRegistered;
            if (_source is not null)
            {
                if (_hotkeyRegistered)
                {
                    if (!UnregisterHotKey(_source.Handle, HotkeyId))
                    {
                        int error = Marshal.GetLastWin32Error();
                        App.Logger.WriteLine("CommandConsoleManager", $"Could not unregister global hotkey (Win32 error {error})");
                    }

                    _hotkeyRegistered = false;
                }

                _source.RemoveHook(WindowProcedure);
                _source.Dispose();
                _source = null;
            }

            _window?.Close();
            _window = null;
            App.Logger.WriteLine(
                "CommandConsoleManager",
                hotkeyWasRegistered ? "Unregistered global backtick hotkey" : "Stopped command console manager"
            );
        }

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
    }
}
