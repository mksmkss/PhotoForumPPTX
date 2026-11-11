using PhotoForumPPTX.Gui;

namespace PhotoForumPPTX.Gui.Tests;

/// <summary>ネイティブダイアログを介さずにMainViewModelのロジックだけを検証するためのテスト用実装</summary>
public sealed class FakeDialogService : IDialogService
{
    public string[]? PhotosToReturn { get; set; }
    public string? SaveLocationToReturn { get; set; }
    public string? OpenedFile { get; private set; }
    public string? RevealedFile { get; private set; }

    /// <summary>直近のダイアログに渡された初期フォルダ</summary>
    public string? PhotosInitialDirectory { get; private set; }
    public string? SaveInitialDirectory { get; private set; }

    public string[]? PickPhotos(string? initialDirectory)
    {
        PhotosInitialDirectory = initialDirectory;
        return PhotosToReturn;
    }

    public string? PickSaveLocation(string suggestedFileName, string? initialDirectory)
    {
        SaveInitialDirectory = initialDirectory;
        return SaveLocationToReturn;
    }

    public void OpenFile(string path) => OpenedFile = path;
    public void RevealInExplorer(string path) => RevealedFile = path;
}
