namespace PhotoForumPPTX.Core;

/// <summary>
/// GitHub Releasesの一覧から、Windows版の新しい版を探す(通信を含まない純粋な判定ロジック)。
/// Mac版は同じリポジトリの v1.x タグを使うため、Windows版は win-v プレフィックスで明確に区別する。
/// </summary>
public static class UpdateCheck
{
    public const string TagPrefix = "win-v";
    public const string AssetName = "PhotoForumPPTX-win.zip";

    public sealed record Found(string Version, string ZipUrl);

    /// <summary>現在より新しいwin-vタグの中で最も新しいものを返す。無ければnull</summary>
    public static Found? FindUpdate(IEnumerable<GitHubRelease> releases, string currentVersion)
    {
        GitHubRelease? newestRelease = null;
        string? newestVersion = null;

        foreach (var release in releases)
        {
            if (!release.TagName.StartsWith(TagPrefix, StringComparison.Ordinal)) continue;
            var version = release.TagName[TagPrefix.Length..];
            if (!ReleaseNotes.IsNewer(version, currentVersion)) continue;
            if (newestVersion == null || ReleaseNotes.IsNewer(version, newestVersion))
            {
                newestVersion = version;
                newestRelease = release;
            }
        }

        if (newestRelease == null || newestVersion == null) return null;
        var asset = newestRelease.Assets.FirstOrDefault(a => a.Name == AssetName);
        return asset == null ? null : new Found(newestVersion, asset.DownloadUrl);
    }
}
