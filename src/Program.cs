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
                using (var drag = new DragController(guard.Policy))
                {
                    guard.Policy.BlockLeftWin = Settings.BlockLeftWin;
                    using (new Tray(drag, guard.Policy)) Application.Run();
                }
            }
        }
    }
}
