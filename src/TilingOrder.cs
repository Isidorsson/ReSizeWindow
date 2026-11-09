using System;
using System.Collections.Generic;

namespace ReSizeWindow
{
    // Tracked windows in insertion order, grouped per monitor. Handles are opaque keys.
    internal sealed class TilingOrder
    {
        private readonly Dictionary<IntPtr, List<IntPtr>> byMonitor = new Dictionary<IntPtr, List<IntPtr>>();
        private readonly Dictionary<IntPtr, IntPtr> monitorOf = new Dictionary<IntPtr, IntPtr>();

        public bool Contains(IntPtr window)
        {
            return monitorOf.ContainsKey(window);
        }

        public IntPtr MonitorOf(IntPtr window)
        {
            IntPtr monitor;
            if (!monitorOf.TryGetValue(window, out monitor)) throw new ArgumentException("Window is not tracked", "window");
            return monitor;
        }

        public List<IntPtr> Windows(IntPtr monitor)
        {
            List<IntPtr> windows;
            return byMonitor.TryGetValue(monitor, out windows) ? new List<IntPtr>(windows) : new List<IntPtr>();
        }

        public List<IntPtr> Monitors()
        {
            return new List<IntPtr>(byMonitor.Keys);
        }

        public List<IntPtr> All()
        {
            return new List<IntPtr>(monitorOf.Keys);
        }

        public void Add(IntPtr window, IntPtr monitor)
        {
            if (Contains(window)) throw new ArgumentException("Window is already tracked", "window");
            Append(window, monitor);
        }

        public void Remove(IntPtr window)
        {
            IntPtr monitor;
            if (!monitorOf.TryGetValue(window, out monitor)) return;

            List<IntPtr> windows = byMonitor[monitor];
            windows.Remove(window);
            if (windows.Count == 0) byMonitor.Remove(monitor);
            monitorOf.Remove(window);
        }

        public void MoveToMonitor(IntPtr window, IntPtr monitor)
        {
            MonitorOf(window);
            Remove(window);
            Append(window, monitor);
        }

        public void Swap(IntPtr a, IntPtr b)
        {
            IntPtr monitorA = MonitorOf(a);
            IntPtr monitorB = MonitorOf(b);
            List<IntPtr> listA = byMonitor[monitorA];
            List<IntPtr> listB = byMonitor[monitorB];
            int indexA = listA.IndexOf(a);
            int indexB = listB.IndexOf(b);

            listA[indexA] = b;
            listB[indexB] = a;
            monitorOf[a] = monitorB;
            monitorOf[b] = monitorA;
        }

        public void Clear()
        {
            byMonitor.Clear();
            monitorOf.Clear();
        }

        private void Append(IntPtr window, IntPtr monitor)
        {
            List<IntPtr> windows;
            if (!byMonitor.TryGetValue(monitor, out windows))
            {
                windows = new List<IntPtr>();
                byMonitor[monitor] = windows;
            }
            windows.Add(window);
            monitorOf[window] = monitor;
        }
    }
}
