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
        private const ushort VkPacketMask = 0xFF;

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

            Send(inputs.ToArray(), "Unable to send Flow Launcher's configured hotkey.");
        }

        public void MaskWindowsStartMenu()
        {
            // This is the VK_FF masking sequence used by the former AHK script.
            // It prevents Windows from treating LWin as a standalone Start press.
            Send(
                new[]
                {
                    CreateKeyboardInput(VkPacketMask, 0),
                    CreateKeyboardInput(VkPacketMask, KeyEventKeyUp)
                },
                "Unable to mask the Windows Start-menu key sequence.");
        }

        private static void Send(Input[] inputs, string errorMessage)
        {
            var sent = SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<Input>());
            if (sent != (uint)inputs.Length)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), errorMessage);
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

            // MOUSEINPUT is the largest INPUT union member. Without it the
            // marshalled INPUT size is 32 bytes on x64 instead of the required
            // 40 bytes, and SendInput fails with ERROR_INVALID_PARAMETER (87).
            [FieldOffset(0)]
            public MouseInput Mouse;
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

        [StructLayout(LayoutKind.Sequential)]
        private struct MouseInput
        {
            public int X;
            public int Y;
            public uint MouseData;
            public uint Flags;
            public uint Time;
            public UIntPtr ExtraInfo;
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint inputCount, Input[] inputs, int inputSize);
    }
}
