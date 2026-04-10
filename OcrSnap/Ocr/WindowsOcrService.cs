using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Windows.Globalization;
using Windows.Media.Ocr;
using Windows.Storage.Streams;
using WinBitmapDecoder = Windows.Graphics.Imaging.BitmapDecoder;
using WinBitmapPixelFormat = Windows.Graphics.Imaging.BitmapPixelFormat;
using WinBitmapAlphaMode = Windows.Graphics.Imaging.BitmapAlphaMode;

namespace OcrSnap.Ocr
{
    public class WindowsOcrService
    {
        public bool IsLanguageSupported(string bcp47Tag)
        {
            try
            {
                var lang = new Language(bcp47Tag);
                return OcrEngine.IsLanguageSupported(lang);
            }
            catch
            {
                return false;
            }
        }

        public async Task<OcrResult> RecognizeAsync(BitmapSource image, string bcp47Tag)
        {
            var lang = new Language(bcp47Tag);
            var engine = OcrEngine.TryCreateFromLanguage(lang)
                ?? throw new Exception($"Windows OCR 不支援語言：{bcp47Tag}\n請至 Windows 設定 → 時間與語言 → 語言，安裝對應語言包並勾選「基本輸入」與「OCR」。");

            // BitmapSource → PNG bytes
            byte[] pngBytes;
            using (var ms = new MemoryStream())
            {
                var enc = new PngBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(image));
                enc.Save(ms);
                pngBytes = ms.ToArray();
            }

            // PNG bytes → SoftwareBitmap（WinRT），明確 using 確保非託管資源即時釋放
            Windows.Media.Ocr.OcrResult winResult;
            var sw = Stopwatch.StartNew();
            using (var raStream = new InMemoryRandomAccessStream())
            {
                await raStream.WriteAsync(pngBytes.AsBuffer());
                raStream.Seek(0);
                var decoder = await WinBitmapDecoder.CreateAsync(raStream);
                using var softwareBitmap = await decoder.GetSoftwareBitmapAsync(
                    WinBitmapPixelFormat.Bgra8, WinBitmapAlphaMode.Premultiplied);
                winResult = await engine.RecognizeAsync(softwareBitmap);
            }
            sw.Stop();

            // 辨識完成後立即回收 WinRT 非託管資源
            GC.Collect(2, GCCollectionMode.Optimized);
            GC.WaitForPendingFinalizers();

            var sb = new StringBuilder();
            var regions = new List<OcrRegion>();

            // 中日韓文字不在字與字之間加空格
            bool isCjk = bcp47Tag.StartsWith("zh") || bcp47Tag == "ja" || bcp47Tag == "ko";

            foreach (var line in winResult.Lines)
            {
                string lineText = isCjk
                    ? string.Concat(line.Words.Select(w => w.Text))
                    : line.Text;
                sb.AppendLine(lineText);

                // 從 Words 計算整行 bounding box
                double minX = double.MaxValue, minY = double.MaxValue;
                double maxX = 0, maxY = 0;
                foreach (var word in line.Words)
                {
                    var r = word.BoundingRect;
                    if (r.X < minX) minX = r.X;
                    if (r.Y < minY) minY = r.Y;
                    if (r.X + r.Width > maxX) maxX = r.X + r.Width;
                    if (r.Y + r.Height > maxY) maxY = r.Y + r.Height;
                }

                regions.Add(new OcrRegion
                {
                    Page = 1,
                    Type = "text",
                    Text = lineText,
                    Confidence = 1.0,
                    BoundingBox = new BoundingBox
                    {
                        X = minX == double.MaxValue ? 0 : minX,
                        Y = minY == double.MaxValue ? 0 : minY,
                        Width = maxX - (minX == double.MaxValue ? 0 : minX),
                        Height = maxY - (minY == double.MaxValue ? 0 : minY)
                    }
                });
            }

            return new OcrResult
            {
                Markdown = sb.ToString().TrimEnd(),
                Pages = 1,
                ProcessTimeMs = (int)sw.ElapsedMilliseconds,
                Regions = regions.ToArray()
            };
        }
    }
}
