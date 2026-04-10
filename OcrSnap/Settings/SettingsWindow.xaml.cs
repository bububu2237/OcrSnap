using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using OcrSnap.Core;

namespace OcrSnap.Settings
{
    public partial class SettingsWindow : Window
    {
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int val, int size);

        public SettingsWindow()
        {
            InitializeComponent();
            SourceInitialized += (_, _) =>
            {
                var hwnd = new WindowInteropHelper(this).Handle;
                int dark = 1;
                DwmSetWindowAttribute(hwnd, 20, ref dark, sizeof(int));
            };
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            var s = App.Settings;

            // Hotkey modifiers
            ChkCtrl.IsChecked  = (s.HotkeyModifiers & NativeMethods.MOD_CONTROL) != 0;
            ChkAlt.IsChecked   = (s.HotkeyModifiers & NativeMethods.MOD_ALT)     != 0;
            ChkShift.IsChecked = (s.HotkeyModifiers & NativeMethods.MOD_SHIFT)   != 0;

            foreach (ComboBoxItem item in KeyBox.Items)
            {
                if ((uint)(int.Parse(item.Tag?.ToString() ?? "0")) == s.HotkeyKey)
                {
                    KeyBox.SelectedItem = item;
                    break;
                }
            }
            if (KeyBox.SelectedIndex < 0) KeyBox.SelectedIndex = 1;

            ChkRunAtStartup.IsChecked = StartupHelper.IsEnabled;
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            uint mods = 0;
            if (ChkCtrl.IsChecked  == true) mods |= NativeMethods.MOD_CONTROL;
            if (ChkAlt.IsChecked   == true) mods |= NativeMethods.MOD_ALT;
            if (ChkShift.IsChecked == true) mods |= NativeMethods.MOD_SHIFT;

            uint key = 0x71;
            if (KeyBox.SelectedItem is ComboBoxItem keyItem)
                key = (uint)int.Parse(keyItem.Tag?.ToString() ?? "113");

            bool runAtStartup = ChkRunAtStartup.IsChecked == true;

            var s = App.Settings;
            s.HotkeyModifiers = mods;
            s.HotkeyKey       = key;
            s.RunAtStartup    = runAtStartup;
            s.Save();

            // 同步寫入 / 移除 Registry 啟動項
            StartupHelper.SetEnabled(runAtStartup);

            DialogResult = true;
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
    }
}
