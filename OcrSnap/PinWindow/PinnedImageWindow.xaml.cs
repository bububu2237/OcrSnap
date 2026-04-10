using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using OcrSnap.Core;
using OcrSnap.Ocr;

namespace OcrSnap.PinWindow
{
    public partial class PinnedImageWindow : Window
    {
        private readonly BitmapSource _image;
        private string? _pinId;   // null = 尚未存入 session（還原視窗時已有 id）
        private bool _isDragging;
        private Point _dragStart;

        /// <summary>使用者釘選新截圖時呼叫；自動加入 PinSession。</summary>
        public PinnedImageWindow(BitmapSource image)
        {
            InitializeComponent();
            _image = image;
            PinImage.Source = image;
            Width = image.PixelWidth;
            Height = image.PixelHeight;
            Init();
        }

        /// <summary>App 啟動還原已存的釘選時呼叫；不重複寫 PinSession。</summary>
        public PinnedImageWindow(BitmapSource image, PinEntry entry)
        {
            InitializeComponent();
            _image = image;
            _pinId = entry.Id;
            PinImage.Source = image;
            Width = entry.Width;
            Height = entry.Height;
            Left = entry.Left;
            Top = entry.Top;
            Opacity = entry.Opacity;
            Init();
        }

        private void Init()
        {
            Loaded += OnLoaded;
            Closed += OnClosed;
            MouseLeftButtonDown += OnMouseDown;
            MouseMove += OnMouseMove;
            MouseLeftButtonUp += OnMouseUp;
            MouseWheel += OnMouseWheel;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // 新釘選：存入 session（位置在 Loaded 後才確定）
            if (_pinId == null)
                _pinId = PinSession.AddPin(_image, Left, Top, Width, Height);

            BuildContextMenu();
        }

        private void OnClosed(object? sender, EventArgs e)
        {
            if (_pinId != null)
                PinSession.RemovePin(_pinId);
            PinImage.Source = null;
            App.TrimMemory();
        }

        private void BuildContextMenu()
        {
            var menu = new System.Windows.Controls.ContextMenu();

            var menuOcr = new System.Windows.Controls.MenuItem { Header = "OCR 文字辨識" };
            menuOcr.Click += async (_, _) =>
            {
                var winOcr = App.OcrService;
                try
                {
                    var result = await winOcr.RecognizeAsync(_image, App.Settings.OcrWindowsLanguage);
                    new OcrResultWindow(result, _image).Show();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"OCR 失敗：{ex.Message}", "錯誤");
                }
            };
            menu.Items.Add(menuOcr);

            menu.Items.Add(new System.Windows.Controls.Separator());

            var menuCopy = new System.Windows.Controls.MenuItem { Header = "複製圖片" };
            menuCopy.Click += (_, _) => Clipboard.SetImage(_image);
            menu.Items.Add(menuCopy);

            var menuSave = new System.Windows.Controls.MenuItem { Header = "儲存圖片..." };
            menuSave.Click += (_, _) => SaveImage();
            menu.Items.Add(menuSave);

            menu.Items.Add(new System.Windows.Controls.Separator());

            var menuClose = new System.Windows.Controls.MenuItem { Header = "關閉" };
            menuClose.Click += (_, _) => Close();
            menu.Items.Add(menuClose);

            ContextMenu = menu;
        }

        private void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            _isDragging = true;
            _dragStart = e.GetPosition(this);
            CaptureMouse();
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (!_isDragging) return;
            var p = e.GetPosition(this);
            Left += p.X - _dragStart.X;
            Top += p.Y - _dragStart.Y;
        }

        private void OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            _isDragging = false;
            ReleaseMouseCapture();
            // 拖曳結束後更新持久化位置
            if (_pinId != null)
                PinSession.UpdatePin(_pinId, Left, Top, Opacity);
        }

        private void OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            Opacity = Math.Clamp(Opacity + e.Delta / 2000.0, 0.1, 1.0);
            if (_pinId != null)
                PinSession.UpdatePin(_pinId, Left, Top, Opacity);
        }

        private void SaveImage()
        {
            var dlg = new SaveFileDialog
            {
                Filter = "PNG 圖片|*.png|JPEG 圖片|*.jpg",
                FileName = $"snipaste_{DateTime.Now:yyyyMMdd_HHmmss}"
            };
            if (dlg.ShowDialog() != true) return;

            BitmapEncoder encoder = dlg.FilterIndex == 2
                ? new JpegBitmapEncoder()
                : new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(_image));
            using var fs = File.OpenWrite(dlg.FileName);
            encoder.Save(fs);
        }
    }
}
