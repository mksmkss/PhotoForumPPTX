using PhotoForumPPTX.Core;

namespace PhotoForumPPTX.Gui;

public static class WhatsNewCoordinator
{
    /// <summary>
    /// 起動時に見せるべき新機能ポップアップの内容を決める(ファイルI/Oを含まない純粋な判定ロジック)。
    /// 新規インストールでは表示しない。既存ユーザーで記録が無ければ、現在の全履歴を表示対象とする。
    /// </summary>
    /// <param name="storedLastSeenVersion">AppSettings.LastSeenVersion(空なら未記録)</param>
    /// <param name="hadExistingSettings">このアプリの設定ファイルが以前から存在したか(= 既存ユーザーか)</param>
    public static (IReadOnlyList<ReleaseNote> Pending, string LastSeenVersionToPersist) Compute(
        string storedLastSeenVersion, bool hadExistingSettings)
    {
        var last = !string.IsNullOrEmpty(storedLastSeenVersion)
            ? storedLastSeenVersion
            : hadExistingSettings ? "0.0" : AppInfo.Version;

        var pending = ReleaseNotes.Between(last, AppInfo.Version);
        var toPersist = pending.Count > 0 ? last : AppInfo.Version;
        return (pending, toPersist);
    }
}
