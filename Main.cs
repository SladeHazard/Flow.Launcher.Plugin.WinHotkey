
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Controls;
using AutoHotkey.Interop;
namespace Flow.Launcher.Plugin.WinHotkey
{
    public class WinHotkey : IPlugin, ISettingProvider
    {
        private PluginInitContext _context;
        private static AutoHotkeyEngine _ahk;
        private Settings _settings;

        public void Init(PluginInitContext context)
        {
            _context = context;
            _settings = _context.API.LoadSettingJsonStorage<Settings>();
            _ahk = new AutoHotkeyEngine();
            Hook();
        }

        string MainSettingsPath()
        {
            string SettingsJsonPath = Path.GetDirectoryName(Path.GetDirectoryName(_context.CurrentPluginMetadata.PluginDirectory));
            return Path.Combine(SettingsJsonPath, "Settings", "Settings.json");
        }

        Dictionary<string, JsonElement> LoadSettingsJson()
        {
            string json_data = System.IO.File.ReadAllText(MainSettingsPath());
            return JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json_data);
        }

        string GetCurrentHotkey()
        {
            return LoadSettingsJson()["Hotkey"].GetString();
        }

        string GetHotkeyInAhkFormat()
        {
            // Split the shortcut string into individual key parts
            string[] keys = GetCurrentHotkey().Split('+');

            // Convert each key to its AHK format
            for (int i = 0; i < keys.Length; i++)
            {
                keys[i] = keys[i].Trim(); // Remove leading and trailing spaces
                if (keys[i].Length == 1)
                {
                    keys[i] = keys[i].ToLower();
                }
                else if (keys[i].Length == 2 && keys[i].StartsWith("D"))
                {
                    keys[i] = "{" + keys[i].Substring(1) + "}";
                }
                else if (keys[i].StartsWith("Page"))
                {
                    keys[i] = "{" + keys[i].Replace("Page", "Pg") + "}";
                }
                else if (keys[i] == "Next")
                {
                    keys[i] = "{" + "PgDn" + "}";
                }
                else
                {
                    switch (keys[i].ToLower())
                    {
                        case "alt":
                            keys[i] = "!";
                            break;
                        case "ctrl":
                            keys[i] = "^";
                            break;
                        case "shift":
                            keys[i] = "+";
                            break;
                        case "win":
                            keys[i] = "#";
                            break;
                        case "back":
                            keys[i] = "{Backspace}";
                            break;
                        case "oemquestion":
                            keys[i] = "/";
                            break;
                        case "oemplus":
                            keys[i] = "=";
                            break;
                        case "oemminus":
                            keys[i] = "-";
                            break;
                        case "oem5":
                            keys[i] = "\\";
                            break;
                        case "oem6":
                            keys[i] = "]";
                            break;
                        case "oemopenbrackets":
                            keys[i] = "[";
                            break;
                        case "oemperiod":
                            keys[i] = ".";
                            break;
                        case "oemcomma":
                            keys[i] = ",";
                            break;
                        case "oem1":
                            keys[i] = ";";
                            break;
                        case "oemquotes":
                            keys[i] = "'";
                            break;
                        case "divide":
                            keys[i] = "{NumpadDiv}";
                            break;
                        case "multiply":
                            keys[i] = "{NumpadMult}";
                            break;
                        case "subtract":
                            keys[i] = "{NumpadSub}";
                            break;
                        case "add":
                            keys[i] = "{NumpadAdd}";
                            break;
                        default:
                            keys[i] = "{" + keys[i] + "}";
                            break;
                    }
                }
            }

            // Combine the keys back into the AHK format
            string ahkFormat = string.Join("", keys);

            return ahkFormat;
        }

        public List<Result> Query(Query query)
        {
            return new List<Result>();
        }

