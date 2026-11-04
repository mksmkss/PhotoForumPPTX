using PhotoForumPPTX.Core;
using PhotoForumPPTX.Gui;

namespace PhotoForumPPTX.Gui.Tests;

public class MapTableViewModelTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "pfp-maptable-test-" + Guid.NewGuid());

    public MapTableViewModelTests() => Directory.CreateDirectory(_dir);

    public void Dispose()
    {
        if (Directory.Exists(_dir)) Directory.Delete(_dir, true);
    }

    private MapTable CreateTable() => new("cameras.json",
        new[] { new MapEntry("ILCE-7M4", "α7 IV") }, version: 1, storageDir: _dir);

    [Fact]
    public void Entries_AreLoadedFromTable()
    {
        var vm = new MapTableViewModel(CreateTable(), "caption");
        Assert.Single(vm.Entries);
        Assert.Equal("ILCE-7M4", vm.Entries[0].Key);
        Assert.Equal("α7 IV", vm.Entries[0].Value);
    }

    [Fact]
    public void AddEntry_InsertsEditableRowAtTop()
    {
        var vm = new MapTableViewModel(CreateTable(), "caption");
        vm.AddEntryCommand.Execute(null);

        Assert.Equal(2, vm.Entries.Count);
        Assert.Equal("", vm.Entries[0].Key);
    }

    [Fact]
    public void EditingEntry_PersistsToTableFile()
    {
        var table = CreateTable();
        var vm = new MapTableViewModel(table, "caption");
        vm.Entries[0].Value = "α7 IV (改)";

        var reloaded = new MapTable("cameras.json", new[] { new MapEntry("ILCE-7M4", "α7 IV") }, version: 1, storageDir: _dir);
        Assert.Equal("α7 IV (改)", reloaded.Lookup("ILCE-7M4"));
    }

    [Fact]
    public void RemoveEntry_RemovesAndPersists()
    {
        var table = CreateTable();
        var vm = new MapTableViewModel(table, "caption");
        vm.RemoveEntryCommand.Execute(vm.Entries[0]);

        Assert.Empty(vm.Entries);
        var reloaded = new MapTable("cameras.json", new[] { new MapEntry("ILCE-7M4", "α7 IV") }, version: 1, storageDir: _dir);
        Assert.Null(reloaded.Lookup("ILCE-7M4"));
    }

    [Fact]
    public void ResetEntries_RestoresDefaults()
    {
        var table = CreateTable();
        var vm = new MapTableViewModel(table, "caption");
        vm.Entries[0].Value = "改変済み";
        vm.AddEntryCommand.Execute(null);
        Assert.Equal(2, vm.Entries.Count);

        vm.ResetEntriesCommand.Execute(null);

        Assert.Single(vm.Entries);
        Assert.Equal("α7 IV", vm.Entries[0].Value);
    }
}
