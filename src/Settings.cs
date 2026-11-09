using Microsoft.Win32;

namespace ReSizeWindow
{
    internal static class Settings
    {
        private const string KeyPath = @"Software\" + Program.AppName;
        private const string BlockLeftWinName = "BlockLeftWin";
        private const string AutoTilingName = "AutoTiling";

        public static bool BlockLeftWin
        {
            get { return GetFlag(BlockLeftWinName); }
            set { SetFlag(BlockLeftWinName, value); }
        }

        public static bool AutoTiling
        {
            get { return GetFlag(AutoTilingName); }
            set { SetFlag(AutoTilingName, value); }
        }

        private static bool GetFlag(string name)
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(KeyPath))
            {
                return key != null && 1.Equals(key.GetValue(name));
            }
        }

        private static void SetFlag(string name, bool value)
        {
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(KeyPath))
            {
                key.SetValue(name, value ? 1 : 0, RegistryValueKind.DWord);
            }
        }
    }
}
