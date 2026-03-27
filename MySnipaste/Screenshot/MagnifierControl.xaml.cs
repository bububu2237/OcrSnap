using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace OcrSnap.Screenshot
{
    public partial class MagnifierControl : UserControl
    {
        private const int SampleRadius = 8;   // 取樣半徑（像素），共 17x17
        private const int DisplaySize = 160;   // 顯示區域大小

        public MagnifierControl()
        {
            InitializeComponent();
        }

        public void Update(BitmapSource fullScreen, Point screenPos, Point windowPos, bool showHex)
        {
            if (fullScreen == null) return;

            int sw = fullScreen.PixelWidth;
            int sh = fullScreen.PixelHeight;

            // 螢幕座標轉換到圖片座標（虛擬螢幕偏移）
            double offX = SystemParameters.VirtualScreenLeft;
            double offY = SystemParameters.VirtualScreenTop;
            int imgX = (int)(screenPos.X - offX);
            int imgY = (int)(screenPos.Y - offY);

            // 取樣區域
            int sampleSize = SampleRadius * 2 + 1;
            int srcX = Math.Max(0, imgX - SampleRadius);
            int srcY = Math.Max(0, imgY - SampleRadius);
            int srcW = Math.Min(sampleSize, sw - srcX);
            int srcH = Math.Min(sampleSize, sh - srcY);

            if (srcW <= 0 || srcH <= 0) return;

            // 裁切並縮放
            var cropped = new CroppedBitmap(fullScreen, new Int32Rect(srcX, srcY, srcW, srcH));
            ZoomImage.Source = cropped;
            ZoomImage.Width = DisplaySize;
            ZoomImage.Height = 120;

            // 中心十字
            double cx = DisplaySize / 2.0;
            double cy = 120 / 2.0;
            CrossH.X1 = 0; CrossH.Y1 = cy; CrossH.X2 = DisplaySize; CrossH.Y2 = cy;
            CrossV.X1 = cx; CrossV.Y1 = 0; CrossV.X2 = cx; CrossV.Y2 = 120;

            // 中心像素顏色
            var color = GetBitmapPixel(fullScreen, imgX, imgY);
            ColorSwatch.Background = new SolidColorBrush(color);

            if (showHex)
                ColorText.Text = $"#{color.R:X2}{color.G:X2}{color.B:X2}";
            else
                ColorText.Text = $"R:{color.R}  G:{color.G}  B:{color.B}";

            CoordText.Text = $"X:{(int)screenPos.X}  Y:{(int)screenPos.Y}";
        }

        private static Color GetBitmapPixel(BitmapSource bmp, int x, int y)
        {
            x = Math.Clamp(x, 0, bmp.PixelWidth - 1);
            y = Math.Clamp(y, 0, bmp.PixelHeight - 1);

            var cropped = new CroppedBitmap(bmp, new Int32Rect(x, y, 1, 1));
            byte[] pixels = new byte[4];
            cropped.CopyPixels(pixels, 4, 0);
            return Color.FromRgb(pixels[2], pixels[1], pixels[0]); // BGR -> RGB
        }
    }
}
