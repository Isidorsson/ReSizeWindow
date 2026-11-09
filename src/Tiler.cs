using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace ReSizeWindow
{
    // Keeps eligible windows tiled in a dwindle layout per monitor while Enabled.
    internal sealed class Tiler : IDisposable
    {
        private const int GapDip = 8;
        private const int DefaultDpi = 96;
        private const int CoalesceMs = 50;

        private readonly TilingOrder order = new TilingOrder();
        private readonly Dictionary<IntPtr, List<double>> splits = new Dictionary<IntPtr, List<double>>();
        private readonly WinEventHooks hooks;
        private readonly Timer timer = new Timer();
        private bool enabled;
        private bool dragging;
        private IntPtr dragWindow;
        private Box dragStartFrame;

        public Tiler()
        {
            hooks = new WinEventHooks(OnWindowEvent);
            timer.Interval = CoalesceMs;
            timer.Tick += delegate
            {
                timer.Stop();
                if (!dragging) Reflow();
            };
        }

        public bool Enabled
        {
            get { return enabled; }
            set
            {
                if (value == enabled) return;
                if (value)
                {
                    hooks.Start();
                    enabled = true;
                    Reflow();
                    return;
                }
                enabled = false;
                dragging = false;
                hooks.Stop();
                timer.Stop();
                order.Clear();
                splits.Clear();
            }
        }

        public void Dispose()
        {
            Enabled = false;
            timer.Dispose();
            hooks.Dispose();
        }

        public void Refresh()
        {
            ScheduleReflow();
        }

        // Layout changes wait until the drag ends, so the dragged window isn't pulled into a cell mid-drag.
        public void DragStarted(IntPtr window)
        {
            dragging = true;
            dragWindow = window;
            dragStartFrame = FrameOf(window);
        }

        // A moved tracked window swaps with the tracked window under the cursor, or follows the
        // cursor to another monitor. A resized one moves the splits it borders, resizing its neighbours.
        public void Dropped(IntPtr window, Native.POINT cursor)
        {
            dragging = false;
            if (enabled && order.Contains(window))
            {
                if (WasResized(window)) ResizeTile(window);
                else Rearrange(window, cursor);
            }
            ScheduleReflow();
        }

        private void ResizeTile(IntPtr window)
        {
            IntPtr monitor = order.MonitorOf(window);
            TileArea tiles;
            if (!TryGetTiles(monitor, out tiles)) return;

            List<IntPtr> windows = order.Windows(monitor);
            splits[monitor] = TilingLayout.ResizeSplits(tiles, SplitsFor(monitor, windows.Count), windows.IndexOf(window), FrameOf(window));
        }

        private void Rearrange(IntPtr window, Native.POINT cursor)
        {
            IntPtr other = TrackedWindowAt(cursor, window);
            if (other != IntPtr.Zero)
            {
                order.Swap(window, other);
                return;
            }
            IntPtr monitor = Native.MonitorFromPoint(cursor, Native.MONITOR_DEFAULTTONEAREST);
            if (monitor != order.MonitorOf(window)) order.MoveToMonitor(window, monitor);
        }

        private bool WasResized(IntPtr window)
        {
            if (window != dragWindow) return false;
            Box frame = FrameOf(window);
            return frame.Width != dragStartFrame.Width || frame.Height != dragStartFrame.Height;
        }

        private void OnWindowEvent(uint eventType, IntPtr window)
        {
            if (!enabled) return;
            if (eventType == Native.EVENT_OBJECT_DESTROY && window == dragWindow) dragging = false;
            if (eventType == Native.EVENT_SYSTEM_MOVESIZESTART)
            {
                DragStarted(window);
                return;
            }
            if (eventType == Native.EVENT_SYSTEM_MOVESIZEEND)
            {
                Native.POINT cursor;
                Native.GetCursorPos(out cursor);
                Dropped(window, cursor);
                return;
            }
            if (Matters(eventType, window)) ScheduleReflow();
        }

        // Also called from the mouse hook, which must return quickly, so placement never happens inline.
        private void ScheduleReflow()
        {
            if (enabled && !timer.Enabled) timer.Start();
        }

        // Location changes are constant noise, including our own moves, so only a tracked window
        // leaving the tiled state or an untracked one becoming eligible (restored) counts.
        private bool Matters(uint eventType, IntPtr window)
        {
            bool tracked = order.Contains(window);
            if (eventType == Native.EVENT_OBJECT_LOCATIONCHANGE)
            {
                return tracked ? Native.IsZoomed(window) || Native.IsIconic(window) : WindowFilter.IsTileable(window);
            }
            return tracked || Native.GetAncestor(window, Native.GA_ROOT) == window;
        }

        private void Reflow()
        {
            Track();
            RehomeOrphans();
            foreach (IntPtr monitor in order.Monitors()) ReflowMonitor(monitor);
        }

        private void Track()
        {
            List<IntPtr> current = WindowFilter.EnumerateTileable();
            var live = new HashSet<IntPtr>(current);
            foreach (IntPtr window in order.All())
            {
                if (!live.Contains(window)) order.Remove(window);
            }
            foreach (IntPtr window in current)
            {
                if (!order.Contains(window)) order.Add(window, Native.MonitorFromWindow(window, Native.MONITOR_DEFAULTTONEAREST));
            }
        }

        // A disconnected monitor leaves a stale handle behind.
        private void RehomeOrphans()
        {
            foreach (IntPtr monitor in order.Monitors())
            {
                Box area;
                if (TryGetWorkArea(monitor, out area)) continue;
                splits.Remove(monitor);
                foreach (IntPtr window in order.Windows(monitor))
                {
                    order.MoveToMonitor(window, Native.MonitorFromWindow(window, Native.MONITOR_DEFAULTTONEAREST));
                }
            }
        }

        private void ReflowMonitor(IntPtr monitor)
        {
            TileArea tiles;
            if (!TryGetTiles(monitor, out tiles)) return;

            List<IntPtr> windows = order.Windows(monitor);
            WindowPlacer.Place(windows, TilingLayout.Dwindle(tiles, SplitsFor(monitor, windows.Count)));
        }

        private IntPtr TrackedWindowAt(Native.POINT cursor, IntPtr except)
        {
            foreach (IntPtr window in order.All())
            {
                if (window == except) continue;
                Box frame = FrameOf(window);
                bool inside = cursor.X >= frame.Left && cursor.X < frame.Right && cursor.Y >= frame.Top && cursor.Y < frame.Bottom;
                if (inside) return window;
            }
            return IntPtr.Zero;
        }

        private List<double> SplitsFor(IntPtr monitor, int windowCount)
        {
            List<double> stored;
            if (!splits.TryGetValue(monitor, out stored)) stored = new List<double>();
            return TilingLayout.FitSplits(stored, windowCount);
        }

        private bool TryGetTiles(IntPtr monitor, out TileArea tiles)
        {
            Box area;
            bool ok = TryGetWorkArea(monitor, out area);
            tiles = new TileArea(area, ok ? GapFor(order.Windows(monitor)[0]) : 0);
            return ok;
        }

        private static Box FrameOf(IntPtr window)
        {
            return WindowList.Measure(window).Frame;
        }

        private static bool TryGetWorkArea(IntPtr monitor, out Box area)
        {
            var info = new Native.MONITORINFO();
            info.cbSize = System.Runtime.InteropServices.Marshal.SizeOf(typeof(Native.MONITORINFO));
            bool ok = Native.GetMonitorInfo(monitor, ref info);
            area = new Box(info.rcWork.Left, info.rcWork.Top, info.rcWork.Right, info.rcWork.Bottom);
            return ok;
        }

        private static int GapFor(IntPtr window)
        {
            uint dpi = Native.GetDpiForWindow(window);
            if (dpi == 0) dpi = DefaultDpi;
            return (int)((GapDip * dpi + DefaultDpi / 2) / DefaultDpi);
        }
    }
}
