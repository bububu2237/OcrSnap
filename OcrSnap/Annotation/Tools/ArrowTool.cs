using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

namespace OcrSnap.Annotation.Tools
{
    public class ArrowTool : IAnnotationTool
    {
        private Point _start;

        public UIElement? OnMouseDown(Point pos, Color color, double stroke)
        {
            _start = pos;
            return BuildArrow(pos, pos, color, stroke);
        }

        public void OnMouseMove(Point pos, UIElement element)
        {
            if (element is not Path path) return;
            var color = (path.Stroke as SolidColorBrush)?.Color ?? Colors.Red;
            var newPath = BuildArrow(_start, pos, color, path.StrokeThickness);
            path.Data = ((Path)newPath).Data;
        }

        public void OnMouseUp(Point pos, UIElement element) => OnMouseMove(pos, element);

        private static Path BuildArrow(Point from, Point to, Color color, double stroke)
        {
            var group = new GeometryGroup();

            // 主幹
            group.Children.Add(new LineGeometry(from, to));

            // 箭頭頭部
            double angle = Math.Atan2(to.Y - from.Y, to.X - from.X);
            double headLen = Math.Max(10, stroke * 5);
            double headAngle = Math.PI / 6; // 30 度

            var p1 = new Point(
                to.X - headLen * Math.Cos(angle - headAngle),
                to.Y - headLen * Math.Sin(angle - headAngle));
            var p2 = new Point(
                to.X - headLen * Math.Cos(angle + headAngle),
                to.Y - headLen * Math.Sin(angle + headAngle));

            var head = new PathGeometry();
            var figure = new PathFigure { StartPoint = to };
            figure.Segments.Add(new LineSegment(p1, true));
            figure.Segments.Add(new LineSegment(to, true));
            figure.Segments.Add(new LineSegment(p2, true));
            head.Figures.Add(figure);
            group.Children.Add(head);

            return new Path
            {
                Data = group,
                Stroke = new SolidColorBrush(color),
                StrokeThickness = stroke,
                StrokeLineJoin = PenLineJoin.Round,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round
            };
        }
    }
}
