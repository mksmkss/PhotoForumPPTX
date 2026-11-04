namespace PhotoForumPPTX.Core;

public sealed record SlideFonts(string Latin, string Ea)
{
    /// <summary>テンプレートの既定</summary>
    public static readonly SlideFonts Template = new("Calibri", "ＭＳ Ｐゴシック");

    /// <summary>アプリの初期値(欧文はArial、和文はテンプレートのまま)</summary>
    public static readonly SlideFonts Default = new("Arial", Template.Ea);
}
