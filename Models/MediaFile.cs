using System.IO;

namespace FotoCleaner.Models;

public sealed record MediaFile(string FullPath, string FileName, long SizeBytes, TimeSpan? Duration, ulong Hash, int Width = 0, int Height = 0)
{
    public bool IsSelected { get; set; }
    public string Format => Path.GetExtension(FileName).TrimStart('.').ToUpperInvariant();
    public string Resolution => Width > 0 && Height > 0 ? $"{Width:N0} x {Height:N0}" : "No disponible";
    public string Location => Path.GetDirectoryName(FullPath) ?? FullPath;
    public string Weight => $"{SizeBytes / 1024d / 1024d:0.0} MB";
    public int QualityScore => Math.Clamp((int)Math.Round(Math.Min(50, Width * (double)Height / 4_000_000d * 5) + Math.Min(30, SizeBytes / 1024d / 1024d / 2d * 3) + (Format is "RAW" or "CR2" or "CR3" or "NEF" or "ARW" or "DNG" or "TIFF" or "TIF" ? 20 : Format is "PNG" or "WEBP" ? 17 : 14)), 0, 100);
    public string Details => $"{Weight}" + (Duration is null ? "" : $" · {Duration.Value:mm\\:ss}");
}
