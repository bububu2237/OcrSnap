using System;
using System.Windows;
using Hardcodet.Wpf.TaskbarNotification;
using Microsoft.Extensions.DependencyInjection;
using OcrSnap.Core;
using OcrSnap.Ocr;
using OcrSnap.Screenshot;
using OcrSnap.Settings;

namespace OcrSnap
{
    public partial class App : Application
    {
        public static IServiceProvider Services { get; private set; } = null!;
        public static AppSettings Settings { get; private set; } = null!;

        private GlobalHotkey _hotkey = null!;
        private TaskbarIcon _notifyIcon = null!;
        private Window _hostWindow = null!;
        private int _captureHotkeyId = -1;
        private System.Windows.Controls.MenuItem _menuCapture = null!;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            Settings = AppSettings.Load();

            // 建立 DI 容器
            var services = new ServiceCollection();
            services.AddSingleton<WindowsOcrService>();
            Services = services.BuildServiceProvider();

            // 建立隱藏宿主視窗（提供 HWND 給熱鍵）
            _hostWindow = new Window
            {
                Width = 0, Height = 0,
                WindowStyle = WindowStyle.None,
                AllowsTransparency = true,
                Background = System.Windows.Media.Brushes.Transparent,
                ShowInTaskbar = false,
                IsHitTestVisible = false,
                Left = -9999, Top = -9999
            };
            _hostWindow.Show();
            _hostWindow.Hide();

            // 初始化全域熱鍵
            _hotkey = new GlobalHotkey();
            _hotkey.Initialize(_hostWindow);
            RegisterCaptureHotkey();

            // 建立系統匣圖示
            _notifyIcon = new TaskbarIcon
            {
                IconSource = new System.Windows.Media.Imaging.BitmapImage(
                    new Uri("pack://application:,,,/Resources/Icons/app.ico")),
                ToolTipText = "OcrSnap"
            };

            var menu = new System.Windows.Controls.ContextMenu();

            _menuCapture = new System.Windows.Controls.MenuItem { Header = "截圖 (" + HotkeyDisplayString() + ")" };
            _menuCapture.Click += (_, _) => StartCapture();
            menu.Items.Add(_menuCapture);

            menu.Items.Add(new System.Windows.Controls.Separator());

            var menuSettings = new System.Windows.Controls.MenuItem { Header = "設定" };
            menuSettings.Click += MenuSettings_Click;
            menu.Items.Add(menuSettings);

            menu.Items.Add(new System.Windows.Controls.Separator());

            var menuExit = new System.Windows.Controls.MenuItem { Header = "結束" };
            menuExit.Click += (_, _) => ExitApp();
            menu.Items.Add(menuExit);

            _notifyIcon.ContextMenu = menu;
        }

        private void RegisterCaptureHotkey()
        {
            if (_captureHotkeyId >= 0)
                _hotkey.Unregister(_captureHotkeyId);

            _captureHotkeyId = _hotkey.Register(Settings.HotkeyModifiers, Settings.HotkeyKey, StartCapture);
        }

        public static void StartCapture()
        {
            var overlay = new OverlayWindow();
            overlay.Show();
        }

        private void MenuSettings_Click(object sender, RoutedEventArgs e)
        {
            var win = new SettingsWindow();
            if (win.ShowDialog() == true)
            {
                Settings = AppSettings.Load();
                RegisterCaptureHotkey();
                _menuCapture.Header = "截圖 (" + HotkeyDisplayString() + ")";
            }
        }

        private string HotkeyDisplayString()
        {
            var parts = new System.Collections.Generic.List<string>();
            if ((Settings.HotkeyModifiers & OcrSnap.Core.NativeMethods.MOD_CONTROL) != 0) parts.Add("Ctrl");
            if ((Settings.HotkeyModifiers & OcrSnap.Core.NativeMethods.MOD_ALT) != 0) parts.Add("Alt");
            if ((Settings.HotkeyModifiers & OcrSnap.Core.NativeMethods.MOD_SHIFT) != 0) parts.Add("Shift");
            parts.Add(KeyName(Settings.HotkeyKey));
            return string.Join("+", parts);
        }

        private static string KeyName(uint vk) => vk switch
        {
            >= 0x70 and <= 0x87 => "F" + (vk - 0x6F),
            >= 0x30 and <= 0x39 => ((char)vk).ToString(),
            >= 0x41 and <= 0x5A => ((char)vk).ToString(),
            _ => "0x" + vk.ToString("X2")
        };

        private void ExitApp()
        {
            _hotkey.Dispose();
            _notifyIcon.Dispose();
            Shutdown();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _notifyIcon?.Dispose();
            _hotkey?.Dispose();
            base.OnExit(e);
        }
    }
}
