namespace PhotoForumPPTX.Core;

public sealed record ReleaseNote(string Version, IReadOnlyList<string> Items, string? Image = null);

/// <summary>更新内容。新しい版を出すときは、先頭に1件足す(スクリーンショットはApp/Resources/whatsnewに置く)</summary>
public static class ReleaseNotes
{
    public static readonly IReadOnlyList<ReleaseNote> All = new[]
    {
        new ReleaseNote("0.3", new[]
        {
            "アップデートのダウンロードが途中で失敗しにくいよう、タイムアウトを延長しました",
            "アップデートに失敗したとき、画面上部にも理由が表示されるようになりました",
        }),
        new ReleaseNote("0.2", new[]
        {
            "ヘルプ(使い方・不具合報告)と新機能ポップアップを追加しました",
            "アップデートを確認し、ボタン1つでダウンロード・入れ替え・再起動できるようになりました",
        }),
        new ReleaseNote("0.1", new[]
        {
            "写真を選んでEXIF(カメラ・レンズ・焦点距離・SS・F値・ISO)を読み取り、テンプレート通りのpptxを書き出せるようになりました",
            "スライドに入れる項目のON/OFFと並べ替え、写真ごとの手修正に対応しました",
            "設定でカメラ名・レンズ名の対応表、フォント、行間を編集できるようになりました(次回起動後も保持されます)",
        }),
    };

    /// <summary>last より新しく、current 以下の版(新しい順)</summary>
    public static IReadOnlyList<ReleaseNote> Between(string last, string current) =>
        All.Where(n => IsNewer(n.Version, last) && !IsNewer(n.Version, current)).ToList();

    public static bool IsNewer(string a, string b)
    {
        var x = a.Split('.').Select(s => int.TryParse(s, out var v) ? v : 0).ToArray();
        var y = b.Split('.').Select(s => int.TryParse(s, out var v) ? v : 0).ToArray();
        for (var i = 0; i < Math.Max(x.Length, y.Length); i++)
        {
            var p = i < x.Length ? x[i] : 0;
            var q = i < y.Length ? y[i] : 0;
            if (p != q) return p > q;
        }
        return false;
    }
}
