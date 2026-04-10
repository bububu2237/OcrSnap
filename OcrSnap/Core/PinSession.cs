using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace OcrSnap.Core
{
    public class PinEntry
    {
        public string Id { get; set; } = "";
        public double Left { get; set; }
        public double Top { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public double Opacity { get; set; } = 1.0;
        public string ImageFile { get; set; } = "";   // 僅檔名，不含路徑
    }

    /// <summary>
    /// 管理釘選視窗的持久化狀態。
    /// 圖片存為 PNG（磁碟壓縮），metadata 存為 pins.json。
    /// 設計原則：記憶體中不快取圖片，僅在還原視窗時才載入。
    /// </summary>
    public static class PinSession
    {
        private static readonly string PinsDir =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                         "OcrSnap", "pins");
        private static readonly string IndexPath = Path.Combine(PinsDir, "pins.json");

        private static List<PinEntry> _entries = new();

        public static IReadOnlyList<PinEntry> Entries => _entries;

        public static void Load()
        {
            try
            {
                if (!File.Exists(IndexPath)) return;
                var json = File.ReadAllText(IndexPath);
                _entries = JsonSerializer.Deserialize<List<PinEntry>>(json) ?? new();

                // 移除圖檔已不存在的孤兒記錄
                _entries.RemoveAll(e => !File.Exists(Path.Combine(PinsDir, e.ImageFile)));
            }
            catch
            {
                _entries = new();
            }
        }

        /// <summary>新增釘選；將 BitmapSource 壓縮儲存為 PNG，回傳 pinId。</summary>
        public static string AddPin(BitmapSource image, double left, double top, double width, double height)
        {
            Directory.CreateDirectory(PinsDir);
            var id = Guid.NewGuid().ToString("N");
            var fileName = id + ".png";
            var filePath = Path.Combine(PinsDir, fileName);

            // 以 PNG 壓縮存檔，磁碟佔用遠低於 RAW 像素
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(image));
            using (var fs = File.OpenWrite(filePath))
                encoder.Save(fs);

            _entries.Add(new PinEntry
            {
                Id = id,
                Left = left, Top = top,
                Width = width, Height = height,
                Opacity = 1.0,
                ImageFile = fileName
            });
            Save();
            return id;
        }

        public static void RemovePin(string id)
        {
            var entry = _entries.Find(e => e.Id == id);
            if (entry == null) return;

            _entries.Remove(entry);
            Save();

            // 刪除 PNG 檔
            try { File.Delete(Path.Combine(PinsDir, entry.ImageFile)); } catch { }
        }

        public static void UpdatePin(string id, double left, double top, double opacity)
        {
            var entry = _entries.Find(e => e.Id == id);
            if (entry == null) return;
            entry.Left = left;
            entry.Top = top;
            entry.Opacity = opacity;
            Save();
        }

        /// <summary>載入指定 pin 的 BitmapImage（BitmapCacheOption.OnLoad = 讀完即釋放檔案 handle）。</summary>
        public static BitmapImage? LoadImage(PinEntry entry)
        {
            var path = Path.Combine(PinsDir, entry.ImageFile);
            if (!File.Exists(path)) return null;
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.UriSource = new Uri(path, UriKind.Absolute);
            bmp.CacheOption = BitmapCacheOption.OnLoad;  // 讀入後立即釋放 file handle
            bmp.EndInit();
            bmp.Freeze();
            return bmp;
        }

        private static void Save()
        {
            try
            {
                Directory.CreateDirectory(PinsDir);
                var json = JsonSerializer.Serialize(_entries, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(IndexPath, json);
            }
            catch { }
        }
    }
}
