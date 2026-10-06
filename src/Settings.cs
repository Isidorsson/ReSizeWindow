using Microsoft.Win32;

namespace ReSizeWindow
{
    internal static class Settings
    {
        private const string KeyPath = @"Software\" + Program.AppName;
        private const string BlockLeftWinName = "BlockLeftWin";

        public static bool BlockLeftWin
        {
            get
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(KeyPath))
                {
                    return key != null && 1.Equals(key.GetValue(BlockLeftWinName));
                }
            }
            set
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(KeyPath))
                {
                    key.SetValue(BlockLeftWinName, value ? 1 : 0, RegistryValueKind.DWord);
                }
            }
        }
    }
}
