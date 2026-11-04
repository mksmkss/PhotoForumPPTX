using PhotoForumPPTX.Core;

namespace PhotoForumPPTX.Gui;

/// <summary>GitHub Releases一覧の取得をViewから抽象化する(テストでは実際の通信をせず偽データを注入する)</summary>
public interface IReleaseFeed
{
    Task<IReadOnlyList<GitHubRelease>> FetchAsync();
}
