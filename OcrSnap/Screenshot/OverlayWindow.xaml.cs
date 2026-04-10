using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using OcrSnap.Annotation;
using OcrSnap.Core;

namespace OcrSnap.Screenshot
{
    public partial class OverlayWindow : Window
    {
        private BitmapSource? _screenBitmap;
        private bool _isSelecting;
        private Point _startPoint;
        private Point _currentPoint;
        private Rect _selectedRect;

        // 選取框元素
        private readonly Rectangle _selectionRect = new()
        {
            Stroke = Brushes.Yellow,
            StrokeThickness = 1.5,
            Fill = Brushes.Transparent,
            IsHitTestVisible = false
        };
        private readonly TextBlock _sizeLabel = new()
        {
            Foreground = Brushes.Yellow,
            Background = new SolidColorBrush(Color.FromArgb(160, 0, 0, 0)),
            FontSize = 11,
            Padding = new Thickness(4, 2, 4, 2),
            IsHitTestVisible = false
        };

        // 暗色遮罩
        private readonly Rectangle _maskTop = CreateMask();
        private readonly Rectangle _maskBottom = CreateMask();
        private readonly Rectangle _maskLeft = CreateMask();
        private readonly Rectangle _maskRight = CreateMask();

        private static Rectangle CreateMask() => new()
        {
            Fill = new SolidColorBrush(Color.FromArgb(100, 0, 0, 0)),
            IsHitTestVisible = false
        };

        public OverlayWindow()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            MouseLeftButtonDown += OnMouseDown;
            MouseMove += OnMouseMove;
            MouseLeftButtonUp += OnMouseUp;
            KeyDown += OnKeyDown;
            Focusable = true;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // 覆蓋所有螢幕
            Left = SystemParameters.VirtualScreenLeft;
            Top = SystemParameters.VirtualScreenTop;
            Width = SystemParameters.VirtualScreenWidth;
            Height = SystemParameters.VirtualScreenHeight;

            // 截圖
            _screenBitmap = ScreenCapture.CaptureAllScreens();
            ScreenImage.Source = _screenBitmap;
            ScreenImage.Width = Width;
            ScreenImage.Height = Height;

            // 初始化遮罩（全螢幕暗色）
            MaskCanvas.Children.Add(_maskTop);
            MaskCanvas.Children.Add(_maskBottom);
            MaskCanvas.Children.Add(_maskLeft);
            MaskCanvas.Children.Add(_maskRight);
            UpdateMask(new Rect());

            // 初始化選取框
            SelectionCanvas.Children.Add(_selectionRect);
            SelectionCanvas.Children.Add(_sizeLabel);

            Focus();
        }

        private void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            _isSelecting = true;
            _startPoint = e.GetPosition(RootGrid);
            _currentPoint = _startPoint;
            CaptureMouse();
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            _currentPoint = e.GetPosition(RootGrid);

            // 更新放大鏡
            var screenPos = PointToScreen(_currentPoint);
            Magnifier.Update(_screenBitmap!, screenPos, _currentPoint,
                             App.Settings.ColorDisplayHex);

            // 放大鏡位置（跟著游標，保持在畫面內）
            double mx = _currentPoint.X + 20;
            double my = _currentPoint.Y + 20;
            if (mx + Magnifier.Width > Width) mx = _currentPoint.X - Magnifier.Width - 10;
            if (my + Magnifier.Height > Height) my = _currentPoint.Y - Magnifier.Height - 10;
            Canvas.SetLeft(Magnifier, mx);
            Canvas.SetTop(Magnifier, my);

