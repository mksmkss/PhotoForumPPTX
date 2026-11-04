using PhotoForumPPTX.Core;

namespace PhotoForumPPTX.Core.Tests;

public class ReleaseNotesTests
{
    [Theory]
    [InlineData("1.2", "1.1", true)]
    [InlineData("1.1", "1.2", false)]
    [InlineData("1.1", "1.1", false)]
    [InlineData("2.0", "1.9", true)]
    [InlineData("0.1", "0.0", true)]
    public void IsNewer_ComparesDottedVersions(string a, string b, bool expected)
    {
        Assert.Equal(expected, ReleaseNotes.IsNewer(a, b));
    }

    [Fact]
    public void Between_ExcludesLastButIncludesUpToCurrent()
    {
        var notes = ReleaseNotes.Between("0.0", AppInfo.Version);
        Assert.Contains(notes, n => n.Version == AppInfo.Version);
    }

    [Fact]
    public void Between_ExcludesAlreadySeenVersion()
    {
        var notes = ReleaseNotes.Between(AppInfo.Version, AppInfo.Version);
        Assert.Empty(notes);
    }
}
