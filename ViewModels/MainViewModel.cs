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
    private double threshold = 92;
    private double previewSize = 180;
    private bool busy;
    private readonly MediaScanner scanner = new(new PerceptualHashService(), new HashDatabase());
    private readonly FileRelocationService relocator = new();
    public ObservableCollection<DuplicateGroup> Groups { get; } = [];
    public string SelectedFolder { get => selectedFolder; set => SetProperty(ref selectedFolder, value); }
    public string StatusText { get => statusText; set => SetProperty(ref statusText, value); }
    public double Threshold { get => threshold; set => SetProperty(ref threshold, value); }
    public double PreviewSize { get => previewSize; set => SetProperty(ref previewSize, value); }
    public bool CanScan => !busy && Directory.Exists(selectedFolder);
    public int DuplicateCount => Groups.Count;
    public RelayCommand SelectFolderCommand { get; }
    public AsyncRelayCommand ScanCommand { get; }
    public AsyncRelayCommand MoveSelectedCommand { get; }
    public RelayCommand SelectLowQualityCommand { get; }
    public MainViewModel()
    {
        SelectFolderCommand = new(SelectFolder);
        ScanCommand = new(ScanAsync, () => CanScan);
        MoveSelectedCommand = new(MoveSelectedAsync, () => !busy);
        SelectLowQualityCommand = new(SelectLowQuality);
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
        busy = true; OnStateChanged(); Groups.Clear(); StatusText = "Preparando análisis...";
        try
        {
            var files = await scanner.ScanAsync(SelectedFolder, new Progress<string>(s => StatusText = s), CancellationToken.None);
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
        catch (Exception ex) { StatusText = $"Error: {ex.Message}"; }
        finally { busy = false; OnStateChanged(); }
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
    private void OnStateChanged() 
    { 
        OnPropertyChanged(nameof(CanScan)); 
        OnPropertyChanged(nameof(DuplicateCount)); 
        ((RelayCommand)SelectFolderCommand).Refresh(); 
        ((AsyncRelayCommand)ScanCommand).RefreshCanExecute(); 
    }
}
