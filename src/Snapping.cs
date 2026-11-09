using System;
using System.Collections.Generic;

namespace ReSizeWindow
{
    internal struct SnapContext
    {
        public readonly Box[] Windows;
        public readonly Box[] WorkAreas;
        public readonly int Threshold, MinWidth, MinHeight;

        public SnapContext(Box[] windows, Box[] workAreas, int threshold, int minWidth, int minHeight)
        {
            Windows = windows;
            WorkAreas = workAreas;
            Threshold = threshold;
            MinWidth = minWidth;
            MinHeight = minHeight;
        }
    }

    internal enum Axis { Horizontal, Vertical }

    // Magnet snapping of a window's visible frame to other windows and work area edges.
    // Each axis is solved on its own, picking the smallest correction within the threshold.
    internal static class Snapping
    {
        public static Box SnapMove(Box proposed, SnapContext context)
        {
            int dx = MoveShift(proposed, context, Axis.Horizontal);
            int dy = MoveShift(proposed, context, Axis.Vertical);
            return WindowMath.Offset(proposed, dx, dy);
        }

        public static Box SnapResize(Box proposed, Edges edges, SnapContext context)
        {
            Box horizontal = ResizeAxis(proposed, edges, context, Axis.Horizontal);
            Box vertical = ResizeAxis(proposed, edges, context, Axis.Vertical);
            return new Box(horizontal.Left, vertical.Top, horizontal.Right, vertical.Bottom);
        }

        private static int MoveShift(Box box, SnapContext context, Axis axis)
        {
            int? toStart = Closest(Start(box, axis), StartTargets(box, context, axis), context.Threshold, int.MinValue, int.MaxValue);
            int? toEnd = Closest(End(box, axis), EndTargets(box, context, axis), context.Threshold, int.MinValue, int.MaxValue);

            if (!toStart.HasValue) return toEnd ?? 0;
            if (!toEnd.HasValue) return toStart.Value;
            return Math.Abs(toStart.Value) <= Math.Abs(toEnd.Value) ? toStart.Value : toEnd.Value;
        }

        private static Box ResizeAxis(Box box, Edges edges, SnapContext context, Axis axis)
        {
            int start = Start(box, axis);
            int end = End(box, axis);
            int min = MinSize(context, axis);

            if ((edges & StartEdge(axis)) != 0)
                start += Closest(start, StartTargets(box, context, axis), context.Threshold, int.MinValue, end - min) ?? 0;
            if ((edges & EndEdge(axis)) != 0)
                end += Closest(end, EndTargets(box, context, axis), context.Threshold, start + min, int.MaxValue) ?? 0;

            return WithRange(box, axis, start, end);
        }

        // A window only attracts when it overlaps (or nearly) on the other axis: its same edge aligns, its opposite edge attaches.
        private static List<int> StartTargets(Box box, SnapContext context, Axis axis)
        {
            var targets = new List<int>();
            foreach (Box window in context.Windows)
            {
                if (!Overlaps(box, window, context.Threshold, Other(axis))) continue;
                targets.Add(Start(window, axis));
                targets.Add(End(window, axis));
            }
            foreach (Box area in context.WorkAreas) targets.Add(Start(area, axis));
            return targets;
        }

        private static List<int> EndTargets(Box box, SnapContext context, Axis axis)
        {
            var targets = new List<int>();
            foreach (Box window in context.Windows)
            {
                if (!Overlaps(box, window, context.Threshold, Other(axis))) continue;
                targets.Add(End(window, axis));
                targets.Add(Start(window, axis));
            }
            foreach (Box area in context.WorkAreas) targets.Add(End(area, axis));
            return targets;
        }

        private static int? Closest(int edge, List<int> targets, int threshold, int min, int max)
        {
            int? best = null;
            foreach (int target in targets)
            {
                if (target < min || target > max) continue;
                int correction = target - edge;
                if (Math.Abs(correction) > threshold) continue;
                if (!best.HasValue || Math.Abs(correction) < Math.Abs(best.Value)) best = correction;
            }
            return best;
        }

        private static bool Overlaps(Box a, Box b, int threshold, Axis axis)
        {
            return Start(a, axis) < End(b, axis) + threshold && End(a, axis) > Start(b, axis) - threshold;
        }

        private static Axis Other(Axis axis)
        {
            return axis == Axis.Horizontal ? Axis.Vertical : Axis.Horizontal;
        }

        private static int Start(Box box, Axis axis)
        {
            return axis == Axis.Horizontal ? box.Left : box.Top;
        }

        private static int End(Box box, Axis axis)
        {
            return axis == Axis.Horizontal ? box.Right : box.Bottom;
        }

        private static Edges StartEdge(Axis axis)
        {
            return axis == Axis.Horizontal ? Edges.Left : Edges.Top;
        }

        private static Edges EndEdge(Axis axis)
        {
            return axis == Axis.Horizontal ? Edges.Right : Edges.Bottom;
        }

        private static int MinSize(SnapContext context, Axis axis)
        {
            return axis == Axis.Horizontal ? context.MinWidth : context.MinHeight;
        }

        private static Box WithRange(Box box, Axis axis, int start, int end)
        {
            return axis == Axis.Horizontal
                ? new Box(start, box.Top, end, box.Bottom)
                : new Box(box.Left, start, box.Right, end);
        }
    }
}
