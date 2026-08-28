using FotoCleaner.ViewModels;

namespace FotoCleaner;

public partial class MainWindow : System.Windows.Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
    }
}
