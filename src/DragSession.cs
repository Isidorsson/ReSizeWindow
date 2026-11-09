using System;
using System.Collections.Generic;

namespace ReSizeWindow
{
    internal enum DragMode { Move, Resize }

    internal struct DragStart
    {
        public IntPtr Window;
        public Box Rect;
        public Native.POINT Cursor;
        public bool MoveTouching;
    }

    // One Win+drag gesture: works out the geometry for each mouse move, including snapping
    // and the touching windows that follow the dragged one.
    internal sealed class DragSession
    {
        private const int SnapThresholdDip = 15;
        private const int BaseDpi = 96;
        private const uint PlaceFlags = Native.SWP_NOZORDER | Native.SWP_NOACTIVATE;

        private readonly DragMode mode;
        private readonly DragStart start;
        private readonly Edges edges;
        private readonly Insets resizeInsets;
        private readonly int minWidth, minHeight;
        private readonly WindowInfo[] followers;
        private readonly SnapContext snap;

        public IntPtr Window { get { return start.Window; } }

        public DragSession(DragMode mode, DragStart start)
        {
            this.mode = mode;
            this.start = start;
            edges = mode == DragMode.Resize ? WindowMath.EdgesAt(start.Rect, start.Cursor.X, start.Cursor.Y) : Edges.None;
            minWidth = Native.GetSystemMetrics(Native.SM_CXMINTRACK);
            minHeight = Native.GetSystemMetrics(Native.SM_CYMINTRACK);

            WindowInfo primary = WindowList.Measure(start.Window);
            resizeInsets = primary.Insets;

            List<WindowInfo> others = WindowList.Snapshot(start.Window);
            followers = start.MoveTouching ? FindFollowers(primary.Frame, others) : new WindowInfo[0];
            snap = BuildSnapContext(others);
        }

        public void Apply(Native.POINT cursor)
        {
            int dx = cursor.X - start.Cursor.X;
            int dy = cursor.Y - start.Cursor.Y;
            if (mode == DragMode.Move) Move(dx, dy);
            else Resize(dx, dy);
        }

        private void Move(int dx, int dy)
        {
            if (!SnappingSuppressed())
            {
                Insets insets = WindowList.Measure(start.Window).Insets;
                Box frame = WindowMath.Shrink(WindowMath.Offset(start.Rect, dx, dy), insets);
                Box snapped = Snapping.SnapMove(frame, snap);
                dx += snapped.Left - frame.Left;
                dy += snapped.Top - frame.Top;
            }

            if (followers.Length == 0) PlaceMoved(start.Window, start.Rect, dx, dy);
            else MoveGroup(dx, dy);
        }

        private void Resize(int dx, int dy)
        {
            Box rect = WindowMath.Resize(start.Rect, edges, dx, dy, minWidth, minHeight);
            if (!SnappingSuppressed())
            {
                Box frame = WindowMath.Shrink(rect, resizeInsets);
                rect = WindowMath.Grow(Snapping.SnapResize(frame, edges, snap), resizeInsets);
            }
            Native.SetWindowPos(start.Window, IntPtr.Zero, rect.Left, rect.Top, rect.Width, rect.Height,
                PlaceFlags | Native.SWP_ASYNCWINDOWPOS);
        }

        private void MoveGroup(int dx, int dy)
        {
            PlaceMoved(start.Window, start.Rect, dx, dy);
            foreach (WindowInfo follower in followers) PlaceMoved(follower.Handle, follower.Rect, dx, dy);
        }

        // Async so a hung window can't stall the mouse hook past its timeout.
        private static void PlaceMoved(IntPtr window, Box from, int dx, int dy)
        {
            Native.SetWindowPos(window, IntPtr.Zero, from.Left + dx, from.Top + dy, 0, 0,
                PlaceFlags | Native.SWP_NOSIZE | Native.SWP_ASYNCWINDOWPOS);
        }

        private static bool SnappingSuppressed()
        {
            return Native.IsKeyDown(Native.VK_CONTROL);
        }

        private static WindowInfo[] FindFollowers(Box primaryFrame, List<WindowInfo> others)
        {
            List<WindowInfo> movable = others.FindAll(w => !w.Maximized);
            Box[] frames = movable.ConvertAll(w => w.Frame).ToArray();
            return Array.ConvertAll(TouchGroup.Find(primaryFrame, frames), i => movable[i]);
        }

        // Windows travelling with the dragged one are not snap targets.
        private SnapContext BuildSnapContext(List<WindowInfo> others)
        {
            var targets = new List<Box>();
            foreach (WindowInfo window in others)
            {
                IntPtr handle = window.Handle;
                if (!Array.Exists(followers, f => f.Handle == handle)) targets.Add(window.Frame);
            }

            double scale = DpiOf(start.Window) / (double)BaseDpi;
            return new SnapContext(targets.ToArray(), WindowList.WorkAreas(), (int)Math.Round(SnapThresholdDip * scale),
                Math.Max(0, minWidth - resizeInsets.Left - resizeInsets.Right),
                Math.Max(0, minHeight - resizeInsets.Top - resizeInsets.Bottom));
        }

        private static uint DpiOf(IntPtr window)
        {
            uint dpi = Native.GetDpiForWindow(window);
            return dpi == 0 ? BaseDpi : dpi;
        }
    }
}
