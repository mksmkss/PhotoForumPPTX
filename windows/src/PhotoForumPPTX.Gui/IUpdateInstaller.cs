namespace PhotoForumPPTX.Gui;

/// <summary>実際の更新適用(ダウンロード→入れ替え→再起動)をViewから抽象化する</summary>
public interface IUpdateInstaller
{
    Task InstallAsync(string zipUrl);
}
