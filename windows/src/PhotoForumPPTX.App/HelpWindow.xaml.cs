using System.Diagnostics;
using System.Windows;
using PhotoForumPPTX.Core;

namespace PhotoForumPPTX.App;

public partial class HelpWindow : Window
{
    public HelpWindow() => InitializeComponent();

    private void OnNewIssueClick(object sender, RoutedEventArgs e) =>
        UrlOpener.Open(Links.NewIssueUrl(Environment.OSVersion.VersionString));

    private void OnIssuesClick(object sender, RoutedEventArgs e) => UrlOpener.Open(Links.IssuesUrl);

    private void OnRepoClick(object sender, RoutedEventArgs e) => UrlOpener.Open(Links.RepoUrl);
}

internal static class UrlOpener
{
    public static void Open(string url) => Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
}
