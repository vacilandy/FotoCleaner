using System.Windows;
using FotoCleaner.ViewModels;

namespace FotoCleaner;

public partial class MainWindow : System.Windows.Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
    }

    private void ResultsList_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
    {
        ResultsScrollViewer.ScrollToVerticalOffset(ResultsScrollViewer.VerticalOffset - e.Delta);
        e.Handled = true;
    }
}
