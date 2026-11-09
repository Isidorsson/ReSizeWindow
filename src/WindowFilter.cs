using System;
using System.Collections.Generic;

namespace ReSizeWindow
{
    // Decides which windows auto tiling manages. Everything else floats.
    internal static class WindowFilter
    {
        public static List<IntPtr> EnumerateTileable()
        {
            var windows = new List<IntPtr>();
            Native.EnumWindows(delegate(IntPtr window, IntPtr lParam)
            {
                if (IsTileable(window)) windows.Add(window);
                return true;
            }, IntPtr.Zero);
            return windows;
        }

        public static bool IsTileable(IntPtr window)
        {
            if (!Native.IsWindowVisible(window) || Native.IsIconic(window) || Native.IsZoomed(window)) return false;
            if (Native.GetWindow(window, Native.GW_OWNER) != IntPtr.Zero) return false;
            if (!HasTileableStyle(window) || WindowList.IsCloaked(window)) return false;
            if (Native.GetWindowTextLength(window) == 0) return false;
            return !WindowList.IsShellWindow(window) && !WindowList.IsOwnWindow(window);
        }

        private static bool HasTileableStyle(IntPtr window)
        {
            uint style = (uint)Native.GetWindowLongPtr(window, Native.GWL_STYLE).ToInt64();
            uint exStyle = (uint)Native.GetWindowLongPtr(window, Native.GWL_EXSTYLE).ToInt64();
            bool captioned = (style & Native.WS_CAPTION) == Native.WS_CAPTION;
            bool resizable = (style & Native.WS_THICKFRAME) != 0;
            bool floating = (exStyle & (Native.WS_EX_TOOLWINDOW | Native.WS_EX_TOPMOST)) != 0;
            return captioned && resizable && !floating;
        }
    }
}
