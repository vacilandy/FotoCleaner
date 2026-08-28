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
    public string FileName => File.FileName;
    public string FullPath => File.FullPath;
    public string Details => File.Details;
    public string Resolution => File.Resolution;
    public string Location => File.Location;
    public string Format => File.Format;
    public string Weight => File.Weight;
    public int QualityScore => File.QualityScore;
    public string QualityLabel => $"Calidad estimada: {QualityScore}/100";
    public SelectableMediaFile() { RotateCommand = new(Rotate); }
    private void Rotate() => Rotation = (Rotation + 90) % 360;
}
