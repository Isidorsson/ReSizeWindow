using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

namespace ReSizeWindow
{
    // Win + left drag moves the window under the cursor, Win + right drag resizes it.
    internal sealed class DragController : IDisposable
    {
        private enum Mode { None, Move, Resize }

        private static readonly string[] ShellClasses = { "Progman", "WorkerW", "Shell_TrayWnd", "Shell_SecondaryTrayWnd" };

        private readonly Native.LowLevelHookProc hookProc;
        private readonly IntPtr hook;
        private readonly StartMenuPolicy startMenu;

        private Mode mode;
        private int releaseMessage;
        private IntPtr target;
        private Box startRect;
        private Native.POINT startCursor;
        private Edges edges;

        public bool Enabled { get; set; }

        public DragController(StartMenuPolicy startMenu)
        {
            this.startMenu = startMenu;
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
            if (mode != Mode.None) return ContinueDrag(message, info.pt);
            if (!Enabled || (info.flags & Native.LLMHF_INJECTED) != 0 || !IsWinKeyDown()) return false;

            if (message == Native.WM_LBUTTONDOWN) return BeginDrag(Mode.Move, Native.WM_LBUTTONUP, info.pt);
            if (message == Native.WM_RBUTTONDOWN) return BeginDrag(Mode.Resize, Native.WM_RBUTTONUP, info.pt);
            return false;
        }

        private bool BeginDrag(Mode newMode, int release, Native.POINT cursor)
        {
            IntPtr window = TopLevelWindowAt(cursor);
            if (window == IntPtr.Zero) return false;

            bool maximized = Native.IsZoomed(window);
            if (newMode == Mode.Resize && (maximized || !IsResizable(window))) return false;

            startMenu.MarkUsed();
            // Injecting input makes this process the last input source, which Windows requires before handing over focus.
            StartMenuGuard.TapMaskKey();
            Native.SetForegroundWindow(window);

            Box rect = GetRect(window);
            if (maximized) rect = Restore(window, rect, cursor);

            mode = newMode;
            releaseMessage = release;
            target = window;
            startRect = rect;
            startCursor = cursor;
            edges = WindowMath.EdgesAt(rect, cursor.X, cursor.Y);
            return true;
        }

        private bool ContinueDrag(int message, Native.POINT cursor)
        {
            if (message == releaseMessage || !Native.IsWindow(target))
            {
                mode = Mode.None;
                return true;
            }
            if (message != Native.WM_MOUSEMOVE) return true;

            ApplyDrag(cursor);
            return false;
        }

        private void ApplyDrag(Native.POINT cursor)
        {
            int dx = cursor.X - startCursor.X;
            int dy = cursor.Y - startCursor.Y;
            uint flags = Native.SWP_NOZORDER | Native.SWP_NOACTIVATE | Native.SWP_ASYNCWINDOWPOS;

            Box rect;
            if (mode == Mode.Move)
            {
                rect = WindowMath.Offset(startRect, dx, dy);
                flags |= Native.SWP_NOSIZE;
            }
            else
            {
                int minW = Native.GetSystemMetrics(Native.SM_CXMINTRACK);
                int minH = Native.GetSystemMetrics(Native.SM_CYMINTRACK);
                rect = WindowMath.Resize(startRect, edges, dx, dy, minW, minH);
            }
            Native.SetWindowPos(target, IntPtr.Zero, rect.Left, rect.Top, rect.Width, rect.Height, flags);
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
            if (window == IntPtr.Zero) return IntPtr.Zero;

            var className = new StringBuilder(256);
            Native.GetClassName(window, className, className.Capacity);
            return Array.IndexOf(ShellClasses, className.ToString()) >= 0 ? IntPtr.Zero : window;
        }

        private static bool IsResizable(IntPtr window)
        {
            long style = Native.GetWindowLongPtr(window, Native.GWL_STYLE).ToInt64();
            return (style & Native.WS_THICKFRAME) != 0;
        }

        private static Box GetRect(IntPtr window)
        {
            Native.RECT r;
            Native.GetWindowRect(window, out r);
            return new Box(r.Left, r.Top, r.Right, r.Bottom);
        }

        private static bool IsWinKeyDown()
        {
            return (Native.GetAsyncKeyState(Native.VK_LWIN) & 0x8000) != 0
                || (Native.GetAsyncKeyState(Native.VK_RWIN) & 0x8000) != 0;
        }
    }
}
