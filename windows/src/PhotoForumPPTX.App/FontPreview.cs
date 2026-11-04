using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace PhotoForumPPTX.App;

/// <summary>
/// 設定画面のフォントプレビュー用の添付ビヘイビア。
/// pptx本体と同じく、欧文フォント(Latin)と和文フォント(EA)を文字種で振り分けて1つのTextBlockに表示する
/// (WPFのTextBlock.FontFamilyは文字列全体に一括適用されるため、Run単位で分ける必要がある)。
/// </summary>
public static class FontPreview
{
    private const string SampleText = "Shutter Speed: 1/800 sec   α1 II / ILCE-1M2   「作品タイトル」";

    public static readonly DependencyProperty LatinFontProperty =
        DependencyProperty.RegisterAttached("LatinFont", typeof(string), typeof(FontPreview),
            new PropertyMetadata(null, OnFontChanged));

    public static readonly DependencyProperty EaFontProperty =
        DependencyProperty.RegisterAttached("EaFont", typeof(string), typeof(FontPreview),
            new PropertyMetadata(null, OnFontChanged));

    public static void SetLatinFont(TextBlock element, string? value) => element.SetValue(LatinFontProperty, value);
    public static string? GetLatinFont(TextBlock element) => (string?)element.GetValue(LatinFontProperty);
    public static void SetEaFont(TextBlock element, string? value) => element.SetValue(EaFontProperty, value);
    public static string? GetEaFont(TextBlock element) => (string?)element.GetValue(EaFontProperty);

    private static void OnFontChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TextBlock tb) Rebuild(tb);
    }

    private static void Rebuild(TextBlock tb)
    {
        var latin = GetLatinFont(tb);
        var ea = GetEaFont(tb);

        tb.Inlines.Clear();
        var segment = new StringBuilder();
        bool? segmentIsEa = null;

        void Flush()
        {
            if (segment.Length == 0) return;
            var run = new Run(segment.ToString());
            var family = segmentIsEa == true ? ea : latin;
            if (!string.IsNullOrEmpty(family)) run.FontFamily = new FontFamily(family);
            tb.Inlines.Add(run);
            segment.Clear();
        }

        // 全角相当(0x2000以上)は和文フォント、それ以外は欧文フォント。PptxBuilder.EstimateLinesと同じ基準
        foreach (var rune in SampleText.EnumerateRunes())
        {
            var isEa = rune.Value >= 0x2000;
            if (segmentIsEa != null && isEa != segmentIsEa) Flush();
            segmentIsEa = isEa;
            segment.Append(rune.ToString());
        }
        Flush();
    }
}
