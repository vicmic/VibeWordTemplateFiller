using System.Windows;
using System.Windows.Input;
using DistanceCalculator.ViewModels;

namespace DistanceCalculator;

public partial class MainWindow : Window
{
    private MainViewModel ViewModel => (MainViewModel)DataContext;

    public MainWindow()
    {
        InitializeComponent();
    }

    private void Window_DragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            bool isExcel = files?.Any(f =>
                f.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase) ||
                f.EndsWith(".xls", StringComparison.OrdinalIgnoreCase)) ?? false;
            e.Effects = isExcel ? DragDropEffects.Copy : DragDropEffects.None;
        }
        else
        {
            e.Effects = DragDropEffects.None;
        }
        e.Handled = true;
    }

    private void Window_Drop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;

        var files = (string[])e.Data.GetData(DataFormats.FileDrop);
        var excelFile = files?.FirstOrDefault(f =>
            f.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase) ||
            f.EndsWith(".xls", StringComparison.OrdinalIgnoreCase));

        if (excelFile != null)
            ViewModel.LoadFile(excelFile);
    }

    private void DropZone_Click(object sender, MouseButtonEventArgs e)
    {
        ViewModel.BrowseFileCommand.Execute(null);
    }
}
