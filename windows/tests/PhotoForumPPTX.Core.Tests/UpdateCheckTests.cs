using PhotoForumPPTX.Core;

namespace PhotoForumPPTX.Core.Tests;

public class UpdateCheckTests
{
    private static GitHubRelease Release(string tag, params (string Name, string Url)[] assets) =>
        new(tag, assets.Select(a => new GitHubReleaseAsset(a.Name, a.Url)).ToList());

    [Fact]
    public void NoWinTags_ReturnsNull()
    {
        var releases = new[] { Release("v1.6", ("PhotoForumPPTX.zip", "https://example.com/mac.zip")) };
        Assert.Null(UpdateCheck.FindUpdate(releases, "0.1"));
    }

    [Fact]
    public void OlderOrEqualWinTag_ReturnsNull()
    {
        var releases = new[] { Release("win-v0.1", ("PhotoForumPPTX-win.zip", "https://example.com/win01.zip")) };
        Assert.Null(UpdateCheck.FindUpdate(releases, "0.1"));
    }

    [Fact]
    public void NewerWinTag_ReturnsFound()
    {
        var releases = new[] { Release("win-v0.2", ("PhotoForumPPTX-win.zip", "https://example.com/win02.zip")) };
        var found = UpdateCheck.FindUpdate(releases, "0.1");

        Assert.NotNull(found);
        Assert.Equal("0.2", found!.Version);
        Assert.Equal("https://example.com/win02.zip", found.ZipUrl);
    }

    [Fact]
    public void MultipleNewerWinTags_PicksTheNewest()
    {
        var releases = new[]
        {
            Release("win-v0.2", ("PhotoForumPPTX-win.zip", "https://example.com/win02.zip")),
            Release("win-v0.3", ("PhotoForumPPTX-win.zip", "https://example.com/win03.zip")),
            Release("v1.7", ("PhotoForumPPTX.zip", "https://example.com/mac.zip")), // Mac版タグは無視
        };

        var found = UpdateCheck.FindUpdate(releases, "0.1");

        Assert.Equal("0.3", found!.Version);
        Assert.Equal("https://example.com/win03.zip", found.ZipUrl);
    }

    [Fact]
    public void NewerTagWithoutExpectedAsset_ReturnsNull()
    {
        var releases = new[] { Release("win-v0.2", ("readme.txt", "https://example.com/readme.txt")) };
        Assert.Null(UpdateCheck.FindUpdate(releases, "0.1"));
    }

    [Fact]
    public void EmptyReleaseList_ReturnsNull()
    {
        Assert.Null(UpdateCheck.FindUpdate(Array.Empty<GitHubRelease>(), "0.1"));
    }
}
