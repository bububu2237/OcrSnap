using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace OcrSnap.Annotation
{
    public partial class ColorPickerWindow : Window
    {
        public Color SelectedColor { get; private set; }

        private static readonly Color[] PresetColors =
        [
            Colors.Red, Colors.OrangeRed, Colors.Orange, Colors.Yellow,
            Colors.GreenYellow, Colors.Green, Colors.Cyan, Colors.DodgerBlue,
            Colors.Blue, Colors.Purple, Colors.HotPink, Colors.White,
            Colors.LightGray, Colors.Gray, Colors.Black
        ];

        public ColorPickerWindow(Color initial)
        {
            InitializeComponent();
            SelectedColor = initial;
            HexBox.Text = $"#{initial.R:X2}{initial.G:X2}{initial.B:X2}";
            BuildPresets();
        }

        private void BuildPresets()
        {
            foreach (var color in PresetColors)
            {
                var border = new Border
                {
                    Width = 24, Height = 24, Margin = new Thickness(2),
                    Background = new SolidColorBrush(color),
                    BorderBrush = Brushes.Gray, BorderThickness = new Thickness(1),
                    Cursor = System.Windows.Input.Cursors.Hand
                };
                var c = color;
                border.MouseLeftButtonUp += (_, _) =>
                {
                    SelectedColor = c;
                    HexBox.Text = $"#{c.R:X2}{c.G:X2}{c.B:X2}";
                };
                ColorPanel.Children.Add(border);
            }
        }

        private void HexBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                var text = HexBox.Text.Trim();
                if (!text.StartsWith('#')) text = "#" + text;
                var color = (Color)ColorConverter.ConvertFromString(text);
                SelectedColor = color;
                PreviewSwatch.Background = new SolidColorBrush(color);
            }
            catch { }
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e) => DialogResult = true;
        private void BtnCancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
    }
}
