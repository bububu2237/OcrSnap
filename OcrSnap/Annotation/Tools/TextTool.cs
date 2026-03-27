using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace OcrSnap.Annotation.Tools
{
    public class TextTool : IAnnotationTool
    {
        private readonly Canvas _canvas;
        private readonly Stack<UIElement> _undoStack;

        public TextTool(Canvas canvas, Stack<UIElement> undoStack)
        {
            _canvas = canvas;
            _undoStack = undoStack;
        }

        public UIElement? OnMouseDown(Point pos, Color color, double stroke)
        {
            var box = new TextBox
            {
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(color),
                Foreground = new SolidColorBrush(color),
                FontSize = Math.Max(12, stroke * 5),
                MinWidth = 60,
                CaretBrush = new SolidColorBrush(color)
            };
            Canvas.SetLeft(box, pos.X);
            Canvas.SetTop(box, pos.Y);
            _canvas.Children.Add(box);
            box.Focus();

            box.LostFocus += (_, _) =>
            {
                if (string.IsNullOrWhiteSpace(box.Text))
                {
                    _canvas.Children.Remove(box);
                }
                else
                {
                    // 轉為 TextBlock（不可編輯）
                    var tb = new TextBlock
                    {
                        Text = box.Text,
                        Foreground = new SolidColorBrush(color),
                        FontSize = box.FontSize,
                        Background = Brushes.Transparent
                    };
                    Canvas.SetLeft(tb, Canvas.GetLeft(box));
                    Canvas.SetTop(tb, Canvas.GetTop(box));
                    _canvas.Children.Remove(box);
                    _canvas.Children.Add(tb);
                    _undoStack.Push(tb);
                }
            };

            box.KeyDown += (_, e) =>
            {
                if (e.Key == Key.Escape)
                {
                    _canvas.Children.Remove(box);
                    e.Handled = true;
                }
            };

            return null; // 自行管理元素
        }

        public void OnMouseMove(Point pos, UIElement element) { }
        public void OnMouseUp(Point pos, UIElement element) { }
    }
}
