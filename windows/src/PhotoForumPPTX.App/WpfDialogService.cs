using System.Diagnostics;
using System.IO;
using Microsoft.Win32;
using PhotoForumPPTX.Gui;

namespace PhotoForumPPTX.App;

public sealed class WpfDialogService : IDialogService
{
    public string[]? PickPhotos(string? initialDirectory)
    {
        var dialog = new OpenFileDialog
        {
            Title = "写真を選択",
            Multiselect = true,
            Filter = "画像ファイル|*.jpg;*.jpeg;*.png;*.bmp;*.tif;*.tiff|すべてのファイル|*.*",
        };
        if (ExistingDirectory(initialDirectory) is { } dir) dialog.InitialDirectory = dir;
        return dialog.ShowDialog() == true ? dialog.FileNames : null;
    }

    public string? PickSaveLocation(string suggestedFileName, string? initialDirectory)
    {
        var downloads = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
        var dialog = new SaveFileDialog
        {
            Title = "書き出し先を選択",
            FileName = suggestedFileName,
            Filter = "PowerPoint プレゼンテーション|*.pptx",
            InitialDirectory = ExistingDirectory(initialDirectory)
                ?? (Directory.Exists(downloads) ? downloads : Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)),
        };
        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    /// <summary>前回のフォルダが削除・移動されていたら使わない(既定の場所に戻す)</summary>
    private static string? ExistingDirectory(string? path) =>
        !string.IsNullOrEmpty(path) && Directory.Exists(path) ? path : null;

    public void OpenFile(string path) =>
        Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });

    public void RevealInExplorer(string path) =>
        Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{path}\"") { UseShellExecute = true });
}
