using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Reflection;

namespace OcrSnap.Core
{
    /// <summary>
    /// 管理「隨系統啟動」Registry 設定。
    /// 讀寫 HKCU\Software\Microsoft\Windows\CurrentVersion\Run，key = OcrSnap。
    /// </summary>
    public static class StartupHelper
    {
        private const string RegKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
        private const string AppName = "OcrSnap";

        public static bool IsEnabled
        {
            get
            {
                using var key = Registry.CurrentUser.OpenSubKey(RegKey, writable: false);
                return key?.GetValue(AppName) is string path &&
                       path.Equals(ExePath, StringComparison.OrdinalIgnoreCase);
            }
        }

        public static void SetEnabled(bool enable)
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegKey, writable: true);
            if (key == null) return;
            if (enable)
                key.SetValue(AppName, ExePath);
            else
                key.DeleteValue(AppName, throwOnMissingValue: false);
        }

        private static string ExePath =>
            Process.GetCurrentProcess().MainModule?.FileName ?? Assembly.GetExecutingAssembly().Location;
    }
}
