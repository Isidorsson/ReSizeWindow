using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace ReSizeWindow
{
    // Stops the Start menu opening on Win release by swallowing the release and replaying it
    // after a no-op key, so Windows sees Win as used in a combo.
    internal sealed class StartMenuGuard : IDisposable
    {
        public readonly StartMenuPolicy Policy = new StartMenuPolicy();

        private readonly Native.LowLevelHookProc hookProc;
        private readonly IntPtr hook;

        public StartMenuGuard()
        {
            hookProc = HookCallback;
            hook = Native.SetWindowsHookEx(Native.WH_KEYBOARD_LL, hookProc, Native.GetModuleHandle(null), 0);
            if (hook == IntPtr.Zero) throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to install keyboard hook");
        }

        public void Dispose()
        {
            Native.UnhookWindowsHookEx(hook);
        }

        public static void TapMaskKey()
        {
            Native.keybd_event(Native.VK_MENU_MASK, 0, 0, UIntPtr.Zero);
            Native.keybd_event(Native.VK_MENU_MASK, 0, Native.KEYEVENTF_KEYUP, UIntPtr.Zero);
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                var info = (Native.KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(Native.KBDLLHOOKSTRUCT));
                if (IsPhysicalWinRelease(wParam.ToInt32(), info) && Policy.ShouldMaskRelease(info.vkCode == Native.VK_LWIN))
                {
                    TapMaskKey();
                    Native.keybd_event((byte)info.vkCode, 0, Native.KEYEVENTF_KEYUP | Native.KEYEVENTF_EXTENDEDKEY, UIntPtr.Zero);
                    return new IntPtr(1);
                }
            }
            return Native.CallNextHookEx(hook, nCode, wParam, lParam);
        }

        private static bool IsPhysicalWinRelease(int message, Native.KBDLLHOOKSTRUCT info)
        {
            bool isRelease = message == Native.WM_KEYUP || message == Native.WM_SYSKEYUP;
            bool isWin = info.vkCode == Native.VK_LWIN || info.vkCode == Native.VK_RWIN;
            return isRelease && isWin && (info.flags & Native.LLKHF_INJECTED) == 0;
        }
    }
}
