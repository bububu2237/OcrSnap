using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.Extensions.DependencyInjection;

namespace OcrSnap.Ocr
{
    public partial class OcrResultWindow : Window
    {
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int val, int size);

        private OcrResult _result;
        private readonly BitmapSource _image;
        private readonly List<UIElement> _boxElements = new();
        private bool _showBoxes;

        // 手動圈選辨識
        private Point _selStart;
        private bool _isSelecting;
        private Rectangle? _selRect;

        public OcrResultWindow(OcrResult result, BitmapSource image)
        {
            InitializeComponent();
            _result = result;
            _image = image;

            SourceInitialized += (_, _) => SetDarkTitleBar();
            Loaded += OnLoaded;

            PreviewCanvas.MouseLeftButtonDown += PreviewCanvas_MouseDown;
            PreviewCanvas.MouseMove += PreviewCanvas_MouseMove;
            PreviewCanvas.MouseLeftButtonUp += PreviewCanvas_MouseUp;
        }

        private void SetDarkTitleBar()
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            int dark = 1;
            DwmSetWindowAttribute(hwnd, 20, ref dark, sizeof(int));
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // 選取目前語言
            string currentLang = App.Settings.OcrWindowsLanguage;
            foreach (ComboBoxItem item in ModelBox.Items)
            {
                if (item.Tag?.ToString() == currentLang)
                {
                    ModelBox.SelectedItem = item;
                    break;
                }
            }
            if (ModelBox.SelectedIndex < 0) ModelBox.SelectedIndex = 0;

            SetupPreviewImage();
            DisplayResult(_result);
        }

        private void SetupPreviewImage()
        {
            PreviewCanvas.Width = _image.PixelWidth;
            PreviewCanvas.Height = _image.PixelHeight;
            PreviewCanvas.Children.Add(new System.Windows.Controls.Image
            {
                Source = _image,
                Stretch = Stretch.None
            });
        }

        private void DisplayResult(OcrResult result)
        {
            foreach (var elem in _boxElements)
                PreviewCanvas.Children.Remove(elem);
            _boxElements.Clear();

            ResultText.Text = !string.IsNullOrWhiteSpace(result.Markdown)
                ? result.Markdown
                : string.Join("\n", System.Linq.Enumerable.Select(result.Regions, r => r.Text));

            TimeLabel.Text = result.ProcessTimeMs + " ms";

            foreach (var region in result.Regions)
            {
                var bb = region.BoundingBox;
                if (bb == null) continue;
                var rect = new Rectangle
                {
                    Width = bb.Width, Height = bb.Height,
                    Stroke = Brushes.Red, StrokeThickness = 1.5,
                    Fill = new SolidColorBrush(Color.FromArgb(20, 255, 0, 0)),
                    Visibility = _showBoxes ? Visibility.Visible : Visibility.Collapsed,
                    ToolTip = region.Text
                };
                Canvas.SetLeft(rect, bb.X);
                Canvas.SetTop(rect, bb.Y);
                PreviewCanvas.Children.Add(rect);
                _boxElements.Add(rect);
            }
        }

        private void PreviewCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _selStart = e.GetPosition(PreviewCanvas);
            _isSelecting = true;

            _selRect = new Rectangle
            {
                Stroke = Brushes.Cyan,
                StrokeThickness = 1.5,
                StrokeDashArray = new DoubleCollection { 4, 2 },
                Fill = new SolidColorBrush(Color.FromArgb(30, 0, 255, 255))
            };
            Canvas.SetLeft(_selRect, _selStart.X);
            Canvas.SetTop(_selRect, _selStart.Y);
            PreviewCanvas.Children.Add(_selRect);
            PreviewCanvas.CaptureMouse();
        }

        private void PreviewCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isSelecting || _selRect == null) return;
            var pos = e.GetPosition(PreviewCanvas);
            double x = Math.Min(pos.X, _selStart.X);
            double y = Math.Min(pos.Y, _selStart.Y);
            double w = Math.Abs(pos.X - _selStart.X);
            double h = Math.Abs(pos.Y - _selStart.Y);
            Canvas.SetLeft(_selRect, x);
            Canvas.SetTop(_selRect, y);
            _selRect.Width = w;
            _selRect.Height = h;
        }

        private async void PreviewCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (!_isSelecting || _selRect == null) return;
            _isSelecting = false;
            PreviewCanvas.ReleaseMouseCapture();

            PreviewCanvas.Children.Remove(_selRect);
            double rx = Canvas.GetLeft(_selRect);
            double ry = Canvas.GetTop(_selRect);
            double rw = _selRect.Width;
            double rh = _selRect.Height;
            _selRect = null;

            if (rw < 4 || rh < 4) return;

            int px = Math.Max(0, (int)rx);
            int py = Math.Max(0, (int)ry);
            int pw = Math.Min((int)rw, _image.PixelWidth - px);
            int ph = Math.Min((int)rh, _image.PixelHeight - py);
            if (pw <= 0 || ph <= 0) return;

            var crop = new CroppedBitmap(_image, new Int32Rect(px, py, pw, ph));

            BtnReOcr.IsEnabled = false;
            ReOcrLabel.Visibility = Visibility.Collapsed;
            ReOcrSpinner.Visibility = Visibility.Visible;
            try
            {
                string bcp47 = (ModelBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "zh-Hant";
                var winOcr = App.Services.GetRequiredService<WindowsOcrService>();
                var cropResult = await winOcr.RecognizeAsync(crop, bcp47);
                ResultText.Text = !string.IsNullOrWhiteSpace(cropResult.Markdown)
                    ? cropResult.Markdown
                    : string.Join("\n", cropResult.Regions.Select(r => r.Text));
                TimeLabel.Text = cropResult.ProcessTimeMs + " ms";
            }
            catch (Exception ex)
            {
                MessageBox.Show("OCR failed: " + ex.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                BtnReOcr.IsEnabled = true;
                ReOcrLabel.Visibility = Visibility.Visible;
                ReOcrSpinner.Visibility = Visibility.Collapsed;
            }
        }

        private void BtnCopyAll_Click(object sender, RoutedEventArgs e)
        {
            ResultText.SelectAll();
            Clipboard.SetText(ResultText.Text);
        }

        private void BtnShowBoxes_Click(object sender, RoutedEventArgs e)
        {
            _showBoxes = BtnShowBoxes.IsChecked == true;
            foreach (var elem in _boxElements)
                elem.Visibility = _showBoxes ? Visibility.Visible : Visibility.Collapsed;
        }

        private async void BtnReOcr_Click(object sender, RoutedEventArgs e)
        {
            BtnReOcr.IsEnabled = false;
            ReOcrLabel.Visibility = Visibility.Collapsed;
            ReOcrSpinner.Visibility = Visibility.Visible;
            try
            {
                string bcp47 = (ModelBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "zh-Hant";
                var winOcr = App.Services.GetRequiredService<WindowsOcrService>();
                _result = await winOcr.RecognizeAsync(_image, bcp47);
                // 成功才儲存語言偏好
                App.Settings.OcrWindowsLanguage = bcp47;
                App.Settings.Save();
                DisplayResult(_result);
            }
            catch (Exception ex)
            {
                MessageBox.Show("OCR failed: " + ex.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                BtnReOcr.IsEnabled = true;
                ReOcrLabel.Visibility = Visibility.Visible;
                ReOcrSpinner.Visibility = Visibility.Collapsed;
            }
        }
    }
}