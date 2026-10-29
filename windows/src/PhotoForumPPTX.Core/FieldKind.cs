namespace PhotoForumPPTX.Core;

public enum FieldKind
{
    StudentId, Name, Title, Camera, Lens, Focal, Shutter, Aperture, Iso, Note
}

public static class FieldKindExtensions
{
    public static string Label(this FieldKind kind) => kind switch
    {
        FieldKind.StudentId => "学籍番号",
        FieldKind.Name => "氏名",
        FieldKind.Title => "作品タイトル",
        FieldKind.Camera => "カメラ名",
        FieldKind.Lens => "レンズ名",
        FieldKind.Focal => "焦点距離",
        FieldKind.Shutter => "シャッタースピード",
        FieldKind.Aperture => "F値",
        FieldKind.Iso => "ISO",
        FieldKind.Note => "備考(Step Cropなど)",
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };

    /// <summary>文字サイズ(pt)。テンプレートに合わせる</summary>
    public static double FontSize(this FieldKind kind) => kind switch
    {
        FieldKind.StudentId or FieldKind.Name => 10.5,
        FieldKind.Title => 11,
        FieldKind.Camera or FieldKind.Lens => 8,
        FieldKind.Focal => 20,
        FieldKind.Shutter or FieldKind.Aperture or FieldKind.Iso or FieldKind.Note => 7.5,
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };

    /// <summary>同じグループ内は詰め、グループが変わるときだけ間隔を空ける</summary>
    public static int Group(this FieldKind kind) => kind switch
    {
        FieldKind.StudentId or FieldKind.Name or FieldKind.Title => 0,
        FieldKind.Camera or FieldKind.Lens => 1,
        FieldKind.Focal => 3, // 右カラム中央に大きく
        FieldKind.Shutter or FieldKind.Aperture or FieldKind.Iso or FieldKind.Note => 2, // 赤線の下(左寄せ)
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };
}

public readonly record struct FieldSetting(FieldKind Kind, bool Enabled);
