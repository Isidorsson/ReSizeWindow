using System;
using System.Collections.Generic;

namespace ReSizeWindow
{
    internal struct TileArea
    {
        public readonly Box Area;
        public readonly int Gap;

        public TileArea(Box area, int gap)
        {
            Area = area;
            Gap = gap;
        }
    }

    internal static class TilingLayout
    {
        private const double EvenSplit = 0.5;
        private const double MinSplit = 0.1;
        private const double MaxSplit = 0.9;
        // Ignores the odd pixel a window snaps to on its own, e.g. from its minimum size.
        private const int EdgeTolerance = 2;

        private struct WindowEdit
        {
            public int Index;
            public Box Before, After;
            public int Gap;
        }

        // Dwindle BSP: the first window fills the area, each next window takes part of the previous
        // one's region, splitting its longer side. splits[i] is window i's share of that split, so
        // there is one split per window but the last. Gap surrounds and separates windows.
        public static List<Box> Dwindle(TileArea tiles, IList<double> splits)
        {
            List<Box> regions = Regions(tiles, splits);
            var boxes = new List<Box>();
            for (int i = 0; i < splits.Count; i++) boxes.Add(First(regions[i], splits[i], tiles.Gap));
            boxes.Add(regions[splits.Count]);
            return boxes;
        }

        // Keeps the stored splits that still apply; windows without one split evenly.
        public static List<double> FitSplits(IList<double> splits, int windowCount)
        {
            var fitted = new List<double>();
            for (int i = 0; i < windowCount - 1; i++) fitted.Add(i < splits.Count ? splits[i] : EvenSplit);
            return fitted;
        }

        // Moves the split lines a resized window touches to its new edges, so its neighbours give or
        // take the difference. Edges on the work area's border have no split and stay put.
        public static List<double> ResizeSplits(TileArea tiles, IList<double> splits, int index, Box frame)
        {
            List<Box> oldRegions = Regions(tiles, splits);
            var edit = new WindowEdit { Index = index, Before = Dwindle(tiles, splits)[index], After = frame, Gap = tiles.Gap };
            var result = new List<double>(splits);
            Box region = oldRegions[0];

            for (int j = 0; j < result.Count; j++)
            {
                Axis axis = SplitAxis(oldRegions[j]);
                int? lineEnd = MovedLine(edit, j, axis, oldRegions[j + 1]);
                if (lineEnd.HasValue && SplitAxis(region) == axis)
                    result[j] = Clamp((lineEnd.Value - Start(region, axis)) / (double)(Length(region, axis) - tiles.Gap));
                region = Second(region, result[j], tiles.Gap);
            }
            return result;
        }

        // Windows have invisible resize borders. Grow the target by the window's current border
        // so that its visible edges, not its window rect, land on the target.
        public static Box Compensate(Box target, Box visible, Box window)
        {
            return new Box(
                target.Left - (visible.Left - window.Left),
                target.Top - (visible.Top - window.Top),
                target.Right + (window.Right - visible.Right),
                target.Bottom + (window.Bottom - visible.Bottom));
        }

        // Where split j's first part should now end, if the window moved an edge lying on that split.
        private static int? MovedLine(WindowEdit edit, int j, Axis axis, Box oldSecond)
        {
            if (j == edit.Index)
                return Moved(End(edit.Before, axis), End(edit.After, axis)) ? End(edit.After, axis) : (int?)null;
            bool onLine = j < edit.Index && Start(edit.Before, axis) == Start(oldSecond, axis);
            return onLine && Moved(Start(edit.Before, axis), Start(edit.After, axis)) ? Start(edit.After, axis) - edit.Gap : (int?)null;
        }

        // regions[i] is the region split for window i; the last one is the last window's box.
        private static List<Box> Regions(TileArea tiles, IList<double> splits)
        {
            Box area = tiles.Area;
            int gap = tiles.Gap;
            var regions = new List<Box> { new Box(area.Left + gap, area.Top + gap, area.Right - gap, area.Bottom - gap) };
            foreach (double split in splits) regions.Add(Second(regions[regions.Count - 1], split, gap));
            return regions;
        }

        private static Box First(Box region, double split, int gap)
        {
            Axis axis = SplitAxis(region);
            return WithRange(region, axis, Start(region, axis), Start(region, axis) + FirstLength(region, split, gap));
        }

        private static Box Second(Box region, double split, int gap)
        {
            Axis axis = SplitAxis(region);
            int start = Math.Min(End(region, axis), Start(region, axis) + FirstLength(region, split, gap) + gap);
            return WithRange(region, axis, start, End(region, axis));
        }

        // The epsilon keeps a split computed from a pixel position from flooring one pixel short.
        private static int FirstLength(Box region, double split, int gap)
        {
            int available = Math.Max(0, Length(region, SplitAxis(region)) - gap);
            return (int)Math.Floor(available * split + 1e-9);
        }

        private static Axis SplitAxis(Box region)
        {
            return region.Width >= region.Height ? Axis.Horizontal : Axis.Vertical;
        }

        private static bool Moved(int from, int to)
        {
            return Math.Abs(to - from) > EdgeTolerance;
        }

        private static double Clamp(double split)
        {
            return Math.Max(MinSplit, Math.Min(MaxSplit, split));
        }

        private static int Start(Box box, Axis axis)
        {
            return axis == Axis.Horizontal ? box.Left : box.Top;
        }

        private static int End(Box box, Axis axis)
        {
            return axis == Axis.Horizontal ? box.Right : box.Bottom;
        }

        private static int Length(Box box, Axis axis)
        {
            return End(box, axis) - Start(box, axis);
        }

        private static Box WithRange(Box box, Axis axis, int start, int end)
        {
            return axis == Axis.Horizontal
                ? new Box(start, box.Top, end, box.Bottom)
                : new Box(box.Left, start, box.Right, end);
        }
    }
}
