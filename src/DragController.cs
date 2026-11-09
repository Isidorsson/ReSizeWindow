using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace ReSizeWindow
{
    // Win + left drag moves the window under the cursor, Win + right drag resizes it.
    // Win + Shift + left drag moves the windows touching it as well.
    internal sealed class DragController : IDisposable
    {
        private readonly Native.LowLevelHookProc hookProc;
        private readonly IntPtr hook;
        private readonly StartMenuPolicy startMenu;
        private readonly Tiler tiler;

        private DragSession session;
        private int releaseMessage;

        public bool Enabled { get; set; }

        public DragController(StartMenuPolicy startMenu, Tiler tiler)
        {
            this.startMenu = startMenu;
            this.tiler = tiler;
            Enabled = true;
            hookProc = HookCallback;
            hook = Native.SetWindowsHookEx(Native.WH_MOUSE_LL, hookProc, Native.GetModuleHandle(null), 0);
            if (hook == IntPtr.Zero) throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to install mouse hook");
        }

        public void Dispose()
        {
            Native.UnhookWindowsHookEx(hook);
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                var info = (Native.MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(Native.MSLLHOOKSTRUCT));
                if (Handle(wParam.ToInt32(), info)) return new IntPtr(1);
            }
            return Native.CallNextHookEx(hook, nCode, wParam, lParam);
        }

        // Returns true when the event should be swallowed.
        private bool Handle(int message, Native.MSLLHOOKSTRUCT info)
        {
            if (session != null) return ContinueDrag(message, info.pt);
            if (!Enabled || (info.flags & Native.LLMHF_INJECTED) != 0 || !IsWinKeyDown()) return false;

            if (message == Native.WM_LBUTTONDOWN) return BeginDrag(DragMode.Move, Native.WM_LBUTTONUP, info.pt);
            if (message == Native.WM_RBUTTONDOWN) return BeginDrag(DragMode.Resize, Native.WM_RBUTTONUP, info.pt);
            return false;
        }

        private bool BeginDrag(DragMode mode, int release, Native.POINT cursor)
        {
            IntPtr window = TopLevelWindowAt(cursor);
            if (window == IntPtr.Zero) return false;

            bool maximized = Native.IsZoomed(window);
            if (mode == DragMode.Resize && (maximized || !IsResizable(window))) return false;

            startMenu.MarkUsed();
            // Injecting input makes this process the last input source, which Windows requires before handing over focus.
            StartMenuGuard.TapMaskKey();
            Native.SetForegroundWindow(window);

            Box rect = WindowList.Measure(window).Rect;
            if (maximized) rect = Restore(window, rect, cursor);

            tiler.DragStarted(window);
            releaseMessage = release;
            session = new DragSession(mode, new DragStart
            {
                Window = window,
                Rect = rect,
                Cursor = cursor,
                MoveTouching = mode == DragMode.Move && !maximized && Native.IsKeyDown(Native.VK_SHIFT)
            });
            return true;
        }

        private bool ContinueDrag(int message, Native.POINT cursor)
        {
            if (message == releaseMessage || !Native.IsWindow(session.Window))
            {
                tiler.Dropped(session.Window, cursor);
                session = null;
                return true;
            }
            if (message != Native.WM_MOUSEMOVE) return true;

            session.Apply(cursor);
            return false;
        }

        private static Box Restore(IntPtr window, Box maximized, Native.POINT cursor)
        {
            var placement = new Native.WINDOWPLACEMENT();
            placement.length = Marshal.SizeOf(typeof(Native.WINDOWPLACEMENT));
            Native.GetWindowPlacement(window, ref placement);

            Native.RECT normal = placement.rcNormalPosition;
            Box rect = WindowMath.Unmaximize(maximized, normal.Right - normal.Left, normal.Bottom - normal.Top, cursor.X);

            Native.ShowWindowAsync(window, Native.SW_RESTORE);
            Native.SetWindowPos(window, IntPtr.Zero, rect.Left, rect.Top, rect.Width, rect.Height,
                Native.SWP_NOZORDER | Native.SWP_NOACTIVATE | Native.SWP_ASYNCWINDOWPOS);
            return rect;
        }

        private static IntPtr TopLevelWindowAt(Native.POINT point)
        {
            IntPtr window = Native.GetAncestor(Native.WindowFromPoint(point), Native.GA_ROOT);
            return window == IntPtr.Zero || WindowList.IsShellWindow(window) ? IntPtr.Zero : window;
        }

        private static bool IsResizable(IntPtr window)
        {
            long style = Native.GetWindowLongPtr(window, Native.GWL_STYLE).ToInt64();
            return (style & Native.WS_THICKFRAME) != 0;
        }

        private static bool IsWinKeyDown()
        {
            return Native.IsKeyDown(Native.VK_LWIN) || Native.IsKeyDown(Native.VK_RWIN);
        }
    }
}
