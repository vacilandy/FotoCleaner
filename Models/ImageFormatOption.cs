using FotoCleaner.Infrastructure;

namespace FotoCleaner.Models;

public sealed class ImageFormatOption(string extension, bool isEnabled = true) : ObservableObject
{
    private bool isEnabled = isEnabled;
    public string Extension { get; } = extension;
    public bool IsEnabled { get => isEnabled; set => SetProperty(ref isEnabled, value); }
}
