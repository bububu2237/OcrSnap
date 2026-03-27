using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace OcrSnap.Annotation.Tools
{
    public class RectangleTool : IAnnotationTool
    {
        private Point _start;

        public UIElement? OnMouseDown(Point pos, Color color, double stroke)
        {
            _start = pos;
            var rect = new Rectangle
            {
                Stroke = new SolidColorBrush(color),
                StrokeThickness = stroke,
                Fill = Brushes.Transparent
            };
            Canvas.SetLeft(rect, pos.X);
            Canvas.SetTop(rect, pos.Y);
            return rect;
        }

        public void OnMouseMove(Point pos, UIElement element)
        {
            if (element is not Rectangle rect) return;
            double x = Math.Min(pos.X, _start.X);
            double y = Math.Min(pos.Y, _start.Y);
            rect.Width = Math.Abs(pos.X - _start.X);
            rect.Height = Math.Abs(pos.Y - _start.Y);
            Canvas.SetLeft(rect, x);
            Canvas.SetTop(rect, y);
        }

        public void OnMouseUp(Point pos, UIElement element) => OnMouseMove(pos, element);
    }
}
