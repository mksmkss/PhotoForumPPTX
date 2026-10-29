namespace PhotoForumPPTX.Core;

/// <summary>書き込み中のクラッシュ・電源断で設定ファイルが壊れないよう、一時ファイル経由で置き換える</summary>
internal static class AtomicFile
{
    public static void Write(string path, string content)
    {
        var tmp = path + ".tmp";
        File.WriteAllText(tmp, content);
        File.Move(tmp, path, overwrite: true);
    }
}
