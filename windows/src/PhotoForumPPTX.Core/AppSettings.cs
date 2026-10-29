namespace PhotoForumPPTX.Core;

/// <summary>%AppData%\PhotoForumPPTX\settings.json に永続化するアプリ全体の設定</summary>
public sealed class AppSettings
{
    public string StudentId { get; set; } = "";
    public string Name { get; set; } = "";
    public List<FieldSetting> Fields { get; set; } = new();
    public string FontLatin { get; set; } = SlideFonts.Default.Latin;
    public string FontEa { get; set; } = SlideFonts.Default.Ea;
    public double LineSpacing { get; set; } = 1.0;
    public bool OpenAfterExport { get; set; } = true;
}
