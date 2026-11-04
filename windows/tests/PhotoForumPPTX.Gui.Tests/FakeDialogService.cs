using PhotoForumPPTX.Gui;

namespace PhotoForumPPTX.Gui.Tests;

/// <summary>ネイティブダイアログを介さずにMainViewModelのロジックだけを検証するためのテスト用実装</summary>
public sealed class FakeDialogService : IDialogService
{
    public string[]? PhotosToReturn { get; set; }
    public string? SaveLocationToReturn { get; set; }
    public string? OpenedFile { get; private set; }
    public string? RevealedFile { get; private set; }

    public string[]? PickPhotos() => PhotosToReturn;
    public string? PickSaveLocation(string suggestedFileName) => SaveLocationToReturn;
    public void OpenFile(string path) => OpenedFile = path;
    public void RevealInExplorer(string path) => RevealedFile = path;
}
