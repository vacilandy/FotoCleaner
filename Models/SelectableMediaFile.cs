using FotoCleaner.Infrastructure;

namespace FotoCleaner.Models;

public sealed class SelectableMediaFile : ObservableObject
{
    private bool isSelected;
    private int rotation;
    public required MediaFile File { get; init; }
    public bool IsSelected { get => isSelected; set => SetProperty(ref isSelected, value); }
    public int Rotation { get => rotation; private set => SetProperty(ref rotation, value); }
    public RelayCommand RotateCommand { get; }
    public RelayCommand SelectCommand { get; }
    public string FileName => File.FileName;
    public string FullPath => File.FullPath;
    public string Details => File.Details;
    public SelectableMediaFile() { RotateCommand = new(Rotate); SelectCommand = new(() => IsSelected = true); }
    private void Rotate() => Rotation = (Rotation + 90) % 360;
}
