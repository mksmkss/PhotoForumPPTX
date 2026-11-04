using System.Windows;
using PhotoForumPPTX.Core;
using PhotoForumPPTX.Gui;

namespace PhotoForumPPTX.App;

public partial class SettingsWindow : Window
{
    public SettingsWindow(SettingsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void OnWhatsNewFromSettingsClick(object sender, RoutedEventArgs e) =>
        new WhatsNewWindow(ReleaseNotes.All) { Owner = this }.Show();

    private void OnHelpFromSettingsClick(object sender, RoutedEventArgs e) =>
        new HelpWindow { Owner = this }.Show();

    private void OnReportIssueFromSettingsClick(object sender, RoutedEventArgs e) =>
        UrlOpener.Open(Links.NewIssueUrl(Environment.OSVersion.VersionString));

    private void OnOpenRepoFromSettingsClick(object sender, RoutedEventArgs e) => UrlOpener.Open(Links.RepoUrl);
}