        public string ReleaseMappedButton()
        {
            string ahkFormat = string.Empty;
            switch (_settings.InterrModifier)
            {
                case "LAlt":
                    ahkFormat = "!";
                    break;
                case "LWin":
                case Settings.LWinSpaceModifier:
                    ahkFormat = "#";
                    break;
                case "LControl":
                    ahkFormat = "^";
                    break;
            }
            return ahkFormat;

        }
        public void Hook()
        {
            if (!_context.CurrentPluginMetadata.Disabled)
            {
                string timeout = _settings.Timeout;
                bool useWinSpace = string.Equals(_settings.InterrModifier, Settings.LWinSpaceModifier, StringComparison.Ordinal);
                string hotkeyBinding = useWinSpace ? "~*Space" : $"~{_settings.InterrModifier}";
                string keyWaitTarget = useWinSpace ? "Space" : _settings.InterrModifier;
                string priorKeyName = useWinSpace ? "Space" : _settings.InterrModifier;

                var scriptBuilder = new StringBuilder();
                scriptBuilder.AppendLine("#Persistent");
                scriptBuilder.AppendLine("return");

                if (_settings.DoubleTap)
                {
                    scriptBuilder.AppendLine("Interr_PriorKey := \"\"");
                    scriptBuilder.AppendLine("First_Tap_Time := 0");
                }

                scriptBuilder.AppendLine($"{hotkeyBinding}::");

                if (useWinSpace)
                {
                    scriptBuilder.AppendLine("    if (!GetKeyState(\"LWin\", \"P\"))");
                    scriptBuilder.AppendLine("    {");
                    scriptBuilder.AppendLine("        return");
                    scriptBuilder.AppendLine("    }");
                }

                scriptBuilder.AppendLine("    Send, {Blind}{VKFF}");
                scriptBuilder.AppendLine("    KeyboardStartTime := A_TickCount ; Record the start time");
                scriptBuilder.AppendLine($"    KeyWait, {keyWaitTarget}");
                scriptBuilder.AppendLine();
                scriptBuilder.AppendLine("    ; Calculate the time elapsed");
                scriptBuilder.AppendLine("    ElapsedTime := A_TickCount - KeyboardStartTime");
                scriptBuilder.AppendLine();
                scriptBuilder.AppendLine($"    if (A_PriorKey != \"{priorKeyName}\")");
                scriptBuilder.AppendLine("    {");

                if (_settings.DoubleTap)
                {
                    scriptBuilder.AppendLine("        Interr_PriorKey := A_PriorKey");
                }

                if (!useWinSpace)
                {
                    scriptBuilder.AppendLine($"        Send, {ReleaseMappedButton()}");
                }

                scriptBuilder.AppendLine("        return");
                scriptBuilder.AppendLine("    }");

                if (_settings.DoubleTap)
                {
                    scriptBuilder.AppendLine($"    if (Interr_PriorKey != \"{priorKeyName}\" || (A_TickCount - First_Tap_Time) > 500)");
                    scriptBuilder.AppendLine("    {");
                    scriptBuilder.AppendLine("        First_Tap_Time := A_TickCount  ; Set First_Tap_Time to the current tick count");
                    scriptBuilder.AppendLine("        Interr_PriorKey := A_PriorKey");
                    scriptBuilder.AppendLine("        return");
                    scriptBuilder.AppendLine("    }");
                    scriptBuilder.AppendLine($"    if (Interr_PriorKey != \"{priorKeyName}\" || (A_TickCount - First_Tap_Time) > {_settings.DoubleTapTimeout})");
                    scriptBuilder.AppendLine("    {");
                    scriptBuilder.AppendLine("        First_Tap_Time := A_TickCount  ; Set First_Tap_Time to the current tick count");
                    scriptBuilder.AppendLine("        Interr_PriorKey := A_PriorKey");
                    scriptBuilder.AppendLine("        return");
                    scriptBuilder.AppendLine("    }");
                }

                string doubleTapCondition = _settings.DoubleTap ? $"Interr_PriorKey = \"{priorKeyName}\" && " : string.Empty;
                scriptBuilder.AppendLine($"    if ({doubleTapCondition}ElapsedTime < {timeout}) ; Time between press and release is less than 200 milliseconds");
                scriptBuilder.AppendLine("    {");
                scriptBuilder.AppendLine("        ; Get the class of the currently active window");
                scriptBuilder.AppendLine("        WinGetClass, activeWindowClass, A");
                scriptBuilder.AppendLine("        if (activeWindowClass = \"Windows.UI.Core.CoreWindow\" || activeWindowClass = \"Shell_TrayWnd\")");
                scriptBuilder.AppendLine("        {");
                scriptBuilder.AppendLine("            Send, {Esc}");
                scriptBuilder.AppendLine("        }");
                scriptBuilder.AppendLine("        ; Simulate Alt+Space");
                scriptBuilder.AppendLine($"        Send, {GetHotkeyInAhkFormat()}");
                scriptBuilder.AppendLine("        return");
                scriptBuilder.AppendLine("    }");
                scriptBuilder.AppendLine("return");

                string script = scriptBuilder.ToString();

                _ahk.ExecRaw(script);
            }
        }

        public void Unhook()
        {
            _ahk.Terminate();
        }

        public Control CreateSettingPanel()
        {
            return new WinHotkeySettings(_settings);
        }

        public void Dispose()
        {
            Unhook();
        }
    }


    public partial class WinHotkeySettings : UserControl
    {
        private readonly Settings _settings;
        public WinHotkeySettings(Settings settings)
        {
            this.DataContext = settings;
            this.InitializeComponent();
        }
    }

    
    public class Settings
    {
        public const string LWinSpaceModifier = "LWin + Space";
        private string _timeout = "200";
        public string _doubleTapTimeout = "500";
        public string DoubleTapTimeout
        {
            
            get
            {
                return _doubleTapTimeout;
            }
            set
            {
                if (Convert.ToInt32(value) < 200)
                {
                    _doubleTapTimeout = "200";
                }
                else
                {
                    _doubleTapTimeout = value;
                }
            }

        }
        public bool DoubleTap {get; set;} = false;
        public string InterrModifier {get; set;} = "LWin";

        [JsonIgnore]
        public List<string> Modifiers {get; } = new List<string> {"LWin", LWinSpaceModifier, "LControl", "LAlt"};
        public string Timeout
        {
            get
            {
                return _timeout;
            }
            set
            {
                _timeout = value;
            }

        }
    }
}
