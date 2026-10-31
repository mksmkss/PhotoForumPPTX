using PhotoForumPPTX.Core;
using PhotoForumPPTX.Gui;

namespace PhotoForumPPTX.Gui.Tests;

public class UpdateViewModelTests
{
    private static UpdateViewModel CreateViewModel(FakeReleaseFeed? feed = null, FakeUpdateInstaller? installer = null) =>
        new(feed ?? new FakeReleaseFeed(), installer ?? new FakeUpdateInstaller());

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
        var vm = CreateViewModel(feed);

        await vm.CheckAsync();

        Assert.NotNull(vm.Available);
        Assert.Equal("9.9", vm.Available!.Version);
    }

    [Fact]
    public async Task CheckAsync_WithNoNewerRelease_LeavesAvailableNull()
    {
        var vm = CreateViewModel();

        await vm.CheckAsync();

        Assert.Null(vm.Available);
    }

    [Fact]
    public async Task CheckAsync_Manual_SetsStatusMessage()
    {
        var vm = CreateViewModel();

        await vm.CheckAsync(manual: true);

        Assert.Contains("最新版です", vm.Status);
    }

    [Fact]
    public async Task CheckAsync_AutomaticWithNoUpdate_DoesNotSetStatus()
    {
        var vm = CreateViewModel();

        await vm.CheckAsync(manual: false);

        Assert.Equal("", vm.Status);
    }

    [Fact]
    public async Task CheckAsync_NetworkFailure_DoesNotThrow_AndReportsStatusWhenManual()
    {
        var feed = new FakeReleaseFeed { ExceptionToThrow = new HttpRequestException("timeout") };
        var vm = CreateViewModel(feed);

        await vm.CheckAsync(manual: true);

        Assert.Null(vm.Available);
        Assert.Contains("失敗", vm.Status);
    }

    [Fact]
    public void DismissCommand_ClearsAvailable()
    {
        var vm = CreateViewModel();
        vm.Available = new UpdateCheck.Found("9.9", "https://example.com/win.zip");

        vm.DismissCommand.Execute(null);

        Assert.Null(vm.Available);
    }

    [Fact]
    public async Task InstallCommand_WithNoAvailableUpdate_DoesNothing()
    {
        var installer = new FakeUpdateInstaller();
        var vm = CreateViewModel(installer: installer);

        await vm.InstallCommand.ExecuteAsync(null);

        Assert.Null(installer.InstalledZipUrl);
    }

    [Fact]
    public async Task InstallCommand_CallsInstallerWithZipUrl()
    {
        var installer = new FakeUpdateInstaller();
        var vm = CreateViewModel(installer: installer);
        vm.Available = new UpdateCheck.Found("9.9", "https://example.com/win.zip");

        await vm.InstallCommand.ExecuteAsync(null);

        Assert.Equal("https://example.com/win.zip", installer.InstalledZipUrl);
        Assert.False(vm.Installing);
    }

    [Fact]
    public async Task InstallCommand_SetsInstallingWhileRunning()
    {
        var gate = new TaskCompletionSource<bool>();
        var installer = new FakeUpdateInstaller { Gate = gate };
        var vm = CreateViewModel(installer: installer);
        vm.Available = new UpdateCheck.Found("9.9", "https://example.com/win.zip");

        var task = vm.InstallCommand.ExecuteAsync(null);
        Assert.True(vm.Installing);

        gate.SetResult(true);
        await task;
        Assert.False(vm.Installing);
    }

    [Fact]
    public async Task InstallCommand_Failure_SetsStatus_AndClearsInstalling()
    {
        var installer = new FakeUpdateInstaller { ExceptionToThrow = new InvalidOperationException("書き込めません") };
        var vm = CreateViewModel(installer: installer);
        vm.Available = new UpdateCheck.Found("9.9", "https://example.com/win.zip");

        await vm.InstallCommand.ExecuteAsync(null);

        Assert.False(vm.Installing);
        Assert.Contains("失敗", vm.Status);
    }
}
