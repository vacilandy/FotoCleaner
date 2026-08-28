namespace FotoCleaner.Models;

public sealed record MediaFile(string FullPath, string FileName, long SizeBytes, TimeSpan? Duration, ulong Hash)
{
    public bool IsSelected { get; set; }
    public string Details => $"{SizeBytes / 1024d / 1024d:0.0} MB" + (Duration is null ? "" : $" · {Duration.Value:mm\\:ss}");
}
