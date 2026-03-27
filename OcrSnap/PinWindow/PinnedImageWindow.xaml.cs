using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using OcrSnap.Ocr;

namespace OcrSnap.PinWindow
{
    public partial class PinnedImageWindow : Window
    {
        private readonly BitmapSource _image;
        private bool _isDragging;
        private Point _dragStart;

        public PinnedImageWindow(BitmapSource image)
        {
            InitializeComponent();
            _image = image;
            PinImage.Source = image;

            Width = image.PixelWidth;
            Height = image.PixelHeight;

            Loaded += (_, _) => BuildContextMenu();
            MouseLeftButtonDown += OnMouseDown;
            MouseMove += OnMouseMove;
            MouseLeftButtonUp += OnMouseUp;
            MouseWheel += OnMouseWheel;
        }

        private void BuildContextMenu()
        {
            var menu = new System.Windows.Controls.ContextMenu();

            var menuOcr = new System.Windows.Controls.MenuItem { Header = "OCR 文字辨識" };
            menuOcr.Click += async (_, _) =>
            {
                var winOcr = App.Services.GetRequiredService<WindowsOcrService>();
                try
                {
                    var result = await winOcr.RecognizeAsync(_image, App.Settings.OcrWindowsLanguage);
                    var win = new OcrResultWindow(result, _image);
                    win.Show();
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
        }

        private void OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            Opacity = Math.Clamp(Opacity + e.Delta / 2000.0, 0.1, 1.0);
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
