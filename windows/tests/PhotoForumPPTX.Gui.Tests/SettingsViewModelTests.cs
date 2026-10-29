using PhotoForumPPTX.Core;
using PhotoForumPPTX.Gui;

namespace PhotoForumPPTX.Gui.Tests;

public class SettingsViewModelTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "pfp-settings-test-" + Guid.NewGuid());

    public SettingsViewModelTests() => Directory.CreateDirectory(_dir);

    public void Dispose()
    {
        if (Directory.Exists(_dir)) Directory.Delete(_dir, true);
    }

    private SettingsViewModel CreateViewModel(AppSettings? settings = null, IFontCatalog? fontCatalog = null)
    {
        settings ??= new AppSettings();
        var cameras = new MapTable("cameras.json", DefaultTables.Cameras, DefaultTables.Version, storageDir: _dir);
        var lenses = new MapTable("lenses.json", DefaultTables.Lenses, DefaultTables.Version, storageDir: _dir);
        var update = new UpdateViewModel(new FakeReleaseFeed());
        return new SettingsViewModel(settings, cameras, lenses, fontCatalog ?? new FakeFontCatalog("Arial", "Calibri"), update, _dir);
    }

    [Fact]
    public void InitialValues_ComeFromSettings()
    {
        var settings = new AppSettings { FontLatin = "Calibri", FontEa = "游ゴシック", LineSpacing = 1.2, OpenAfterExport = false };
        var vm = CreateViewModel(settings);

        Assert.Equal("Calibri", vm.FontLatin);
        Assert.Equal("游ゴシック", vm.FontEa);
        Assert.Equal(1.2, vm.LineSpacing);
        Assert.False(vm.OpenAfterExport);
    }

    [Fact]
    public void AvailableFonts_IncludesCurrentValueEvenIfNotInstalled()
    {
        var settings = new AppSettings { FontEa = "ＭＳ Ｐゴシック" };
        var vm = CreateViewModel(settings, new FakeFontCatalog("Arial", "Calibri"));

        Assert.Contains("ＭＳ Ｐゴシック", vm.AvailableEaFonts);
        Assert.Contains("Arial", vm.AvailableLatinFonts);
    }

    [Fact]
    public void ChangingFont_PersistsToSettingsFile()
    {
        var vm = CreateViewModel();
        vm.FontLatin = "Calibri";

        var reloaded = AppSettingsStore.Load(_dir);
        Assert.Equal("Calibri", reloaded.FontLatin);
    }

    [Fact]
    public void ResetFonts_RestoresAppDefaults()
    {
        var vm = CreateViewModel();
        vm.FontLatin = "Calibri";
        vm.FontEa = "游ゴシック";

        vm.ResetFontsCommand.Execute(null);

        Assert.Equal(SlideFonts.Default.Latin, vm.FontLatin);
        Assert.Equal(SlideFonts.Default.Ea, vm.FontEa);
    }

    [Fact]
    public void ResetLineSpacing_SetsTo100Percent()
    {
        var vm = CreateViewModel();
        vm.LineSpacing = 1.5;

        vm.ResetLineSpacingCommand.Execute(null);

        Assert.Equal(1.0, vm.LineSpacing);
    }

    [Fact]
    public void ChangingOpenAfterExport_Persists()
    {
        var vm = CreateViewModel();
        vm.OpenAfterExport = false;

        var reloaded = AppSettingsStore.Load(_dir);
        Assert.False(reloaded.OpenAfterExport);
    }

    [Fact]
    public void CameraAndLensTables_AreExposed()
    {
        var vm = CreateViewModel();
        Assert.NotEmpty(vm.CameraTable.Entries);
        Assert.NotEmpty(vm.LensTable.Entries);
    }
}
