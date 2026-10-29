using PhotoForumPPTX.Core;
using PhotoForumPPTX.Gui;

namespace PhotoForumPPTX.Gui.Tests;

public class UpdateViewModelTests
{
    [Fact]
    public async Task CheckAsync_WithNewerRelease_SetsAvailable()
    {
        var feed = new FakeReleaseFeed
        {
            ReleasesToReturn = new[]
            {
                new GitHubRelease("win-v9.9", new[] { new GitHubReleaseAsset(UpdateCheck.AssetName, "https://example.com/win.zip") }),
            },
        };
        var vm = new UpdateViewModel(feed);

        await vm.CheckAsync();

        Assert.NotNull(vm.Available);
        Assert.Equal("9.9", vm.Available!.Version);
    }

    [Fact]
    public async Task CheckAsync_WithNoNewerRelease_LeavesAvailableNull()
    {
        var vm = new UpdateViewModel(new FakeReleaseFeed());

        await vm.CheckAsync();

        Assert.Null(vm.Available);
    }

    [Fact]
    public async Task CheckAsync_Manual_SetsStatusMessage()
    {
        var vm = new UpdateViewModel(new FakeReleaseFeed());

        await vm.CheckAsync(manual: true);

        Assert.Contains("最新版です", vm.Status);
    }

    [Fact]
    public async Task CheckAsync_AutomaticWithNoUpdate_DoesNotSetStatus()
    {
        var vm = new UpdateViewModel(new FakeReleaseFeed());

        await vm.CheckAsync(manual: false);

        Assert.Equal("", vm.Status);
    }

    [Fact]
    public async Task CheckAsync_NetworkFailure_DoesNotThrow_AndReportsStatusWhenManual()
    {
        var feed = new FakeReleaseFeed { ExceptionToThrow = new HttpRequestException("timeout") };
        var vm = new UpdateViewModel(feed);

        await vm.CheckAsync(manual: true);

        Assert.Null(vm.Available);
        Assert.Contains("失敗", vm.Status);
    }

    [Fact]
    public void DismissCommand_ClearsAvailable()
    {
        var vm = new UpdateViewModel(new FakeReleaseFeed());
        vm.Available = new UpdateCheck.Found("9.9", "https://example.com/win.zip");

        vm.DismissCommand.Execute(null);

        Assert.Null(vm.Available);
    }
}
