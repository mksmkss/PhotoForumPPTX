using System.Diagnostics;
using System.IO;
using Microsoft.Win32;
using PhotoForumPPTX.Gui;

namespace PhotoForumPPTX.App;

public sealed class WpfDialogService : IDialogService
{
    public string[]? PickPhotos()
    {
        var dialog = new OpenFileDialog
        {
            Title = "写真を選択",
            Multiselect = true,
            Filter = "画像ファイル|*.jpg;*.jpeg;*.png;*.bmp;*.tif;*.tiff|すべてのファイル|*.*",
        };
        return dialog.ShowDialog() == true ? dialog.FileNames : null;
    }

    public string? PickSaveLocation(string suggestedFileName)
    {
        var downloads = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
        var dialog = new SaveFileDialog
        {
            Title = "書き出し先を選択",
            FileName = suggestedFileName,
            Filter = "PowerPoint プレゼンテーション|*.pptx",
            InitialDirectory = Directory.Exists(downloads) ? downloads : Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        };
        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    public void OpenFile(string path) =>
        Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });

    public void RevealInExplorer(string path) =>
        Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{path}\"") { UseShellExecute = true });
}
