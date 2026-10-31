using PhotoForumPPTX.Gui;

namespace PhotoForumPPTX.Gui.Tests;

public sealed class FakeUpdateInstaller : IUpdateInstaller
{
    public string? InstalledZipUrl { get; private set; }
    public Exception? ExceptionToThrow { get; set; }
    public TaskCompletionSource<bool>? Gate { get; set; }

    public async Task InstallAsync(string zipUrl)
    {
        if (Gate != null) await Gate.Task;
        if (ExceptionToThrow != null) throw ExceptionToThrow;
        InstalledZipUrl = zipUrl;
    }
}
