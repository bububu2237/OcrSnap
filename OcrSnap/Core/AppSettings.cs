using System;
using System.IO;
using System.Text.Json;

namespace OcrSnap.Core
{
    public class AppSettings
    {
        public uint HotkeyModifiers { get; set; } = 0;          // 0 = ?∩耨憌暸
        public uint HotkeyKey { get; set; } = 0x73;             // F4 = 0x73
        public string OcrWindowsLanguage { get; set; } = "zh-Hant";
        public bool ColorDisplayHex { get; set; } = false;       // false=RGB, true=HEX

        private static readonly string SettingsPath =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                         "OcrSnap", "settings.json");

        public static AppSettings Load()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    var json = File.ReadAllText(SettingsPath);
                    return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                }
            }
            catch { }
            return new AppSettings();
        }

        public void Save()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
                var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SettingsPath, json);
            }
            catch { }
        }
    }
}