            if (_isSelecting)
                UpdateSelection();
        }

        private void OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (!_isSelecting) return;
            _isSelecting = false;
            ReleaseMouseCapture();
            UpdateSelection();

            if (_selectedRect.Width < 5 || _selectedRect.Height < 5)
            {
                Close();
                return;
            }

            ConfirmSelection();
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Escape:
                    Close();
                    break;
                case Key.C:
                    CopyCurrentColor();
                    break;
                case Key.LeftShift:
                case Key.RightShift:
                    App.Settings.ColorDisplayHex = !App.Settings.ColorDisplayHex;
                    Magnifier.Update(_screenBitmap!,
                        PointToScreen(_currentPoint), _currentPoint,
                        App.Settings.ColorDisplayHex);
                    break;
            }
        }

        private void UpdateSelection()
        {
            double x = Math.Min(_startPoint.X, _currentPoint.X);
            double y = Math.Min(_startPoint.Y, _currentPoint.Y);
            double w = Math.Abs(_currentPoint.X - _startPoint.X);
            double h = Math.Abs(_currentPoint.Y - _startPoint.Y);
            _selectedRect = new Rect(x, y, w, h);

            Canvas.SetLeft(_selectionRect, x);
            Canvas.SetTop(_selectionRect, y);
            _selectionRect.Width = w;
            _selectionRect.Height = h;

            // 尺寸標籤
            _sizeLabel.Text = $"{(int)w} × {(int)h}";
            Canvas.SetLeft(_sizeLabel, x);
            Canvas.SetTop(_sizeLabel, Math.Max(0, y - 22));

            UpdateMask(_selectedRect);
        }

        private void UpdateMask(Rect sel)
        {
            double sw = Width, sh = Height;

            // 上
            Canvas.SetLeft(_maskTop, 0); Canvas.SetTop(_maskTop, 0);
            _maskTop.Width = sw; _maskTop.Height = sel.IsEmpty ? sh : sel.Top;

            // 下
            double bottomTop = sel.IsEmpty ? sh : sel.Bottom;
            Canvas.SetLeft(_maskBottom, 0); Canvas.SetTop(_maskBottom, bottomTop);
            _maskBottom.Width = sw; _maskBottom.Height = Math.Max(0, sh - bottomTop);

            // 左（夾在上下之間）
            double midTop = sel.IsEmpty ? 0 : sel.Top;
            double midH = sel.IsEmpty ? 0 : sel.Height;
            Canvas.SetLeft(_maskLeft, 0); Canvas.SetTop(_maskLeft, midTop);
            _maskLeft.Width = sel.IsEmpty ? sw : sel.Left; _maskLeft.Height = midH;

            // 右
            double rightLeft = sel.IsEmpty ? sw : sel.Right;
            Canvas.SetLeft(_maskRight, rightLeft); Canvas.SetTop(_maskRight, midTop);
            _maskRight.Width = Math.Max(0, sw - rightLeft); _maskRight.Height = midH;
        }

        private void CopyCurrentColor()
        {
            var screenPos = PointToScreen(_currentPoint);
            var color = ScreenCapture.GetPixelColor((int)screenPos.X, (int)screenPos.Y);
            string text = App.Settings.ColorDisplayHex
                ? $"#{color.R:X2}{color.G:X2}{color.B:X2}"
                : $"rgb({color.R}, {color.G}, {color.B})";
            Clipboard.SetText(text);
        }

        private void ConfirmSelection()
        {
            try
            {
                var topLeft = PointToScreen(new Point(_selectedRect.X, _selectedRect.Y));
                var screenRect = new Rect(topLeft.X, topLeft.Y, _selectedRect.Width, _selectedRect.Height);

                // 從已擷取的 _screenBitmap 裁切，避免把選取框白線截入
                int offX = (int)SystemParameters.VirtualScreenLeft;
                int offY = (int)SystemParameters.VirtualScreenTop;
                int cropX = Math.Clamp((int)(topLeft.X - offX), 0, _screenBitmap!.PixelWidth - 1);
                int cropY = Math.Clamp((int)(topLeft.Y - offY), 0, _screenBitmap!.PixelHeight - 1);
                int cropW = Math.Clamp((int)_selectedRect.Width, 1, _screenBitmap!.PixelWidth - cropX);
                int cropH = Math.Clamp((int)_selectedRect.Height, 1, _screenBitmap!.PixelHeight - cropY);

                var cropped = new System.Windows.Media.Imaging.CroppedBitmap(
                    _screenBitmap!, new System.Windows.Int32Rect(cropX, cropY, cropW, cropH));
                // 深拷貝像素到獨立 WriteableBitmap，打斷對全螢幕 _screenBitmap 的參考鏈
                var captured = new System.Windows.Media.Imaging.WriteableBitmap(cropped);
                captured.Freeze();

                // 立即釋放全螢幕截圖，讓 GC 可回收這塊大記憶體（通常 100~400 MB）
                ScreenImage.Source = null;
                _screenBitmap = null;

                Hide();

                var annotWin = new AnnotationWindow(captured, screenRect);
                annotWin.Show();

                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"錯誤：{ex.GetType().Name}\n\n{ex.Message}\n\n{ex.StackTrace}",
                    "截圖失敗", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }
        }
    }
}
