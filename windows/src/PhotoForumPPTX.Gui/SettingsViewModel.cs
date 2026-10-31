using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PhotoForumPPTX.Core;

namespace PhotoForumPPTX.Gui;

public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly AppSettings _settings;
    private readonly IFontCatalog _fontCatalog;
    private readonly string? _settingsDir;

    public MapTableViewModel CameraTable { get; }
    public MapTableViewModel LensTable { get; }
    public UpdateViewModel Update { get; }

    [ObservableProperty]
    private string _fontLatin;

    [ObservableProperty]
    private string _fontEa;

    [ObservableProperty]
    private double _lineSpacing;

    [ObservableProperty]
    private bool _openAfterExport;

    public IReadOnlyList<string> AvailableLatinFonts => Options(FontLatin);
    public IReadOnlyList<string> AvailableEaFonts => Options(FontEa);

    public SettingsViewModel(AppSettings settings, MapTable cameras, MapTable lenses, IFontCatalog fontCatalog,
        UpdateViewModel update, string? settingsDir = null)
    {
        _settings = settings;
        _fontCatalog = fontCatalog;
        _settingsDir = settingsDir;
        Update = update;

        CameraTable = new MapTableViewModel(cameras,
            "EXIFのカメラ型番 → 表示名\n(例: ILCE-1M2 → α1 II。スライドには「α1 II / ILCE-1M2」と出ます)");
        LensTable = new MapTableViewModel(lenses,
            "EXIFのレンズ名 → 表示名\n(未登録ならEXIFの名前をそのまま使います)");

        _fontLatin = settings.FontLatin;
        _fontEa = settings.FontEa;
        _lineSpacing = settings.LineSpacing;
        _openAfterExport = settings.OpenAfterExport;
    }

    /// <summary>一覧にない名前(テンプレート既定のMS Pゴシックなど)も選択肢に残す</summary>
    private IReadOnlyList<string> Options(string current)
    {
        var families = _fontCatalog.GetFamilyNames();
        return families.Contains(current) ? families : new[] { current }.Concat(families).ToList();
    }

    partial void OnFontLatinChanged(string value)
    {
        _settings.FontLatin = value;
        Persist();
        OnPropertyChanged(nameof(AvailableLatinFonts));
    }

    partial void OnFontEaChanged(string value)
    {
        _settings.FontEa = value;
        Persist();
        OnPropertyChanged(nameof(AvailableEaFonts));
    }

    partial void OnLineSpacingChanged(double value)
    {
        _settings.LineSpacing = value;
        Persist();
    }

    partial void OnOpenAfterExportChanged(bool value)
    {
        _settings.OpenAfterExport = value;
        Persist();
    }

    [RelayCommand]
    private void ResetFonts()
    {
        FontLatin = SlideFonts.Default.Latin;
        FontEa = SlideFonts.Default.Ea;
    }

    [RelayCommand]
    private void ResetLineSpacing() => LineSpacing = 1.0;

    private void Persist() => AppSettingsStore.Save(_settings, _settingsDir);
}
