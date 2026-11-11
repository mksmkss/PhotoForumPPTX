namespace PhotoForumPPTX.Gui;

/// <summary>
/// ファイル選択・保存・開く操作をViewから抽象化する。
/// 本番ではWPFのダイアログを使う実装を注入し、テストでは差し替えて
/// ネイティブダイアログを介さずにViewModelのロジックだけを検証できるようにする。
/// </summary>
public interface IDialogService
{
    /// <summary>写真ファイルを選ばせる。initialDirectoryが空・存在しなければ既定の場所で開く。キャンセル時はnull</summary>
    string[]? PickPhotos(string? initialDirectory);

    /// <summary>保存先を選ばせる。initialDirectoryが空・存在しなければ既定の場所で開く。キャンセル時はnull</summary>
    string? PickSaveLocation(string suggestedFileName, string? initialDirectory);

    /// <summary>ファイルを既定のアプリで開く</summary>
    void OpenFile(string path);

    /// <summary>エクスプローラーでファイルを選択表示する</summary>
    void RevealInExplorer(string path);
}
