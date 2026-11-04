using PhotoForumPPTX.Core;

namespace PhotoForumPPTX.Core.Tests;

public class MapTableTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "pfp-test-" + Guid.NewGuid());

    public void Dispose()
    {
        if (Directory.Exists(_dir)) Directory.Delete(_dir, true);
    }

    [Fact]
    public void Lookup_IsCaseAndWhitespaceInsensitive()
    {
        var table = new MapTable("cameras.json", new[] { new MapEntry("ILCE-7M4", "α7 IV") }, version: 1, storageDir: _dir);
        Assert.Equal("α7 IV", table.Lookup("  ilce-7m4  "));
        Assert.Null(table.Lookup("unknown"));
    }

    [Fact]
    public void NewDefaults_AreMergedWhenVersionIncreases()
    {
        var v1 = new MapTable("lenses.json", new[] { new MapEntry("A", "1") }, version: 1, storageDir: _dir);
        v1.Save();

        var v2 = new MapTable("lenses.json", new[] { new MapEntry("A", "1"), new MapEntry("B", "2") }, version: 2, storageDir: _dir);
        Assert.Equal("1", v2.Lookup("A"));
        Assert.Equal("2", v2.Lookup("B"));
    }

    [Fact]
    public void RemovedEntry_DoesNotComeBackWhileVersionUnchanged()
    {
        var v1 = new MapTable("lenses.json", new[] { new MapEntry("A", "1"), new MapEntry("B", "2") }, version: 1, storageDir: _dir);
        v1.Entries.RemoveAll(e => e.Key == "B");
        v1.Save();

        var reopened = new MapTable("lenses.json", new[] { new MapEntry("A", "1"), new MapEntry("B", "2") }, version: 1, storageDir: _dir);
        Assert.Null(reopened.Lookup("B"));
    }

    [Fact]
    public void Save_DoesNotLeaveTemporaryFileBehind()
    {
        var table = new MapTable("cameras.json", new[] { new MapEntry("A", "1") }, version: 1, storageDir: _dir);
        table.Save();

        var files = Directory.GetFiles(_dir);
        Assert.Contains(files, f => f.EndsWith("cameras.json"));
        Assert.DoesNotContain(files, f => f.EndsWith(".tmp"));
    }
}
