using System.IO;
using System.Windows;
using PhotoForumPPTX.Core;
using PhotoForumPPTX.Gui;

namespace PhotoForumPPTX.App;

public partial class MainWindow : Window
{
    private readonly SettingsViewModel _settingsViewModel;

    public MainWindow()
    {
        InitializeComponent();

        var settings = AppSettingsStore.Load();
        var cameras = new MapTable("cameras.json", DefaultTables.Cameras, DefaultTables.Version);
        var lenses = new MapTable("lenses.json", DefaultTables.Lenses, DefaultTables.Version);
        var templatePath = Path.Combine(AppContext.BaseDirectory, "Resources", "template.pptx");

        _settingsViewModel = new SettingsViewModel(settings, cameras, lenses, new WpfFontCatalog());
        DataContext = new MainViewModel(cameras, lenses, templatePath, new WpfDialogService(), settings);
    }

    private void OnOpenSettingsClick(object sender, RoutedEventArgs e)
    {
        new SettingsWindow(_settingsViewModel) { Owner = this }.ShowDialog();
    }

    private void OnDragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private void OnDrop(object sender, DragEventArgs e)
    {
        if (DataContext is MainViewModel vm && e.Data.GetData(DataFormats.FileDrop) is string[] paths)
            vm.AddPhotos(paths);
    }
}
