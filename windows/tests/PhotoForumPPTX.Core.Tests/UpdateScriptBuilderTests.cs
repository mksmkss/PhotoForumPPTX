using PhotoForumPPTX.Core;

namespace PhotoForumPPTX.Core.Tests;

public class UpdateScriptBuilderTests
{
    [Fact]
    public void Build_IncludesProcessIdAndAllPaths()
    {
        var script = UpdateScriptBuilder.Build(
            waitForProcessId: 1234,
            sourceDir: @"C:\temp\extracted",
            destDir: @"C:\app",
            destExePath: @"C:\app\PhotoForumPPTX.App.exe",
            workDir: @"C:\temp\work");

        Assert.Contains("Wait-Process -Id 1234", script);
        Assert.Contains("\"C:\\temp\\extracted\" \"C:\\app\"", script);
        Assert.Contains("\"C:\\app\\PhotoForumPPTX.App.exe\"", script);
        Assert.Contains("\"C:\\temp\\work\"", script);
    }

    [Fact]
    public void Build_UsesRobocopyMirrorMode()
    {
        var script = UpdateScriptBuilder.Build(1, "src", "dst", "dst/app.exe", "work");

        Assert.Contains("robocopy", script);
        Assert.Contains("/MIR", script);
    }

    [Fact]
    public void Build_RelaunchesAndCleansUpWorkDirAfterCopy()
    {
        var script = UpdateScriptBuilder.Build(1, "src", "dst", "dst/app.exe", "work");
        var lines = script.Split('\n').Select(l => l.Trim()).ToList();

        var copyIndex = lines.FindIndex(l => l.StartsWith("robocopy"));
        var relaunchIndex = lines.FindIndex(l => l.StartsWith("Start-Process"));
        var cleanupIndex = lines.FindIndex(l => l.StartsWith("Remove-Item"));

        Assert.True(copyIndex >= 0 && relaunchIndex > copyIndex && cleanupIndex > relaunchIndex,
            "robocopy → Start-Process → Remove-Item の順になっている必要がある");
    }
}
