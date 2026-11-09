using System;
using System.Collections.Generic;

namespace ReSizeWindow
{
    // Finds the windows that move together with a dragged one: everything transitively touching it.
    internal static class TouchGroup
    {
        public const int Tolerance = 2;

        // Returns the sorted indices of the candidates connected to start, not including start itself.
        public static int[] Find(Box start, Box[] candidates)
        {
            var members = new List<int>();
            var seen = new bool[candidates.Length];
            var frontier = new Queue<Box>();
            frontier.Enqueue(start);

            while (frontier.Count > 0)
            {
                Box current = frontier.Dequeue();
                for (int i = 0; i < candidates.Length; i++)
                {
                    if (seen[i] || !Touches(current, candidates[i])) continue;
                    seen[i] = true;
                    members.Add(i);
                    frontier.Enqueue(candidates[i]);
                }
            }

            members.Sort();
            return members.ToArray();
        }

        public static bool Touches(Box a, Box b)
        {
            bool sideBySide = Near(a.Right, b.Left) || Near(a.Left, b.Right);
            if (sideBySide && Overlap(a.Top, a.Bottom, b.Top, b.Bottom) > 0) return true;

            bool stacked = Near(a.Bottom, b.Top) || Near(a.Top, b.Bottom);
            return stacked && Overlap(a.Left, a.Right, b.Left, b.Right) > 0;
        }

        private static bool Near(int x, int y)
        {
            return Math.Abs(x - y) <= Tolerance;
        }

        private static int Overlap(int startA, int endA, int startB, int endB)
        {
            return Math.Min(endA, endB) - Math.Max(startA, startB);
        }
    }
}
