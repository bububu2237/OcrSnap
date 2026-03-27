using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

namespace OcrSnap.Annotation.Tools
{
    public class PenTool : IAnnotationTool
    {
        public UIElement? OnMouseDown(Point pos, Color color, double stroke)
        {
            var path = new Polyline
            {
                Stroke = new SolidColorBrush(color),
                StrokeThickness = stroke,
                StrokeLineJoin = PenLineJoin.Round,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round
            };
            path.Points.Add(pos);
            return path;
        }

        public void OnMouseMove(Point pos, UIElement element)
        {
            if (element is Polyline path)
                path.Points.Add(pos);
        }

        public void OnMouseUp(Point pos, UIElement element) => OnMouseMove(pos, element);
    }
}
