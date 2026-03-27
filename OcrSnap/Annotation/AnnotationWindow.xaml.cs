using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.Extensions.DependencyInjection;
using OcrSnap.Annotation.Tools;
using OcrSnap.Ocr;
using OcrSnap.PinWindow;

namespace OcrSnap.Annotation
{
    public partial class AnnotationWindow : Window
    {
        private readonly BitmapSource _original;
        private readonly Rect _screenRect;

        private IAnnotationTool _currentTool;
        private Color _currentColor = Colors.Red;
        private double _currentStroke = 2;

        private readonly Stack<UIElement> _undoStack = new();
        private UIElement? _previewElement;
        private bool _isDraggingWindow;
        private Point _dragStart;

        // 調色盤顏色列表
        private static readonly string[] PaletteHex =
        [
            "#F44336","#E91E63","#9C27B0","#673AB7","#3F51B5",
            "#2196F3","#03A9F4","#00BCD4","#009688","#4CAF50",
            "#8BC34A","#FFEB3B","#FFC107","#FF9800","#FF5722",
            "#795548","#9E9E9E","#607D8B","#FFFFFF","#000000"
        ];
        private Border? _selectedSwatch;

        public AnnotationWindow(BitmapSource captured, Rect screenRect)
        {
            InitializeComponent();
            _original = captured;
            _screenRect = screenRect;
            _currentTool = new RectangleTool();

            SourceInitialized += OnSourceInitialized;
            Loaded += (_, _) => BuildColorPalette();
            KeyDown += OnKeyDown;
            MouseLeftButtonDown += OnWindowMouseDown;
            MouseMove += OnWindowMouseMove;
            MouseLeftButtonUp += OnWindowMouseUp;

            DrawCanvas.MouseLeftButtonDown += OnCanvasMouseDown;
            DrawCanvas.MouseMove += OnCanvasMouseMove;
            DrawCanvas.MouseLeftButtonUp += OnCanvasMouseUp;
        }

        private void OnSourceInitialized(object? sender, EventArgs e)
        {
            CapturedImage.Source = _original;

            double dpiScale = GetDpiScale();
            double imgW = _original.PixelWidth / dpiScale;
            double imgH = _original.PixelHeight / dpiScale;
            Width = Math.Max(imgW, 480);
            // Height auto-calculated via SizeToContent="Height"

            DrawCanvas.Width = imgW;
            DrawCanvas.Height = imgH;
            ImageBorder.Width = imgW;
            ImageBorder.Height = imgH;
            ImageGrid.Width = imgW;
            ImageGrid.Height = imgH;

            Left = _screenRect.Left;
            Top = _screenRect.Top;
            BtnRect.IsChecked = true;
        }

        // ── 調色盤 ────────────────────────────────────────────────

        private void BuildColorPalette()
        {
            foreach (var hex in PaletteHex)
            {
                var color = (Color)ColorConverter.ConvertFromString(hex);
                var swatch = new Border
                {
                    Width = 18, Height = 18,
                    Margin = new Thickness(2, 1, 2, 1),
                    Background = new SolidColorBrush(color),
                    CornerRadius = new CornerRadius(3),
                    BorderThickness = new Thickness(0),
                    Cursor = Cursors.Hand,
                    ToolTip = hex
                };
                var c = color;
                var s = swatch;
                swatch.MouseLeftButtonUp += (_, _) => SelectColor(c, s);
                ColorPalette.Children.Add(swatch);
            }

            // 預設選紅色
            if (ColorPalette.Children.Count > 0)
                SelectColor(Colors.Red, (Border)ColorPalette.Children[0]);
        }

        private void SelectColor(Color color, Border swatch)
        {
            _currentColor = color;
            CurrentColorSwatch.Background = new SolidColorBrush(color);

            if (_selectedSwatch != null)
            {
                _selectedSwatch.BorderThickness = new Thickness(0);
                _selectedSwatch.Margin = new Thickness(2, 1, 2, 1);
            }
            _selectedSwatch = swatch;
            swatch.BorderBrush = Brushes.White;
            swatch.BorderThickness = new Thickness(2);
            swatch.Margin = new Thickness(0, 0, 0, 0);
        }

        // ── 視窗拖曳 ──────────────────────────────────────────────

        private void OnWindowMouseDown(object sender, MouseButtonEventArgs e)
        {
            var src = e.OriginalSource as DependencyObject;
            if (IsInteractiveControl(src)) return;

            if (e.Source == Toolbar || IsDescendant(Toolbar, src))
            {
                _isDraggingWindow = true;
                _dragStart = e.GetPosition(this);
                CaptureMouse();
                e.Handled = true;
            }
        }

        private static bool IsInteractiveControl(DependencyObject? element)
        {
            var current = element;
            while (current != null)
            {
                if (current is Button or ToggleButton or ComboBox or ComboBoxItem or TextBox)
                    return true;
                if (current is Border b && b.Name == "CurrentColorSwatch")
                    return true;
                if (current is System.Windows.Controls.WrapPanel wp && wp.Name == "ColorPalette")
                    return true;
                current = VisualTreeHelper.GetParent(current);
            }
            return false;
        }

        private void OnWindowMouseMove(object sender, MouseEventArgs e)
        {
            if (_isDraggingWindow)
            {
                var p = e.GetPosition(this);
                Left += p.X - _dragStart.X;
                Top += p.Y - _dragStart.Y;
            }
        }

