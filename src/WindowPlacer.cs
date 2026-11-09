using System;
using System.Collections.Generic;

namespace ReSizeWindow
{
    // Moves windows so their visible frames land exactly on the given boxes.
    internal static class WindowPlacer
    {
        // Async so a hung window can't stall this thread, which also runs the input hooks.
        private const uint Flags = Native.SWP_NOZORDER | Native.SWP_NOACTIVATE | Native.SWP_ASYNCWINDOWPOS;

        public static void Place(IList<IntPtr> windows, IList<Box> targets)
        {
            for (int i = 0; i < windows.Count; i++)
            {
                Box rect;
                if (!TryGetRect(windows[i], out rect)) continue;

                Box visible = WindowList.VisibleFrame(windows[i], rect);
                if (visible.Equals(targets[i])) continue;
                Box b = TilingLayout.Compensate(targets[i], visible, rect);
                // A window that is gone or refuses (elevated) is skipped; one with a larger minimum size stays bigger than its cell.
                Native.SetWindowPos(windows[i], IntPtr.Zero, b.Left, b.Top, b.Width, b.Height, Flags);
            }
        }

        private static bool TryGetRect(IntPtr window, out Box box)
        {
            Native.RECT r;
            bool ok = Native.GetWindowRect(window, out r);
            box = new Box(r.Left, r.Top, r.Right, r.Bottom);
            return ok;
        }
    }
}
