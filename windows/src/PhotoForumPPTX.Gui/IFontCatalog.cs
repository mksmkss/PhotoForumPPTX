namespace PhotoForumPPTX.Gui;

/// <summary>システムにインストールされているフォント名の一覧取得をViewから抽象化する</summary>
public interface IFontCatalog
{
    IReadOnlyList<string> GetFamilyNames();
}
