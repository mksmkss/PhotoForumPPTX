using PhotoForumPPTX.Core;
using PhotoForumPPTX.Gui;

namespace PhotoForumPPTX.Gui.Tests;

public sealed class FakeReleaseFeed : IReleaseFeed
{
    public IReadOnlyList<GitHubRelease> ReleasesToReturn { get; set; } = Array.Empty<GitHubRelease>();
    public Exception? ExceptionToThrow { get; set; }

    public Task<IReadOnlyList<GitHubRelease>> FetchAsync() =>
        ExceptionToThrow != null ? Task.FromException<IReadOnlyList<GitHubRelease>>(ExceptionToThrow)
                                  : Task.FromResult(ReleasesToReturn);
}
