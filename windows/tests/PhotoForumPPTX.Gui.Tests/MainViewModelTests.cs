using System.IO.Compression;
using PhotoForumPPTX.Core;
using PhotoForumPPTX.Gui;

namespace PhotoForumPPTX.Gui.Tests;

public class MainViewModelTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "pfp-gui-test-" + Guid.NewGuid());
    private readonly string _templatePath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "template.pptx");
    private readonly string _sample1 = Path.Combine(AppContext.BaseDirectory, "Fixtures", "sample1.jpg");
    private readonly string _sample2 = Path.Combine(AppContext.BaseDirectory, "Fixtures", "sample2.jpg");

    public MainViewModelTests() => Directory.CreateDirectory(_dir);

    public void Dispose()
    {
        if (Directory.Exists(_dir)) Directory.Delete(_dir, true);
    }

    private MainViewModel CreateViewModel(out FakeDialogService dialog)
    {
        dialog = new FakeDialogService();
        var cameras = new MapTable("cameras.json", DefaultTables.Cameras, DefaultTables.Version, storageDir: _dir);
        var lenses = new MapTable("lenses.json", DefaultTables.Lenses, DefaultTables.Version, storageDir: _dir);
        return new MainViewModel(cameras, lenses, _templatePath, dialog);
    }

    [Fact]
    public void DefaultFields_MatchMacDefaults()
    {
        var vm = CreateViewModel(out _);
        var enabled = vm.Fields.Where(f => f.Enabled).Select(f => f.Kind).ToList();
        var disabled = vm.Fields.Where(f => !f.Enabled).Select(f => f.Kind).ToList();

        Assert.Equal(new[] { FieldKind.Camera, FieldKind.Lens, FieldKind.Focal, FieldKind.Shutter, FieldKind.Aperture, FieldKind.Iso }, enabled);
        Assert.Equal(new[] { FieldKind.StudentId, FieldKind.Name, FieldKind.Title, FieldKind.Note }, disabled);
    }

    [Fact]
    public void AddPhotos_AddsValidAndSkipsInvalid()
    {
        var vm = CreateViewModel(out _);
        var bogus = Path.Combine(_dir, "not-an-image.jpg");
        File.WriteAllText(bogus, "not an image");

        vm.AddPhotos(new[] { _sample1, bogus });

        Assert.Single(vm.Photos);
        Assert.Equal(_sample1, vm.Photos[0].FilePath);
        Assert.Contains("not-an-image.jpg", vm.Status);
    }

    [Fact]
    public void HasPhotos_And_ExportCanExecute_TrackPhotoCount()
    {
        var vm = CreateViewModel(out _);
        Assert.False(vm.HasPhotos);
        Assert.False(vm.ExportCommand.CanExecute(null));

        vm.AddPhotos(new[] { _sample1 });
        Assert.True(vm.HasPhotos);
        Assert.True(vm.ExportCommand.CanExecute(null));

        vm.RemovePhotoCommand.Execute(vm.Photos[0]);
        Assert.False(vm.HasPhotos);
        Assert.False(vm.ExportCommand.CanExecute(null));
    }

    [Fact]
    public void MovePhoto_ReordersWithinBounds()
    {
        var vm = CreateViewModel(out _);
        vm.AddPhotos(new[] { _sample1, _sample2 });
        var first = vm.Photos[0];
        var second = vm.Photos[1];

        vm.MovePhotoDownCommand.Execute(first);
        Assert.Equal(second, vm.Photos[0]);
        Assert.Equal(first, vm.Photos[1]);

        // 先頭を上に動かそうとしても範囲外なので何も起きない
        vm.MovePhotoUpCommand.Execute(second);
        Assert.Equal(second, vm.Photos[0]);
        Assert.Equal(first, vm.Photos[1]);
    }

    [Fact]
    public void MoveField_Reorders()
    {
        var vm = CreateViewModel(out _);
        var first = vm.Fields[0];
        var second = vm.Fields[1];

        vm.MoveFieldDownCommand.Execute(first);
        Assert.Equal(second, vm.Fields[0]);
        Assert.Equal(first, vm.Fields[1]);
    }

    [Fact]
    public async Task ExportAsync_WithNoPhotos_DoesNothing()
    {
        var vm = CreateViewModel(out var dialog);
        var output = Path.Combine(_dir, "out.pptx");

        await vm.ExportAsync(output);

        Assert.False(File.Exists(output));
        Assert.Null(dialog.OpenedFile);
    }

    [Fact]
    public async Task ExportAsync_ProducesSlidePerPhoto_AndOpensFileWhenConfigured()
    {
        var vm = CreateViewModel(out var dialog);
        vm.OpenAfterExport = true;
        vm.StudentId = "99999";
        vm.Name = "hoge";
        vm.AddPhotos(new[] { _sample1, _sample2 });

        var output = Path.Combine(_dir, "out.pptx");
        await vm.ExportAsync(output);

        Assert.True(File.Exists(output));
        Assert.Equal(output, dialog.OpenedFile);
        Assert.Contains("書き出しました", vm.Status);

        using var zip = ZipFile.OpenRead(output);
        var names = zip.Entries.Select(e => e.FullName.Replace('\\', '/')).ToHashSet();
        Assert.Contains("ppt/slides/slide1.xml", names);
        Assert.Contains("ppt/slides/slide2.xml", names);
        Assert.DoesNotContain("ppt/slides/slide3.xml", names);
    }

    [Fact]
    public async Task ExportAsync_RevealsInExplorerWhenOpenAfterExportIsOff()
    {
        var vm = CreateViewModel(out var dialog);
        vm.OpenAfterExport = false;
        vm.AddPhotos(new[] { _sample1 });

        var output = Path.Combine(_dir, "out.pptx");
        await vm.ExportAsync(output);

        Assert.Equal(output, dialog.RevealedFile);
        Assert.Null(dialog.OpenedFile);
    }

    [Fact]
    public async Task ExportCommand_UsesDialogServiceForSaveLocation()
    {
        var vm = CreateViewModel(out var dialog);
        vm.AddPhotos(new[] { _sample1 });
        dialog.SaveLocationToReturn = Path.Combine(_dir, "via-dialog.pptx");

        await vm.ExportCommand.ExecuteAsync(null);

        Assert.True(File.Exists(dialog.SaveLocationToReturn));
    }

    [Fact]
    public async Task ExportCommand_DoesNothingWhenDialogIsCancelled()
    {
        var vm = CreateViewModel(out var dialog);
        vm.AddPhotos(new[] { _sample1 });
        dialog.SaveLocationToReturn = null;

        await vm.ExportCommand.ExecuteAsync(null);

        Assert.Empty(Directory.GetFiles(_dir, "*.pptx"));
    }
}
