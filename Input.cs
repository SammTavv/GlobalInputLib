using System.ComponentModel;
using System.Runtime.InteropServices;

namespace GlobalInputLib
{
    public static class Input
    {
        static Input()
        {
            // release when exiting
            AppDomain.CurrentDomain.ProcessExit += (_, _) =>
            {
                ReleaseKeys();
            };

            Console.CancelKeyPress += (_, _) =>
            {
                ReleaseKeys();
            };
        }

        // i fucking hate windows code omg

        private const uint releaseKey = 0x0002;
        private const uint useScanCode = 0x0008;
        private const uint mouseMove = 0x0001;
        private const uint mouseLeftDown = 0x0002;
        private const uint mouseLeftUp = 0x0004;
        private const uint mouseRightDown = 0x0008;
        private const uint mouseRightUp = 0x0010;
        private const uint mouseMiddleDown = 0x0020;
        private const uint mouseMiddleUp = 0x0040;
        private const uint mouseScrollVertical = 0x0800;
        private const uint mouseScrollHorizontal = 0x01000;
        private const uint mouseMoveAbsolute = 0x8000;
        private const uint useExtendedKey = 0x0001;

        private static readonly object inputLock = new();
        private static readonly HashSet<VirtualKey> heldKeys = [];

        [StructLayout(LayoutKind.Explicit)]
        private struct InputUnion
        {
            [FieldOffset(0)]
            public MOUSEINPUT mouse;
            [FieldOffset(0)]
            public KEYBDINPUT keyboard;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct INPUT
        {
            /// <summary>
            /// 0 for mouse, 1 for keyboard
            /// </summary>
            public uint type;

            /// <summary>
            /// make a new inputunion struct with the appropriate type
            /// </summary>
            public InputUnion data;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct KEYBDINPUT
        {
            /// <summary>
            /// virtual key
            /// </summary>
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MOUSEINPUT
        {
            /// <summary>
            /// new x
            /// </summary>
            public int dx;
            /// <summary>
            /// new y
            /// </summary>
            public int dy;
            public uint mouseData;
            /// <summary>
            /// the mouse flag to use
            /// </summary>
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="nInputs">number of inputs</param>
        /// <param name="pInputs">new array with an existing input struct</param>
        /// <param name="cbSize"><see cref="Marshal.SizeOf{T}(INPUT)"/></param>
        /// <returns></returns>
        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        private static void SendInputSafe(INPUT[] inputs)
        {
            uint sent = SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());

            if (sent != inputs.Length)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="uCode">virtual key</param>
        /// <param name="uMapType">0 is vk to vsc, 4 is for extended keys</param>
        /// <returns></returns>
        [DllImport("user32.dll")]
        private static extern uint MapVirtualKey(uint uCode, uint uMapType);

        private static void SendKey(VirtualKey vk, bool keyUp)
        {
            uint mapped = MapVirtualKey((uint)vk, 4);
            ushort scanCode = (ushort)(mapped & 0xFF);
            uint prefix = (mapped >> 8) & 0xFF;

            uint flags = useScanCode;

            if (prefix == 0xE0) flags |= useExtendedKey;
            if (keyUp) flags |= releaseKey;

            INPUT input = new()
            {
                type = 1,
                data = new InputUnion
                {
                    keyboard = new KEYBDINPUT
                    {
                        wVk = 0,
                        wScan = scanCode,
                        dwFlags = flags,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            };

            SendInput(1, [input], Marshal.SizeOf<INPUT>());
        }

        /// <summary>
        /// Presses a key
        /// </summary>
        /// <param name="vk">the <see cref="VirtualKey"/> to press</param>
        public static void KeyDown(VirtualKey vk)
        {
            // prevent key press multiple times at once until released
            lock (inputLock)
            {
                if (heldKeys.Contains(vk)) return;

                SendKey(vk, false);
                heldKeys.Add(vk);
            }
        }

        /// <summary>
        /// Releases a key
        /// </summary>
        /// <param name="vk">the <see cref="VirtualKey"/> to release</param>
        public static void KeyUp(VirtualKey vk)
        {
            lock (inputLock)
            {
                if (!heldKeys.Contains(vk)) return;

                SendKey(vk, true);
                heldKeys.Remove(vk);
            }
        }

        /// <summary>
        /// Moves the mouse by an amount of pixels
        /// </summary>
        /// <param name="dx">pixels on the x axis</param>
        /// <param name="dy">pixels on the y axis</param>
        public static void MouseMoveBy(int dx, int dy)
        {
            INPUT input = new()
            {
                type = 0,
                data = new InputUnion
                {
                    mouse = new MOUSEINPUT
                    {
                        dx = dx,
                        dy = dy,
                        mouseData = 0,
                        dwFlags = mouseMove,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            };

            SendInputSafe([input]);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="nIndex">0 for width, 1 for height</param>
        /// <returns></returns>
        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);

        /// <summary>
        /// Moves the mouse to a position on the screen
        /// </summary>
        /// <param name="nx">pixels on the x axis</param>
        /// <param name="ny">pixels on the y axis</param>
        public static void MouseMoveTo(int x, int y)
        {
            int screenWidth = GetSystemMetrics(0);
            int screenHeight = GetSystemMetrics(1);

            int absoluteX = (int)Math.Round(x * 65535.0 / (screenWidth - 1));

            int absoluteY = (int)Math.Round(y * 65535.0 / (screenHeight - 1));

            INPUT input = new()
            {
                type = 0,
                data = new InputUnion
                {
                    mouse = new MOUSEINPUT
                    {
                        dx = absoluteX,
                        dy = absoluteY,
                        mouseData = 0,
                        dwFlags = mouseMove | mouseMoveAbsolute,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            };

            SendInputSafe([input]);
        }

        private static void SendMouseBtn(uint flags)
        {
            INPUT input = new INPUT
            {
                type = 0,
                data = new InputUnion
                {
                    mouse = new MOUSEINPUT
                    {
                        dx = 0,
                        dy = 0,
                        mouseData = 0,
                        dwFlags = flags,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            };

            SendInput(1, [input], Marshal.SizeOf<INPUT>());
        }

        public static void MouseLeftDown() => SendMouseBtn(mouseLeftDown);
        public static void MouseLeftUp() => SendMouseBtn(mouseLeftUp);
        public static void MouseRightDown() => SendMouseBtn(mouseRightDown);
        public static void MouseRightUp() => SendMouseBtn(mouseRightUp);
        public static void MouseMiddleDown() => SendMouseBtn(mouseMiddleDown);
        public static void MouseMiddleUp() => SendMouseBtn(mouseMiddleUp);

        /// <summary>
        /// Scrolls the mouse vertically
        /// </summary>
        /// <param name="amount">positive scrolls up, negative scrolls down. default is 120</param>
        public static void MouseScrollVertical(int amount)
        {
            INPUT input = new()
            {
                type = 0,
                data = new InputUnion
                {
                    mouse = new MOUSEINPUT
                    {
                        dx = 0,
                        dy = 0,
                        mouseData = (uint)amount,
                        dwFlags = mouseScrollVertical,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            };

            SendInputSafe([input]);
        }

        /// <summary>
        /// Scrolls the mouse horizontally
        /// </summary>
        /// <param name="amount">positive scrolls right, negative scrolls left. default is 120</param>
        public static void MouseScrollHorizontal(int amount)
        {
            INPUT input = new()
            {
                type = 0,
                data = new InputUnion
                {
                    mouse = new MOUSEINPUT
                    {
                        dx = 0,
                        dy = 0,
                        mouseData = (uint)amount,
                        dwFlags = mouseScrollHorizontal,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            };

            SendInputSafe([input]);
        }


        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetAsyncKeyState(int vKey);
        
        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT point);

        /// <summary>
        /// Checks if a key is down
        /// </summary>
        /// <param name="vk">the <see cref="VirtualKey"/> to check</param>
        /// <returns></returns>
        public static bool IsKeyDown(VirtualKey vk)
        {
            // 0x8000 is if the key is pressed down
            return (GetAsyncKeyState((int)vk) & 0x8000) != 0;
        }

        public static (int X, int Y) MousePosition
        {
            get
            {
                GetCursorPos(out POINT p);
                return (p.X, p.Y);
            }
        }

        public static HashSet<VirtualKey> HeldKeys
        {
            get
            {
                lock (inputLock)
                {
                    return new HashSet<VirtualKey>(heldKeys);
                }
            }
        }

        /// <summary>
        /// Presses and immediately releases a key
        /// </summary>
        /// <param name="vk">the <see cref="VirtualKey"/> to tap</param>
        public static void KeyTap(VirtualKey vk)
        {
            KeyDown(vk);
            KeyUp(vk);
        }

        public static void ReleaseKeys()
        {
            lock (inputLock)
            {
                foreach (VirtualKey key in new List<VirtualKey>(heldKeys))
                {
                    try
                    {
                        SendKey(key, true);
                        heldKeys.Remove(key);
                    }
                    catch
                    {
                    }
                }
            }
        }
    }
}