using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Flow.Launcher.Plugin.WinHotkey
{
    internal sealed class NativeHotkeyHook : IDisposable
    {
        private const int WhKeyboardLl = 13;
        private const int WmKeyDown = 0x0100;
        private const int WmKeyUp = 0x0101;
        private const int WmSysKeyDown = 0x0104;
        private const int WmSysKeyUp = 0x0105;
        private const uint LlkhfInjected = 0x00000010;

        private const uint VkSpace = 0x20;
        private const uint VkLwin = 0x5B;
        private const uint VkLcontrol = 0xA2;
        private const uint VkLmenu = 0xA4;

        private readonly Settings _settings;
        private readonly Action _trigger;
        private readonly LowLevelKeyboardProc _callback;

        private IntPtr _hookHandle;
        private bool _triggerKeyDown;
        private bool _tapInterrupted;
        private long _pressStartedAt;
        private long _lastTapAt;

        public NativeHotkeyHook(Settings settings, Action trigger)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _trigger = trigger ?? throw new ArgumentNullException(nameof(trigger));
            _callback = HookCallback;
        }

        public void Start()
        {
            if (_hookHandle != IntPtr.Zero)
            {
                return;
            }

            using var process = Process.GetCurrentProcess();
            using var module = process.MainModule;
            var moduleHandle = GetModuleHandle(module?.ModuleName);
            _hookHandle = SetWindowsHookEx(WhKeyboardLl, _callback, moduleHandle, 0);

            if (_hookHandle == IntPtr.Zero)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Unable to install the keyboard hook.");
            }
        }

        private IntPtr HookCallback(int code, IntPtr message, IntPtr data)
        {
            if (code >= 0)
            {
                var keyboardData = Marshal.PtrToStructure<KbdLlHookStruct>(data);
                if ((keyboardData.Flags & LlkhfInjected) == 0)
                {
                    ProcessKey(keyboardData.VirtualKeyCode, message);
                }
            }

            return CallNextHookEx(_hookHandle, code, message, data);
        }

        private void ProcessKey(uint virtualKey, IntPtr message)
        {
            var isKeyDown = message == (IntPtr)WmKeyDown || message == (IntPtr)WmSysKeyDown;
            var isKeyUp = message == (IntPtr)WmKeyUp || message == (IntPtr)WmSysKeyUp;
            if (!isKeyDown && !isKeyUp)
            {
                return;
            }

            var modifierKey = GetConfiguredModifierKey();
            var triggerKey = IsSpaceChord ? VkSpace : modifierKey;

            if (isKeyDown && virtualKey == triggerKey)
            {
                if (IsSpaceChord && !IsKeyDown(modifierKey))
                {
                    return;
                }

                if (!_triggerKeyDown)
                {
                    _triggerKeyDown = true;
                    _tapInterrupted = false;
                    _pressStartedAt = Environment.TickCount64;
                }

                return;
            }

            if (isKeyDown && _triggerKeyDown && virtualKey != modifierKey)
            {
                _tapInterrupted = true;
            }

            if (isKeyUp && virtualKey == triggerKey && _triggerKeyDown)
            {
                var elapsed = Environment.TickCount64 - _pressStartedAt;
                var modifierStillDown = !IsSpaceChord || IsKeyDown(modifierKey);
                _triggerKeyDown = false;

                if (!_tapInterrupted && modifierStillDown && elapsed < _settings.PressTimeoutMilliseconds)
                {
                    RegisterTap();
                }
            }
        }

        private void RegisterTap()
        {
            var now = Environment.TickCount64;
            if (!_settings.DoubleTap)
            {
                _trigger();
                return;
            }

            if (_lastTapAt != 0 && now - _lastTapAt <= _settings.DoubleTapTimeoutMilliseconds)
            {
                _lastTapAt = 0;
                _trigger();
            }
            else
            {
                _lastTapAt = now;
            }
        }

        private bool IsSpaceChord =>
            _settings.InterrModifier == Settings.LWinSpaceModifier ||
            _settings.InterrModifier == Settings.LCtrlSpaceModifier;

        private uint GetConfiguredModifierKey()
        {
            return _settings.InterrModifier switch
            {
                "LAlt" => VkLmenu,
                "LControl" => VkLcontrol,
                Settings.LCtrlSpaceModifier => VkLcontrol,
                _ => VkLwin
            };
        }

        private static bool IsKeyDown(uint virtualKey)
        {
            return (GetAsyncKeyState((int)virtualKey) & 0x8000) != 0;
        }

        public void Dispose()
        {
            if (_hookHandle == IntPtr.Zero)
            {
                return;
            }

            UnhookWindowsHookEx(_hookHandle);
            _hookHandle = IntPtr.Zero;
        }

        private delegate IntPtr LowLevelKeyboardProc(int code, IntPtr message, IntPtr data);

        [StructLayout(LayoutKind.Sequential)]
        private struct KbdLlHookStruct
        {
            public uint VirtualKeyCode;
            public uint ScanCode;
            public uint Flags;
            public uint Time;
            public UIntPtr ExtraInfo;
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(
            int hookId,
            LowLevelKeyboardProc callback,
            IntPtr moduleHandle,
            uint threadId);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hookHandle);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(
            IntPtr hookHandle,
            int code,
            IntPtr message,
            IntPtr data);

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int virtualKey);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string moduleName);
    }
}
