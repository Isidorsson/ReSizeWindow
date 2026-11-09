using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace ReSizeWindow
{
    // Window lifecycle events, delivered on the thread that calls Start (needs its message loop).
    internal sealed class WinEventHooks : IDisposable
    {
        private static readonly uint[][] EventRanges =
        {
            new[] { Native.EVENT_SYSTEM_MOVESIZESTART, Native.EVENT_SYSTEM_MOVESIZEEND },
            new[] { Native.EVENT_SYSTEM_MINIMIZESTART, Native.EVENT_SYSTEM_MINIMIZEEND },
            new[] { Native.EVENT_OBJECT_DESTROY, Native.EVENT_OBJECT_HIDE },
            new[] { Native.EVENT_OBJECT_LOCATIONCHANGE, Native.EVENT_OBJECT_LOCATIONCHANGE },
            new[] { Native.EVENT_OBJECT_CLOAKED, Native.EVENT_OBJECT_UNCLOAKED }
        };

        private readonly Native.WinEventProc callback;
        private readonly Action<uint, IntPtr> handler;
        private readonly List<IntPtr> hooks = new List<IntPtr>();

        public WinEventHooks(Action<uint, IntPtr> handler)
        {
            this.handler = handler;
            callback = OnEvent;
        }

        public void Start()
        {
            if (hooks.Count > 0) return;

            foreach (uint[] range in EventRanges)
            {
                IntPtr hook = Native.SetWinEventHook(range[0], range[1], IntPtr.Zero, callback, 0, 0,
                    Native.WINEVENT_OUTOFCONTEXT | Native.WINEVENT_SKIPOWNPROCESS);
                if (hook == IntPtr.Zero)
                {
                    int error = Marshal.GetLastWin32Error();
                    Stop();
                    throw new Win32Exception(error, "Failed to install window event hook");
                }
                hooks.Add(hook);
            }
        }

        public void Stop()
        {
            foreach (IntPtr hook in hooks) Native.UnhookWinEvent(hook);
            hooks.Clear();
        }

        public void Dispose()
        {
            Stop();
        }

        private void OnEvent(IntPtr hook, uint eventType, IntPtr window, int idObject, int idChild, uint thread, uint time)
        {
            if (idObject != Native.OBJID_WINDOW || idChild != Native.CHILDID_SELF || window == IntPtr.Zero) return;
            handler(eventType, window);
        }
    }
}
