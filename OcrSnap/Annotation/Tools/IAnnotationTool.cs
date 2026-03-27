using System.Windows;
using System.Windows.Media;

namespace OcrSnap.Annotation.Tools
{
    public interface IAnnotationTool
    {
        UIElement? OnMouseDown(Point pos, Color color, double stroke);
        void OnMouseMove(Point pos, UIElement element);
        void OnMouseUp(Point pos, UIElement element);
    }
}
