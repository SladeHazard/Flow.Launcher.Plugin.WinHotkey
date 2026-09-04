using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Input;

namespace Flow.Launcher.Plugin.WinHotkey
{
    internal sealed class NativeHotkeySender
    {
        private const uint InputKeyboard = 1;
        private const uint KeyEventKeyUp = 0x0002;

        private readonly ushort[] _virtualKeys;

        public NativeHotkeySender(string hotkey)
        {
            if (string.IsNullOrWhiteSpace(hotkey))
            {
                throw new ArgumentException("Flow Launcher's hotkey cannot be empty.", nameof(hotkey));
            }

            _virtualKeys = hotkey
                .Split('+')
                .Select(part => ConvertToVirtualKey(part.Trim()))
                .ToArray();
        }

        public void Send()
        {
            var inputs = new List<Input>(_virtualKeys.Length * 2);

            foreach (var virtualKey in _virtualKeys)
            {
                inputs.Add(CreateKeyboardInput(virtualKey, 0));
            }

            for (var index = _virtualKeys.Length - 1; index >= 0; index--)
            {
                inputs.Add(CreateKeyboardInput(_virtualKeys[index], KeyEventKeyUp));
            }

            var inputArray = inputs.ToArray();
            var sent = SendInput((uint)inputArray.Length, inputArray, Marshal.SizeOf<Input>());
            if (sent != (uint)inputArray.Length)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Unable to send Flow Launcher's configured hotkey.");
            }
        }

        private static ushort ConvertToVirtualKey(string keyName)
        {
            var key = keyName.ToLowerInvariant() switch
            {
                "alt" => Key.LeftAlt,
                "ctrl" or "control" => Key.LeftCtrl,
                "shift" => Key.LeftShift,
                "win" or "windows" => Key.LWin,
                "back" => Key.Back,
                "next" => Key.PageDown,
                _ => (Key)new KeyConverter().ConvertFromInvariantString(keyName)
            };

            var virtualKey = KeyInterop.VirtualKeyFromKey(key);
            if (virtualKey == 0)
            {
                throw new NotSupportedException($"Flow Launcher hotkey key '{keyName}' is not supported.");
            }

            return (ushort)virtualKey;
        }

        private static Input CreateKeyboardInput(ushort virtualKey, uint flags)
        {
            return new Input
            {
                Type = InputKeyboard,
                Data = new InputUnion
                {
                    Keyboard = new KeyboardInput
                    {
                        VirtualKey = virtualKey,
                        Flags = flags
                    }
                }
            };
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct Input
        {
            public uint Type;
            public InputUnion Data;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct InputUnion
        {
            [FieldOffset(0)]
            public KeyboardInput Keyboard;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct KeyboardInput
        {
            public ushort VirtualKey;
            public ushort ScanCode;
            public uint Flags;
            public uint Time;
            public UIntPtr ExtraInfo;
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint inputCount, Input[] inputs, int inputSize);
    }
}
