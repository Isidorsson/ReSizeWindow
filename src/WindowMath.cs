using System;

namespace ReSizeWindow
{
    [Flags]
    internal enum Edges
    {
        None = 0,
        Left = 1,
        Top = 2,
        Right = 4,
        Bottom = 8
    }

    internal struct Box
    {
        public readonly int Left, Top, Right, Bottom;

        public Box(int left, int top, int right, int bottom)
        {
            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;
        }

        public int Width { get { return Right - Left; } }
        public int Height { get { return Bottom - Top; } }

        public override string ToString()
        {
            return string.Format("({0},{1},{2},{3})", Left, Top, Right, Bottom);
        }
    }

    // Gap between a window rect and its visible frame (the invisible resize borders).
    internal struct Insets
    {
        public readonly int Left, Top, Right, Bottom;

        public Insets(int left, int top, int right, int bottom)
        {
            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;
        }
    }

    internal static class WindowMath
    {
        public static Box Offset(Box start, int dx, int dy)
        {
            return new Box(start.Left + dx, start.Top + dy, start.Right + dx, start.Bottom + dy);
        }

        public static Insets InsetsBetween(Box rect, Box frame)
        {
            return new Insets(frame.Left - rect.Left, frame.Top - rect.Top, rect.Right - frame.Right, rect.Bottom - frame.Bottom);
        }

        public static Box Shrink(Box rect, Insets insets)
        {
            return new Box(rect.Left + insets.Left, rect.Top + insets.Top, rect.Right - insets.Right, rect.Bottom - insets.Bottom);
        }

        public static Box Grow(Box frame, Insets insets)
        {
            return new Box(frame.Left - insets.Left, frame.Top - insets.Top, frame.Right + insets.Right, frame.Bottom + insets.Bottom);
        }

        // The window is split into a 3x3 grid: outer cells grab that edge or corner,
        // the center cell grabs the nearest corner.
        public static Edges EdgesAt(Box window, int x, int y)
        {
            int thirdW = window.Width / 3;
            int thirdH = window.Height / 3;
            Edges edges = Edges.None;

            if (x < window.Left + thirdW) edges |= Edges.Left;
            else if (x >= window.Right - thirdW) edges |= Edges.Right;

            if (y < window.Top + thirdH) edges |= Edges.Top;
            else if (y >= window.Bottom - thirdH) edges |= Edges.Bottom;

            if (edges != Edges.None) return edges;

            int centerX = window.Left + window.Width / 2;
            int centerY = window.Top + window.Height / 2;
            return (x < centerX ? Edges.Left : Edges.Right) | (y < centerY ? Edges.Top : Edges.Bottom);
        }

        public static Box Resize(Box start, Edges edges, int dx, int dy, int minWidth, int minHeight)
        {
            int left = start.Left, top = start.Top, right = start.Right, bottom = start.Bottom;

            if ((edges & Edges.Left) != 0) left = Math.Min(start.Left + dx, start.Right - minWidth);
            if ((edges & Edges.Right) != 0) right = Math.Max(start.Right + dx, start.Left + minWidth);
            if ((edges & Edges.Top) != 0) top = Math.Min(start.Top + dy, start.Bottom - minHeight);
            if ((edges & Edges.Bottom) != 0) bottom = Math.Max(start.Bottom + dy, start.Top + minHeight);

            return new Box(left, top, right, bottom);
        }

        // Where a maximized window lands when restored mid-drag, keeping the cursor at the
        // same relative horizontal position on the title bar.
        public static Box Unmaximize(Box maximized, int restoredWidth, int restoredHeight, int cursorX)
        {
            double fraction = maximized.Width > 0 ? (cursorX - maximized.Left) / (double)maximized.Width : 0.5;
            int left = cursorX - (int)Math.Round(restoredWidth * fraction);
            return new Box(left, maximized.Top, left + restoredWidth, maximized.Top + restoredHeight);
        }
    }
}
