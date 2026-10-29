using System.IO;
using System.Windows;
using PhotoForumPPTX.Core;
using PhotoForumPPTX.Gui;

namespace PhotoForumPPTX.App;

public partial class MainWindow : Window
{
    private readonly SettingsViewModel _settingsViewModel;
    private readonly UpdateViewModel _updateViewModel;
    private readonly AppSettings _settings;
    private readonly IReadOnlyList<ReleaseNote> _pendingNotes;

    public MainWindow()
    {
        InitializeComponent();

        var hadExistingSettings = AppSettingsStore.Exists();
        _settings = AppSettingsStore.Load();
        var cameras = new MapTable("cameras.json", DefaultTables.Cameras, DefaultTables.Version);
        var lenses = new MapTable("lenses.json", DefaultTables.Lenses, DefaultTables.Version);
        var templatePath = Path.Combine(AppContext.BaseDirectory, "Resources", "template.pptx");

        var (pending, lastSeenToPersist) = WhatsNewCoordinator.Compute(_settings.LastSeenVersion, hadExistingSettings);
        _pendingNotes = pending;
        _settings.LastSeenVersion = lastSeenToPersist;
        AppSettingsStore.Save(_settings);

        _updateViewModel = new UpdateViewModel(new GitHubReleaseFeed());
        UpdateBanner.DataContext = _updateViewModel;

        _settingsViewModel = new SettingsViewModel(_settings, cameras, lenses, new WpfFontCatalog(), _updateViewModel);
        DataContext = new MainViewModel(cameras, lenses, templatePath, new WpfDialogService(), _settings);

        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        _ = _updateViewModel.CheckAsync();
        if (_pendingNotes.Count == 0) return;
        var window = new WhatsNewWindow(_pendingNotes) { Owner = this };
        window.Closed += (_, _) => DismissPendingNotes();
        window.ShowDialog();
    }

    private void DismissPendingNotes()
    {
        _settings.LastSeenVersion = AppInfo.Version;
        AppSettingsStore.Save(_settings);
    }

    private void OnOpenSettingsClick(object sender, RoutedEventArgs e)
    {
        new SettingsWindow(_settingsViewModel) { Owner = this }.ShowDialog();
    }

    private void OnOpenHelpClick(object sender, RoutedEventArgs e) =>
        new HelpWindow { Owner = this }.Show();

    private void OnOpenWhatsNewClick(object sender, RoutedEventArgs e) =>
        new WhatsNewWindow(ReleaseNotes.All) { Owner = this }.Show();

    private void OnReportIssueClick(object sender, RoutedEventArgs e) =>
        UrlOpener.Open(Links.NewIssueUrl(Environment.OSVersion.VersionString));

    private void OnOpenRepoClick(object sender, RoutedEventArgs e) => UrlOpener.Open(Links.RepoUrl);

    private void OnOpenReleasePageClick(object sender, RoutedEventArgs e)
    {
        if (_updateViewModel.Available is { } found) UrlOpener.Open(Links.ReleaseTagUrl(found.Version));
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
