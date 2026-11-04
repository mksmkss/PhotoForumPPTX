namespace PhotoForumPPTX.Core;

public sealed record GitHubReleaseAsset(string Name, string DownloadUrl);

public sealed record GitHubRelease(string TagName, IReadOnlyList<GitHubReleaseAsset> Assets);
