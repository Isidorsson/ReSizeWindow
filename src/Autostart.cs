using Microsoft.Win32;
using System.Windows.Forms;

namespace ReSizeWindow
{
    internal static class Autostart
    {
        private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";

        private static string Command
        {
            get { return "\"" + Application.ExecutablePath + "\""; }
        }

        public static bool IsEnabled()
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RunKey))
            {
                return key != null && Command.Equals(key.GetValue(Program.AppName));
            }
        }

        public static void SetEnabled(bool enabled)
        {
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(RunKey))
            {
                if (enabled) key.SetValue(Program.AppName, Command);
                else key.DeleteValue(Program.AppName, false);
            }
        }
    }
}
