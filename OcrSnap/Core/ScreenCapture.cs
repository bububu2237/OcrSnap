using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace OcrSnap.Core
{
    public static class ScreenCapture
    {
        /// <summary>?瑕???撟?雿萇銝撘萄?????Ｗ?摨扳?嚗?/summary>
        public static BitmapSource CaptureAllScreens()
        {
            int x = (int)SystemParameters.VirtualScreenLeft;
            int y = (int)SystemParameters.VirtualScreenTop;
            int w = (int)SystemParameters.VirtualScreenWidth;
            int h = (int)SystemParameters.VirtualScreenHeight;

            using var bmp = new Bitmap(w, h, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using var g = Graphics.FromImage(bmp);
            g.CopyFromScreen(x, y, 0, 0, new System.Drawing.Size(w, h), CopyPixelOperation.SourceCopy);

            return BitmapToSource(bmp);
        }

        /// <summary>鋆????????Ｗ?摨扳?嚗?/summary>
        public static BitmapSource CaptureRegion(Rect region)
        {
            int x = (int)SystemParameters.VirtualScreenLeft;
            int y = (int)SystemParameters.VirtualScreenTop;
            int w = (int)SystemParameters.VirtualScreenWidth;
            int h = (int)SystemParameters.VirtualScreenHeight;

            using var fullBmp = new Bitmap(w, h, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(fullBmp))
                g.CopyFromScreen(x, y, 0, 0, new System.Drawing.Size(w, h), CopyPixelOperation.SourceCopy);

            int rx = (int)(region.X - x);
            int ry = (int)(region.Y - y);
            int rw = Math.Max(1, (int)region.Width);
            int rh = Math.Max(1, (int)region.Height);

            using var cropped = new Bitmap(rw, rh);
            using (var g = Graphics.FromImage(cropped))
                g.DrawImage(fullBmp, new Rectangle(0, 0, rw, rh), new Rectangle(rx, ry, rw, rh), GraphicsUnit.Pixel);

            return BitmapToSource(cropped);
        }

        /// <summary>???Ｗ?????蝝???/summary>
        public static System.Windows.Media.Color GetPixelColor(int screenX, int screenY)
        {
            IntPtr hdc = NativeMethods.GetDC(IntPtr.Zero);
            try
            {
                uint colorRef = NativeMethods.GetPixel(hdc, screenX, screenY);
                byte r = (byte)(colorRef & 0xFF);
                byte g = (byte)((colorRef >> 8) & 0xFF);
                byte b = (byte)((colorRef >> 16) & 0xFF);
                return System.Windows.Media.Color.FromRgb(r, g, b);
            }
            finally
            {
                NativeMethods.ReleaseDC(IntPtr.Zero, hdc);
            }
        }

        public static BitmapSource BitmapToSource(Bitmap bmp)
        {
            using var ms = new MemoryStream();
            bmp.Save(ms, ImageFormat.Png);
            ms.Seek(0, SeekOrigin.Begin);
            var decoder = new PngBitmapDecoder(ms, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
            return decoder.Frames[0];
        }

        /// <summary>撠?BitmapSource 頧 Base64 PNG 摮葡</summary>
        public static string ToBase64Png(BitmapSource source)
        {
            using var ms = new MemoryStream();
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(source));
            encoder.Save(ms);
            return Convert.ToBase64String(ms.ToArray());
        }
    }
}
