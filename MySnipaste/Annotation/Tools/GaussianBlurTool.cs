using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace OcrSnap.Annotation.Tools
{
    public class GaussianBlurTool : IAnnotationTool
    {
        private readonly BitmapSource _original;
        private readonly double _dpiScale;
        private readonly Canvas _canvas;
        private Point _start;

        public GaussianBlurTool(BitmapSource original, double dpiScale, Canvas canvas)
        {
            _original = original;
            _dpiScale = dpiScale;
            _canvas = canvas;
        }

        public UIElement? OnMouseDown(Point pos, Color color, double stroke)
        {
            _start = pos;
            var rect = new Rectangle
            {
                Stroke = Brushes.Cyan,
                StrokeThickness = 1.5,
                StrokeDashArray = new DoubleCollection(new double[] { 4, 2 }),
                Fill = new SolidColorBrush(Color.FromArgb(30, 0, 200, 255)),
                IsHitTestVisible = false
            };
            Canvas.SetLeft(rect, pos.X);
            Canvas.SetTop(rect, pos.Y);
            return rect;
        }

        public void OnMouseMove(Point pos, UIElement element)
        {
            if (element is not Rectangle rect) return;
            double x = Math.Min(_start.X, pos.X);
            double y = Math.Min(_start.Y, pos.Y);
            Canvas.SetLeft(rect, x);
            Canvas.SetTop(rect, y);
            rect.Width = Math.Max(1, Math.Abs(pos.X - _start.X));
            rect.Height = Math.Max(1, Math.Abs(pos.Y - _start.Y));
        }

        public void OnMouseUp(Point pos, UIElement element)
        {
            if (element is not Rectangle rect) return;
            double x = Canvas.GetLeft(rect);
            double y = Canvas.GetTop(rect);
            double w = rect.Width;
            double h = rect.Height;

            _canvas.Children.Remove(rect);

            if (w < 4 || h < 4) return;

            int px = Math.Clamp((int)(x * _dpiScale), 0, _original.PixelWidth - 1);
            int py = Math.Clamp((int)(y * _dpiScale), 0, _original.PixelHeight - 1);
            int pw = Math.Clamp((int)(w * _dpiScale), 1, _original.PixelWidth - px);
            int ph = Math.Clamp((int)(h * _dpiScale), 1, _original.PixelHeight - py);

            var crop = new CroppedBitmap(_original, new Int32Rect(px, py, pw, ph));
            crop.Freeze();

            var img = new System.Windows.Controls.Image
            {
                Source = crop,
                Width = w,
                Height = h,
                Stretch = Stretch.Fill,
                Effect = new BlurEffect { Radius = 15, KernelType = KernelType.Gaussian }
            };

            Canvas.SetLeft(img, x);
            Canvas.SetTop(img, y);
            _canvas.Children.Add(img);
        }
    }
}
