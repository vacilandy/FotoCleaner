using System.IO;
using System.Windows.Forms;
using FotoCleaner.Infrastructure;
using FotoCleaner.Models;
using FotoCleaner.Services;
using System.Collections.ObjectModel;
using System.Windows;

namespace FotoCleaner.ViewModels;

public sealed class MainViewModel : ObservableObject
{
    private string selectedFolder = "Ninguna carpeta seleccionada";
    private string statusText = "Listo para analizar";
    private double threshold = 90;
    private double previewSize = 220;
    private bool busy;
    private CancellationTokenSource? scanCancellation;
    private readonly MediaScanner scanner = new(new PerceptualHashService(), new HashDatabase());
    private readonly FileRelocationService relocator = new();
    public ObservableCollection<DuplicateGroup> Groups { get; } = [];
    public ObservableCollection<ImageFormatOption> ImageFormats { get; } = [
        new("JPG"), new("JPEG"), new("PNG"), new("WEBP"), new("BMP"),
        new("GIF"), new("TIFF"), new("HEIC"), new("AVIF"), new("RAW"),
        new("CR2"), new("CR3"), new("NEF"), new("ARW"), new("DNG"), new("ORF"), new("RW2"), new("PEF")
    ];
    public string SelectedFolder { get => selectedFolder; set => SetProperty(ref selectedFolder, value); }
    public string StatusText { get => statusText; set => SetProperty(ref statusText, value); }
    public double Threshold { get => threshold; set => SetProperty(ref threshold, value); }
    public double PreviewSize { get => previewSize; set => SetProperty(ref previewSize, value); }
    public bool CanScan => !busy && Directory.Exists(selectedFolder);
    public bool IsBusy => busy;
    public int DuplicateCount => Groups.Count;
    public RelayCommand SelectFolderCommand { get; }
    public AsyncRelayCommand ScanCommand { get; }
    public AsyncRelayCommand MoveSelectedCommand { get; }
    public RelayCommand SelectLowQualityCommand { get; }
    public RelayCommand ClearSelectionCommand { get; }
    public RelayCommand EnableAllFormatsCommand { get; }
    public RelayCommand DisableAllFormatsCommand { get; }
    public MainViewModel()
    {
        SelectFolderCommand = new(SelectFolder);
        ScanCommand = new(ScanAsync, () => CanScan);
        MoveSelectedCommand = new(MoveSelectedAsync, () => !busy);
        SelectLowQualityCommand = new(SelectLowQuality);
        ClearSelectionCommand = new(ClearSelection);
        EnableAllFormatsCommand = new(() => SetAllFormats(true));
        DisableAllFormatsCommand = new(() => SetAllFormats(false));
    }
    private void SelectFolder()
    {
        try
        {
            var dialog = new FolderBrowserDialog { Description = "Selecciona una carpeta de medios" };
            if (dialog.ShowDialog() == DialogResult.OK) 
            { 
                SelectedFolder = dialog.SelectedPath; 
                OnStateChanged();
                ((AsyncRelayCommand)ScanCommand).RefreshCanExecute();
            }
        }
        catch (Exception ex) { StatusText = $"Error al abrir carpeta: {ex.Message}"; }
    }
    private async Task ScanAsync()
    {
        busy = true; scanCancellation = new CancellationTokenSource(); OnStateChanged(); Groups.Clear(); StatusText = "Preparando análisis...";
        try
        {
            var selectedExtensions = ImageFormats.Where(format => format.IsEnabled).Select(format => $".{format.Extension.ToLowerInvariant()}");
            var files = await scanner.ScanAsync(SelectedFolder, selectedExtensions, new Progress<string>(s => StatusText = s), scanCancellation.Token);
            var processed = new HashSet<string>();
            foreach (var file in files)
            {
                if (processed.Contains(file.FullPath)) continue;
                var similar = files.Where(f => f.FullPath != file.FullPath && Similar(file, f)).ToList();
                if (similar.Count > 0)
                {
                    var group = new List<SelectableMediaFile> { new SelectableMediaFile { File = file } };
                    group.AddRange(similar.Select(f => new SelectableMediaFile { File = f }));
                    foreach (var f in group) processed.Add(f.FullPath);
                    Groups.Add(new DuplicateGroup { Label = $"Grupo similar · {group.Count} archivos", Items = group });
                }
            }
            StatusText = $"Análisis terminado · {files.Count} archivos revisados, {Groups.Count} grupos encontrados";
        }
        catch (OperationCanceledException) { StatusText = "Análisis cancelado"; }
        catch (Exception ex) { StatusText = $"Error: {ex.Message}"; }
        finally { scanCancellation?.Dispose(); scanCancellation = null; busy = false; OnStateChanged(); }
    }
    private bool Similar(MediaFile left, MediaFile right)
    {
        if (left.Duration is not null && right.Duration is not null && Math.Abs((left.Duration.Value - right.Duration.Value).TotalSeconds) > 2) return false;
        return IHashService.HammingDistance(left.Hash, right.Hash) <= Math.Round(64 * (1 - Threshold / 100));
    }
    private async Task MoveSelectedAsync()
    {
        busy = true; OnStateChanged();
        try
        {
            var selected = Groups.SelectMany(g => g.Items).Where(x => x.IsSelected).Select(x => x.FullPath).ToArray();
            if (selected.Length == 0) { StatusText = "Selecciona al menos un archivo para mover"; return; }
            var moved = await relocator.MoveAsync(selected, SelectedFolder, CancellationToken.None);
            StatusText = $"{moved} archivos movidos a Duplicadas";
            await ScanAsync();
        }
        catch (Exception ex) { StatusText = $"Error al mover: {ex.Message}"; }
        finally { busy = false; OnStateChanged(); }
    }
    private void SelectLowQuality()
    {
        foreach (var group in Groups)
        {
            var smallest = group.Items.OrderBy(item => item.File.SizeBytes).FirstOrDefault();
            if (smallest is not null) smallest.IsSelected = true;
        }
        StatusText = "Seleccionada la foto más pequeña de cada grupo";
    }
    private void ClearSelection()
    {
        foreach (var item in Groups.SelectMany(group => group.Items)) item.IsSelected = false;
        StatusText = "Se desmarcaron todas las fotos";
    }
    private void SetAllFormats(bool enabled)
    {
        foreach (var format in ImageFormats) format.IsEnabled = enabled;
        StatusText = enabled ? "Seleccionados todos los formatos" : "Deseleccionados todos los formatos";
    }
    private void OnStateChanged() 
    { 
        OnPropertyChanged(nameof(CanScan)); 
        OnPropertyChanged(nameof(IsBusy));
        OnPropertyChanged(nameof(DuplicateCount)); 
        ((RelayCommand)SelectFolderCommand).Refresh(); 
        ((AsyncRelayCommand)ScanCommand).RefreshCanExecute(); 
    }
}