        private void OnWindowMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDraggingWindow)
            {
                _isDraggingWindow = false;
                ReleaseMouseCapture();
            }
        }

        private static bool IsDescendant(DependencyObject parent, DependencyObject? child)
        {
            var current = child;
            while (current != null)
            {
                if (current == parent) return true;
                current = VisualTreeHelper.GetParent(current);
            }
            return false;
        }

        // ── 繪圖事件 ──────────────────────────────────────────────

        private void OnCanvasMouseDown(object sender, MouseButtonEventArgs e)
        {
            var pos = e.GetPosition(DrawCanvas);
            _previewElement = _currentTool.OnMouseDown(pos, _currentColor, _currentStroke);
            if (_previewElement != null)
                DrawCanvas.Children.Add(_previewElement);
            DrawCanvas.CaptureMouse();
        }

        private void OnCanvasMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed || _previewElement == null) return;
            _currentTool.OnMouseMove(e.GetPosition(DrawCanvas), _previewElement);
        }

        private void OnCanvasMouseUp(object sender, MouseButtonEventArgs e)
        {
            DrawCanvas.ReleaseMouseCapture();
            if (_previewElement == null) return;
            _currentTool.OnMouseUp(e.GetPosition(DrawCanvas), _previewElement);

            if (_currentTool is GaussianBlurTool)
            {
                // GaussianBlurTool 自行移除 preview rect 並加入模糊 Image
                // undo 目標是最後加入 canvas 的 Image
                if (DrawCanvas.Children.Count > 0 &&
                    DrawCanvas.Children[^1] is System.Windows.Controls.Image blurImg)
                    _undoStack.Push(blurImg);
            }
            else if (_currentTool is not TextTool)
            {
                _undoStack.Push(_previewElement);
            }
            _previewElement = null;
        }

        // ── 工具選擇 ──────────────────────────────────────────────

        private void ToolBtn_Click(object sender, RoutedEventArgs e)
        {
            foreach (var child in ToolbarPanel.Children)
            {
                if (child is ToggleButton tb && tb != sender)
                    tb.IsChecked = false;
            }
            var btn = (ToggleButton)sender;
            btn.IsChecked = true;
            _currentTool = (string)btn.Tag switch
            {
                "Rect"   => new RectangleTool(),
                "Pen"    => new PenTool(),
                "Arrow"  => new ArrowTool(),
                "Text"   => new TextTool(DrawCanvas, _undoStack),
                "Blur"   => new GaussianBlurTool(_original, GetDpiScale(), DrawCanvas),
                _        => _currentTool
            };
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Z && Keyboard.Modifiers == ModifierKeys.Control)
                BtnUndo_Click(this, new RoutedEventArgs());
            else if (e.Key == Key.Escape)
                Close();
        }

        // ── 功能按鈕 ──────────────────────────────────────────────

        private void BtnUndo_Click(object sender, RoutedEventArgs e)
        {
            if (_undoStack.Count > 0)
                DrawCanvas.Children.Remove(_undoStack.Pop());
        }

        private void BtnCopy_Click(object sender, RoutedEventArgs e)
            => Clipboard.SetImage(RenderToBitmap());

        private async void BtnOcr_Click(object sender, RoutedEventArgs e)
        {
            BtnOcr.IsEnabled = false;
            OcrLabel.Visibility = Visibility.Collapsed;
            OcrSpinner.Visibility = Visibility.Visible;
            var winOcr = App.Services.GetRequiredService<WindowsOcrService>();
            try
            {
                var bitmap = RenderToBitmap();
                var result = await winOcr.RecognizeAsync(bitmap, App.Settings.OcrWindowsLanguage);
                new OcrResultWindow(result, bitmap).Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"OCR 失敗：{ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                BtnOcr.IsEnabled = true;
                OcrLabel.Visibility = Visibility.Visible;
                OcrSpinner.Visibility = Visibility.Collapsed;
            }
        }

        private void BtnPin_Click(object sender, RoutedEventArgs e)
        {
            new PinnedImageWindow(RenderToBitmap()).Show();
            Close();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();

        // ── 線寬 ──────────────────────────────────────────────────

        private void StrokeBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _currentStroke = StrokeBox.SelectedIndex switch
            {
                0 => 1, 1 => 2, 2 => 3, 3 => 5, _ => 2
            };
        }

        // ── DPI + 輸出 ────────────────────────────────────────────

        private double GetDpiScale()
        {
            var source = PresentationSource.FromVisual(this);
            return source?.CompositionTarget?.TransformToDevice.M11 ?? 1.0;
        }

        public BitmapSource RenderToBitmap()
        {
            var rtb = new RenderTargetBitmap(
                _original.PixelWidth, _original.PixelHeight, 96, 96, PixelFormats.Pbgra32);

            var dv = new DrawingVisual();
            using (var ctx = dv.RenderOpen())
                ctx.DrawImage(_original, new Rect(0, 0, _original.PixelWidth, _original.PixelHeight));
            rtb.Render(dv);

            double dpiScale = GetDpiScale();
            double logW = _original.PixelWidth / dpiScale;
            double logH = _original.PixelHeight / dpiScale;
            DrawCanvas.Measure(new Size(logW, logH));
            DrawCanvas.Arrange(new Rect(0, 0, logW, logH));
            rtb.Render(DrawCanvas);

            return rtb;
        }
    }
}
