using PhotoForumPPTX.Core;

namespace PhotoForumPPTX.Core.Tests;

public class AppSettingsStoreTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "pfp-settings-store-test-" + Guid.NewGuid());

    public void Dispose()
    {
        if (Directory.Exists(_dir)) Directory.Delete(_dir, true);
        if (File.Exists(_dir)) File.Delete(_dir);
    }

    [Fact]
    public void Load_WithNoFile_ReturnsDefaults()
    {
        var settings = AppSettingsStore.Load(_dir);
        Assert.Equal("", settings.StudentId);
        Assert.True(settings.OpenAfterExport);
        Assert.Equal(1.0, settings.LineSpacing);
    }

    [Fact]
    public void Exists_ReflectsWhetherSettingsFileWasEverSaved()
    {
        Assert.False(AppSettingsStore.Exists(_dir));
        AppSettingsStore.Save(new AppSettings(), _dir);
        Assert.True(AppSettingsStore.Exists(_dir));
    }

    [Fact]
    public void SaveThenLoad_RoundTripsAllFields()
    {
        var settings = new AppSettings
        {
            StudentId = "12345",
            Name = "三輪太郎",
            Fields = new List<FieldSetting> { new(FieldKind.Camera, true), new(FieldKind.Note, false) },
            FontLatin = "Calibri",
            FontEa = "游ゴシック",
            LineSpacing = 1.3,
            OpenAfterExport = false,
        };

        AppSettingsStore.Save(settings, _dir);
        var loaded = AppSettingsStore.Load(_dir);

        Assert.Equal(settings.StudentId, loaded.StudentId);
        Assert.Equal(settings.Name, loaded.Name);
        Assert.Equal(settings.Fields, loaded.Fields);
        Assert.Equal(settings.FontLatin, loaded.FontLatin);
        Assert.Equal(settings.FontEa, loaded.FontEa);
        Assert.Equal(settings.LineSpacing, loaded.LineSpacing);
        Assert.Equal(settings.OpenAfterExport, loaded.OpenAfterExport);
    }

    [Fact]
    public void Save_DoesNotLeaveTemporaryFileBehind()
    {
        AppSettingsStore.Save(new AppSettings(), _dir);

        var files = Directory.GetFiles(_dir);
        Assert.Contains(files, f => f.EndsWith("settings.json"));
        Assert.DoesNotContain(files, f => f.EndsWith(".tmp"));
    }

    [Fact]
    public void Load_WithCorruptFile_FallsBackToDefaults()
    {
        Directory.CreateDirectory(_dir);
        File.WriteAllText(Path.Combine(_dir, "settings.json"), "{ this is not valid json");

        var loaded = AppSettingsStore.Load(_dir);

        Assert.Equal("", loaded.StudentId);
    }

    [Fact]
    public void Save_DoesNotThrow_WhenDirectoryCannotBeCreated()
    {
        // _dirと同名の"ディレクトリ"が実はファイルである状況を作り、Directory.CreateDirectoryを確実に失敗させる
        File.WriteAllText(_dir, "not a directory");

        var exception = Record.Exception(() => AppSettingsStore.Save(new AppSettings { StudentId = "x" }, _dir));

        Assert.Null(exception);
    }
}
