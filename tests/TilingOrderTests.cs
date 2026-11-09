using System;
using System.Collections.Generic;

namespace ReSizeWindow.Tests
{
    internal static class TilingOrderTests
    {
        private static readonly IntPtr A = new IntPtr(1);
        private static readonly IntPtr B = new IntPtr(2);
        private static readonly IntPtr C = new IntPtr(3);
        private static readonly IntPtr D = new IntPtr(4);
        private static readonly IntPtr Left = new IntPtr(100);
        private static readonly IntPtr Right = new IntPtr(200);

        public static void Run()
        {
            Add_KeepsInsertionOrderPerMonitor();
            Add_Duplicate_Throws();
            Remove_DropsWindowAndEmptyMonitor();
            Remove_Untracked_IsNoOp();
            Swap_SameMonitor_ExchangesPositions();
            Swap_AcrossMonitors_ExchangesMonitors();
            MoveToMonitor_AppendsToEnd();
            MoveToMonitor_NewMonitor_CreatesList();
            MoveToMonitor_LastWindow_EmptiesSourceMonitor();
            Clear_ForgetsEverything();
        }

        private static TilingOrder Sample()
        {
            var order = new TilingOrder();
            order.Add(A, Left);
            order.Add(B, Left);
            order.Add(C, Right);
            return order;
        }

        private static string Ids(List<IntPtr> windows)
        {
            var parts = new List<string>();
            foreach (IntPtr w in windows) parts.Add(w.ToString());
            return string.Join(",", parts.ToArray());
        }

        private static void Add_KeepsInsertionOrderPerMonitor()
        {
            TilingOrder order = Sample();
            AssertEqual("left order", "1,2", Ids(order.Windows(Left)));
            AssertEqual("right order", "3", Ids(order.Windows(Right)));
            AssertEqual("monitor of", Right, order.MonitorOf(C));
            AssertEqual("unknown monitor", 0, order.Windows(new IntPtr(999)).Count);
        }

        private static void Add_Duplicate_Throws()
        {
            TilingOrder order = Sample();
            bool threw = false;
            try { order.Add(A, Right); }
            catch (ArgumentException) { threw = true; }
            AssertEqual("duplicate add", true, threw);
        }

        private static void Remove_DropsWindowAndEmptyMonitor()
        {
            TilingOrder order = Sample();
            order.Remove(A);
            AssertEqual("after remove", "2", Ids(order.Windows(Left)));
            AssertEqual("not contained", false, order.Contains(A));

            order.Remove(C);
            AssertEqual("empty monitor gone", 1, order.Monitors().Count);
        }

        private static void Remove_Untracked_IsNoOp()
        {
            TilingOrder order = Sample();
            order.Remove(D);
            AssertEqual("still three", 3, order.All().Count);
        }

        private static void Swap_SameMonitor_ExchangesPositions()
        {
            TilingOrder order = Sample();
            order.Swap(A, B);
            AssertEqual("swapped", "2,1", Ids(order.Windows(Left)));
        }

        private static void Swap_AcrossMonitors_ExchangesMonitors()
        {
            TilingOrder order = Sample();
            order.Swap(A, C);
            AssertEqual("left after", "3,2", Ids(order.Windows(Left)));
            AssertEqual("right after", "1", Ids(order.Windows(Right)));
            AssertEqual("a moved", Right, order.MonitorOf(A));
            AssertEqual("c moved", Left, order.MonitorOf(C));
        }

        private static void MoveToMonitor_AppendsToEnd()
        {
            TilingOrder order = Sample();
            order.MoveToMonitor(A, Right);
            AssertEqual("source", "2", Ids(order.Windows(Left)));
            AssertEqual("target", "3,1", Ids(order.Windows(Right)));
        }

        private static void MoveToMonitor_NewMonitor_CreatesList()
        {
            TilingOrder order = Sample();
            var third = new IntPtr(300);
            order.MoveToMonitor(B, third);
            AssertEqual("new monitor", "2", Ids(order.Windows(third)));
            AssertEqual("monitor count", 3, order.Monitors().Count);
        }

        private static void MoveToMonitor_LastWindow_EmptiesSourceMonitor()
        {
            TilingOrder order = Sample();
            order.MoveToMonitor(C, Left);
            AssertEqual("right gone", 1, order.Monitors().Count);
            AssertEqual("left order", "1,2,3", Ids(order.Windows(Left)));
        }

        private static void Clear_ForgetsEverything()
        {
            TilingOrder order = Sample();
            order.Clear();
            AssertEqual("cleared windows", 0, order.All().Count);
            AssertEqual("cleared monitors", 0, order.Monitors().Count);
        }

        private static void AssertEqual<T>(string name, T expected, T actual)
        {
            TestRunner.AssertEqual(name, expected, actual);
        }
    }
}
