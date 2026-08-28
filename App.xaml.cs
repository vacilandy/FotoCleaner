using System.Windows;

namespace FotoCleaner;

public partial class App : System.Windows.Application
{
    public App()
    {
        AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
        {
            System.Windows.MessageBox.Show($"Error no capturado:\n{e.ExceptionObject}", "Error Fatal", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        };
        DispatcherUnhandledException += (sender, e) =>
        {
            System.Windows.MessageBox.Show($"Error en UI:\n{e.Exception}", "Error en Interfaz", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            e.Handled = true;
        };
    }
}
