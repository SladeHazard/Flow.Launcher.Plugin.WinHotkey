using System;
using System.Collections.Generic;
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

        public void Init(PluginInitContext context)
        {
            _context = context;
            _settings = _context.API.LoadSettingJsonStorage<Settings>();

            if (!_context.CurrentPluginMetadata.Disabled)
            {
                _hotkeyHook = new NativeHotkeyHook(_settings, ShowFlowLauncher);
                _hotkeyHook.Start();
            }
        }

        private void ShowFlowLauncher()
        {
            // The low-level hook callback must return quickly. Move UI work back to
            // Flow Launcher's dispatcher rather than invoking the API on the hook.
            Application.Current.Dispatcher.BeginInvoke(
                new Action(() => _context.API.ShowMainWindow()),
                DispatcherPriority.ApplicationIdle);
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
