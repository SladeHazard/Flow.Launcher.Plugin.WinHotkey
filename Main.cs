using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Flow.Launcher.Plugin.WinHotkey
{
    public class WinHotkey : IPlugin, ISettingProvider
    {
        private PluginInitContext _context;
        private Settings _settings;
        private NativeHotkeyHook _hotkeyHook;
        private NativeHotkeySender _hotkeySender;

        public void Init(PluginInitContext context)
        {
            _context = context;
            _settings = _context.API.LoadSettingJsonStorage<Settings>();

            if (!_context.CurrentPluginMetadata.Disabled)
            {
                _hotkeySender = new NativeHotkeySender(GetFlowLauncherHotkey());
                _hotkeyHook = new NativeHotkeyHook(
                    _settings,
                    SendFlowLauncherHotkey,
                    _hotkeySender.MaskWindowsStartMenu);
                _hotkeyHook.Start();
            }
        }

        private string GetFlowLauncherHotkey()
        {
            var flowDirectory = Path.GetDirectoryName(
                Path.GetDirectoryName(_context.CurrentPluginMetadata.PluginDirectory));
            var settingsPath = Path.Combine(flowDirectory, "Settings", "Settings.json");

            using var settings = JsonDocument.Parse(File.ReadAllText(settingsPath));
            return settings.RootElement.GetProperty("Hotkey").GetString()
                ?? throw new InvalidDataException("Flow Launcher's hotkey is not configured.");
        }

        private void SendFlowLauncherHotkey()
        {
            // Queue the synthetic shortcut until the physical modifier release has
            // left the hook. This prevents LWin from being added to Flow's shortcut.
            Application.Current.Dispatcher.BeginInvoke(
                new Action(_hotkeySender.Send),
                DispatcherPriority.Normal);
        }

        public List<Result> Query(Query query)
        {
            return new List<Result>();
        }

        public Control CreateSettingPanel()
        {
            return new WinHotkeySettings(_settings);
        }

        public void Dispose()
        {
            _hotkeyHook?.Dispose();
            _hotkeyHook = null;
            _hotkeySender = null;
        }
    }

    public partial class WinHotkeySettings : UserControl
    {
        public WinHotkeySettings(Settings settings)
        {
            DataContext = settings;
            InitializeComponent();
        }
    }

    public class Settings
    {
        public const string LWinSpaceModifier = "LWin + Space";
        public const string LCtrlSpaceModifier = "LControl + Space";

        private const int MinimumDoubleTapTimeout = 200;
        private const int DefaultPressTimeout = 200;
        private const int DefaultDoubleTapTimeout = 500;

        private string _timeout = DefaultPressTimeout.ToString();
        private string _doubleTapTimeout = DefaultDoubleTapTimeout.ToString();

        public string DoubleTapTimeout
        {
            get => _doubleTapTimeout;
            set => _doubleTapTimeout = ParseAtLeast(value, MinimumDoubleTapTimeout, DefaultDoubleTapTimeout).ToString();
        }

        public bool DoubleTap { get; set; }

        public string InterrModifier { get; set; } = "LWin";

        [JsonIgnore]
        public List<string> Modifiers { get; } = new()
        {
            "LWin",
            LWinSpaceModifier,
            "LControl",
            LCtrlSpaceModifier,
            "LAlt"
        };

        public string Timeout
        {
            get => _timeout;
            set => _timeout = ParseAtLeast(value, 1, DefaultPressTimeout).ToString();
        }

        [JsonIgnore]
        public int PressTimeoutMilliseconds => ParseAtLeast(Timeout, 1, DefaultPressTimeout);

        [JsonIgnore]
        public int DoubleTapTimeoutMilliseconds => ParseAtLeast(DoubleTapTimeout, MinimumDoubleTapTimeout, DefaultDoubleTapTimeout);

        private static int ParseAtLeast(string value, int minimum, int fallback)
        {
            return int.TryParse(value, out var parsed) && parsed >= minimum ? parsed : fallback;
        }
    }
}
