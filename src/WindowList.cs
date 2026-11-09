using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace ReSizeWindow
{
    internal struct WindowInfo
    {
        public readonly IntPtr Handle;
        public readonly Box Rect;
        public readonly Box Frame;
        public readonly bool Maximized;

        public WindowInfo(IntPtr handle, Box rect, Box frame, bool maximized)
        {
            Handle = handle;
            Rect = rect;
            Frame = frame;
            Maximized = maximized;
        }

        public Insets Insets { get { return WindowMath.InsetsBetween(Rect, Frame); } }
    }

    // Reads window geometry from the system. Frame is the visible bounds reported by DWM,
    // Rect is what SetWindowPos works in, including the invisible resize borders.
    internal static class WindowList
    {
        private static readonly string[] ShellClasses = { "Progman", "WorkerW", "Shell_TrayWnd", "Shell_SecondaryTrayWnd" };
        private static readonly uint OwnProcessId = (uint)Process.GetCurrentProcess().Id;

        public static bool IsShellWindow(IntPtr window)
        {
            var className = new StringBuilder(256);
            Native.GetClassName(window, className, className.Capacity);
            return Array.IndexOf(ShellClasses, className.ToString()) >= 0;
        }

        public static bool IsOwnWindow(IntPtr window)
        {
            uint processId;
            Native.GetWindowThreadProcessId(window, out processId);
            return processId == OwnProcessId;
        }

        public static WindowInfo Measure(IntPtr window)
        {
            Native.RECT r;
            Native.GetWindowRect(window, out r);
            var rect = new Box(r.Left, r.Top, r.Right, r.Bottom);
            return new WindowInfo(window, rect, VisibleFrame(window, rect), Native.IsZoomed(window));
        }

        // Top-level windows that can be a snap target or travel with a dragged window.
        public static List<WindowInfo> Snapshot(IntPtr exclude)
        {
            var windows = new List<WindowInfo>();
            Native.EnumWindows(delegate(IntPtr window, IntPtr lParam)
            {
                if (window == exclude || !IsCandidate(window)) return true;
                WindowInfo info = Measure(window);
                if (info.Frame.Width > 0 && info.Frame.Height > 0) windows.Add(info);
                return true;
            }, IntPtr.Zero);
            return windows;
        }

        public static Box[] WorkAreas()
        {
            var areas = new List<Box>();
            Native.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, delegate(IntPtr monitor, IntPtr hdc, IntPtr clip, IntPtr data)
            {
                var info = new Native.MONITORINFO();
                info.cbSize = Marshal.SizeOf(typeof(Native.MONITORINFO));
                if (Native.GetMonitorInfo(monitor, ref info))
                    areas.Add(new Box(info.rcWork.Left, info.rcWork.Top, info.rcWork.Right, info.rcWork.Bottom));
                return true;
            }, IntPtr.Zero);
            return areas.ToArray();
        }

        private static bool IsCandidate(IntPtr window)
        {
            if (!Native.IsWindowVisible(window) || Native.IsIconic(window) || IsCloaked(window)) return false;

            long exStyle = Native.GetWindowLongPtr(window, Native.GWL_EXSTYLE).ToInt64();
            return (exStyle & Native.WS_EX_TOOLWINDOW) == 0 && !IsShellWindow(window);
        }

        public static bool IsCloaked(IntPtr window)
        {
            int cloaked;
            return Native.DwmGetWindowAttribute(window, Native.DWMWA_CLOAKED, out cloaked, sizeof(int)) == 0 && cloaked != 0;
        }

        // Without DWM composition there are no invisible borders, so the rect is the visible frame.
        public static Box VisibleFrame(IntPtr window, Box rect)
        {
            Native.RECT frame;
            int result = Native.DwmGetWindowAttribute(window, Native.DWMWA_EXTENDED_FRAME_BOUNDS, out frame, Marshal.SizeOf(typeof(Native.RECT)));
            return result == 0 ? new Box(frame.Left, frame.Top, frame.Right, frame.Bottom) : rect;
        }
    }
}
