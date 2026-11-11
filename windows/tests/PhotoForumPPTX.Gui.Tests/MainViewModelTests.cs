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

    private MainViewModel CreateViewModel(out FakeDialogService dialog) => CreateViewModel(out dialog, out _);

    private MainViewModel CreateViewModel(out FakeDialogService dialog, out AppSettings settings)
    {
        dialog = new FakeDialogService();
        settings = new AppSettings();
        var cameras = new MapTable("cameras.json", DefaultTables.Cameras, DefaultTables.Version, storageDir: _dir);
        var lenses = new MapTable("lenses.json", DefaultTables.Lenses, DefaultTables.Version, storageDir: _dir);
        return new MainViewModel(cameras, lenses, _templatePath, dialog, settings, _dir);
    }

    private static readonly FieldKind[] SlideOrder =
    {
        FieldKind.StudentId, FieldKind.Name, FieldKind.Title,
        FieldKind.Focal,
        FieldKind.Camera, FieldKind.Lens,
        FieldKind.Shutter, FieldKind.Aperture, FieldKind.Iso, FieldKind.Note,
    };

    [Fact]
    public void DefaultFields_EnabledStatesMatchMacDefaults()
    {
        var vm = CreateViewModel(out _);
        var enabled = vm.Fields.Where(f => f.Enabled).Select(f => f.Kind).ToHashSet();
        var disabled = vm.Fields.Where(f => !f.Enabled).Select(f => f.Kind).ToHashSet();

        Assert.Equal(new HashSet<FieldKind> { FieldKind.Camera, FieldKind.Lens, FieldKind.Focal, FieldKind.Shutter, FieldKind.Aperture, FieldKind.Iso }, enabled);
        Assert.Equal(new HashSet<FieldKind> { FieldKind.StudentId, FieldKind.Name, FieldKind.Title, FieldKind.Note }, disabled);
    }

    [Fact]
    public void DefaultFields_AreInSlideOrder()
    {
        var vm = CreateViewModel(out _);

        Assert.Equal(SlideOrder, vm.Fields.Select(f => f.Kind));
    }

    [Fact]
    public void LoadFields_SortsLegacyOrderIntoSlideOrder_KeepingOrderWithinGroup_AndSavesIt()
    {
        // 旧版で自由に並べた状態(グループをまたぐ並び。レンズ→カメラ、ISO→SSはグループ内の並べ替え)
        var settings = new AppSettings
        {
            Fields = new List<FieldSetting>
            {
                new(FieldKind.Lens, true), new(FieldKind.Camera, true), new(FieldKind.Focal, true),
                new(FieldKind.Iso, true), new(FieldKind.Shutter, true), new(FieldKind.Aperture, true),
                new(FieldKind.StudentId, false), new(FieldKind.Name, false), new(FieldKind.Title, false), new(FieldKind.Note, false),
            },
        };
        var cameras = new MapTable("cameras.json", DefaultTables.Cameras, DefaultTables.Version, storageDir: _dir);
        var lenses = new MapTable("lenses.json", DefaultTables.Lenses, DefaultTables.Version, storageDir: _dir);

        var vm = new MainViewModel(cameras, lenses, _templatePath, new FakeDialogService(), settings, _dir);

        var expected = new[]
        {
            FieldKind.StudentId, FieldKind.Name, FieldKind.Title,
            FieldKind.Focal,
            FieldKind.Lens, FieldKind.Camera,
            FieldKind.Iso, FieldKind.Shutter, FieldKind.Aperture, FieldKind.Note,
        };
        Assert.Equal(expected, vm.Fields.Select(f => f.Kind));
        Assert.Equal(expected, AppSettingsStore.Load(_dir).Fields.Select(f => f.Kind));
    }

    [Fact]
    public void FieldPositions_HideArrowsAtGroupEdges_AndMarkGroupBoundaries()
    {
        var vm = CreateViewModel(out _);
        FieldSettingViewModel F(FieldKind k) => vm.Fields.First(f => f.Kind == k);

        // 学籍番号・氏名・タイトル
        Assert.False(F(FieldKind.StudentId).CanMoveUp);
        Assert.True(F(FieldKind.StudentId).CanMoveDown);
        Assert.False(F(FieldKind.StudentId).StartsNewGroup);
        Assert.True(F(FieldKind.Name).CanMoveUp);
        Assert.True(F(FieldKind.Name).CanMoveDown);
        Assert.False(F(FieldKind.Title).CanMoveDown);

        // 焦点距離は1項目だけのグループ
        Assert.False(F(FieldKind.Focal).CanMoveUp);
        Assert.False(F(FieldKind.Focal).CanMoveDown);
        Assert.True(F(FieldKind.Focal).StartsNewGroup);

        // カメラ・レンズ
        Assert.True(F(FieldKind.Camera).StartsNewGroup);
        Assert.False(F(FieldKind.Camera).CanMoveUp);
        Assert.False(F(FieldKind.Lens).CanMoveDown);

        // 露出情報・備考
        Assert.True(F(FieldKind.Shutter).StartsNewGroup);
        Assert.False(F(FieldKind.Aperture).StartsNewGroup);
        Assert.False(F(FieldKind.Note).CanMoveDown);
    }

    [Fact]
    public void FieldPositions_UpdateAfterMove()
    {
        var vm = CreateViewModel(out _);
        var camera = vm.Fields.First(f => f.Kind == FieldKind.Camera);
        var lens = vm.Fields.First(f => f.Kind == FieldKind.Lens);

        vm.MoveFieldDownCommand.Execute(camera);

        Assert.True(lens.StartsNewGroup);
        Assert.False(lens.CanMoveUp);
        Assert.True(lens.CanMoveDown);
        Assert.False(camera.StartsNewGroup);
        Assert.True(camera.CanMoveUp);
        Assert.False(camera.CanMoveDown);
    }

    [Fact]
    public void ResetFieldOrder_RestoresSlideOrder_KeepingEnabledStates_AndSaves()
    {
        var vm = CreateViewModel(out _);
        vm.MoveFieldDownCommand.Execute(vm.Fields.First(f => f.Kind == FieldKind.Camera));
        vm.MoveFieldDownCommand.Execute(vm.Fields.First(f => f.Kind == FieldKind.StudentId));
        vm.MoveFieldUpCommand.Execute(vm.Fields.First(f => f.Kind == FieldKind.Note));
        vm.Fields.First(f => f.Kind == FieldKind.Note).Enabled = true;
        vm.Fields.First(f => f.Kind == FieldKind.Iso).Enabled = false;
        Assert.NotEqual(SlideOrder, vm.Fields.Select(f => f.Kind));

        vm.ResetFieldOrderCommand.Execute(null);

        Assert.Equal(SlideOrder, vm.Fields.Select(f => f.Kind));
        Assert.True(vm.Fields.First(f => f.Kind == FieldKind.Note).Enabled);
        Assert.False(vm.Fields.First(f => f.Kind == FieldKind.Iso).Enabled);
        Assert.Equal(SlideOrder, AppSettingsStore.Load(_dir).Fields.Select(f => f.Kind));
        Assert.False(vm.Fields.First(f => f.Kind == FieldKind.StudentId).CanMoveUp);
        Assert.False(vm.Fields.First(f => f.Kind == FieldKind.Lens).CanMoveDown);
    }

    [Theory]
    [InlineData(FieldKind.Title, false)]     // 学籍番号グループの末尾 → 焦点距離の下へは行かない
    [InlineData(FieldKind.Focal, true)]      // 焦点距離 → タイトルの上へは行かない
    [InlineData(FieldKind.Focal, false)]     // 焦点距離 → カメラの下へは行かない
    [InlineData(FieldKind.Camera, true)]     // カメラ → 焦点距離の上へは行かない
    [InlineData(FieldKind.Lens, false)]      // レンズ → SSの下へは行かない
    [InlineData(FieldKind.Shutter, true)]    // SS → レンズの上へは行かない
    [InlineData(FieldKind.StudentId, true)]  // リストの先頭
    [InlineData(FieldKind.Note, false)]      // リストの末尾
    public void MoveField_DoesNotCrossGroupBoundary(FieldKind kind, bool up)
    {
        var vm = CreateViewModel(out _);
        var field = vm.Fields.First(f => f.Kind == kind);

        if (up) vm.MoveFieldUpCommand.Execute(field);
        else vm.MoveFieldDownCommand.Execute(field);

        Assert.Equal(SlideOrder, vm.Fields.Select(f => f.Kind));
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
        var vm = CreateViewModel(out var dialog, out var settings);
        settings.OpenAfterExport = true;
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
        var vm = CreateViewModel(out var dialog, out var settings);
        settings.OpenAfterExport = false;
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

    [Fact]
    public void AddPhotosDialog_RemembersFolderAndOpensThereNextTime()
    {
        var vm = CreateViewModel(out var dialog, out var settings);
        dialog.PhotosToReturn = new[] { _sample1 };

        vm.AddPhotosDialogCommand.Execute(null);
        Assert.Equal("", dialog.PhotosInitialDirectory);

        var folder = Path.GetDirectoryName(_sample1);
        Assert.Equal(folder, settings.LastPhotoDir);
        Assert.Equal(folder, AppSettingsStore.Load(_dir).LastPhotoDir);

        vm.AddPhotosDialogCommand.Execute(null);
        Assert.Equal(folder, dialog.PhotosInitialDirectory);
    }

    [Fact]
    public void AddPhotosDialog_Cancelled_KeepsPreviousFolder()
    {
        var vm = CreateViewModel(out var dialog, out var settings);
        settings.LastPhotoDir = _dir;
        dialog.PhotosToReturn = null;

        vm.AddPhotosDialogCommand.Execute(null);

        Assert.Equal(_dir, settings.LastPhotoDir);
    }

    [Fact]
    public async Task ExportCommand_RemembersFolderAndOpensThereNextTime()
    {
        var vm = CreateViewModel(out var dialog, out var settings);
        vm.AddPhotos(new[] { _sample1 });
        var outDir = Path.Combine(_dir, "out");
        Directory.CreateDirectory(outDir);
        dialog.SaveLocationToReturn = Path.Combine(outDir, "a.pptx");

        await vm.ExportCommand.ExecuteAsync(null);
        Assert.Equal("", dialog.SaveInitialDirectory);
        Assert.Equal(outDir, settings.LastExportDir);
        Assert.Equal(outDir, AppSettingsStore.Load(_dir).LastExportDir);

        await vm.ExportCommand.ExecuteAsync(null);
        Assert.Equal(outDir, dialog.SaveInitialDirectory);
    }

    [Fact]
    public async Task ExportCommand_Cancelled_KeepsPreviousFolder()
    {
        var vm = CreateViewModel(out var dialog, out var settings);
        vm.AddPhotos(new[] { _sample1 });
        settings.LastExportDir = _dir;
        dialog.SaveLocationToReturn = null;

        await vm.ExportCommand.ExecuteAsync(null);

        Assert.Equal(_dir, settings.LastExportDir);
    }

    [Fact]
    public void StudentIdNameAndFields_ArePersistedAndReloaded()
    {
        var vm = CreateViewModel(out _, out var settings);
        vm.StudentId = "12345";
        vm.Name = "三輪太郎";
        vm.StudentIdField.Enabled = true;

        var reloaded = AppSettingsStore.Load(_dir);
        Assert.Equal("12345", reloaded.StudentId);
        Assert.Equal("三輪太郎", reloaded.Name);
        Assert.Contains(reloaded.Fields, f => f.Kind == FieldKind.StudentId && f.Enabled);

        // 同じディレクトリから作った新しいMainViewModelは保存済みの状態を引き継ぐ
        var cameras = new MapTable("cameras.json", DefaultTables.Cameras, DefaultTables.Version, storageDir: _dir);
        var lenses = new MapTable("lenses.json", DefaultTables.Lenses, DefaultTables.Version, storageDir: _dir);
        var vm2 = new MainViewModel(cameras, lenses, _templatePath, new FakeDialogService(), reloaded, _dir);
        Assert.Equal("12345", vm2.StudentId);
        Assert.Equal("三輪太郎", vm2.Name);
        Assert.True(vm2.StudentIdField.Enabled);
    }
}
