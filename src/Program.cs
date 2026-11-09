using System;
using System.Threading;
using System.Windows.Forms;

namespace ReSizeWindow
{
    internal static class Program
    {
        public const string AppName = "ReSizeWindow";

        [STAThread]
        private static void Main()
        {
            bool createdNew;
            using (new Mutex(true, @"Local\" + AppName, out createdNew))
            {
                if (!createdNew) return;

                Native.SetProcessDpiAwarenessContext(Native.DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2);
                Application.EnableVisualStyles();

                using (var guard = new StartMenuGuard())
                using (var tiler = new Tiler())
                using (var drag = new DragController(guard.Policy, tiler))
                using (var alwaysOnTop = new AlwaysOnTop())
                {
                    guard.Policy.BlockLeftWin = Settings.BlockLeftWin;
                    alwaysOnTop.Toggled += tiler.Refresh;
                    using (new Tray(drag, guard.Policy, alwaysOnTop, tiler)) Application.Run();
                }
            }
        }
    }
}
