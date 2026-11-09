using System;
using System.Media;
using System.Windows.Forms;

namespace ReSizeWindow
{
    // Win+Ctrl+T toggles always-on-top for the foreground window.
    internal sealed class AlwaysOnTop : IDisposable
    {
        private const int HotkeyId = 1;
        private const uint HotkeyModifiers = Native.MOD_WIN | Native.MOD_CONTROL | Native.MOD_NOREPEAT;
        private const uint HotkeyKey = 'T';

        private readonly MessageWindow window;

        public event Action Toggled;

        public bool Enabled { get; set; }
        public bool HotkeyRegistered { get; private set; }

        public AlwaysOnTop()
        {
            Enabled = true;
            window = new MessageWindow(OnHotkey);
            HotkeyRegistered = Native.RegisterHotKey(window.Handle, HotkeyId, HotkeyModifiers, HotkeyKey);
        }

        public void Dispose()
        {
            if (HotkeyRegistered) Native.UnregisterHotKey(window.Handle, HotkeyId);
            window.DestroyHandle();
        }

        private void OnHotkey()
        {
            if (!Enabled) return;
            IntPtr target = Native.GetForegroundWindow();
            if (target == IntPtr.Zero || WindowList.IsShellWindow(target) || WindowList.IsOwnWindow(target)) return;
            if (!SetTopmost(target, !IsTopmost(target)))
            {
                SystemSounds.Beep.Play();
                return;
            }
            if (Toggled != null) Toggled();
        }

        private static bool IsTopmost(IntPtr hwnd)
        {
            return (Native.GetWindowLongPtr(hwnd, Native.GWL_EXSTYLE).ToInt64() & Native.WS_EX_TOPMOST) != 0;
        }

        private static bool SetTopmost(IntPtr hwnd, bool topmost)
        {
            IntPtr insertAfter = topmost ? Native.HWND_TOPMOST : Native.HWND_NOTOPMOST;
            return Native.SetWindowPos(hwnd, insertAfter, 0, 0, 0, 0,
                Native.SWP_NOMOVE | Native.SWP_NOSIZE | Native.SWP_NOACTIVATE);
        }

        private sealed class MessageWindow : NativeWindow
        {
            private readonly Action onHotkey;

            public MessageWindow(Action onHotkey)
            {
                this.onHotkey = onHotkey;
                CreateHandle(new CreateParams { Parent = Native.HWND_MESSAGE });
            }

            protected override void WndProc(ref Message m)
            {
                if (m.Msg == Native.WM_HOTKEY) onHotkey();
                else base.WndProc(ref m);
            }
        }
    }
}
