using System.Text.Json;

namespace PhotoForumPPTX.Core;

public sealed record MapEntry(string Key, string Value);

/// <summary>
/// EXIF値 → 表示名 の対応表。ユーザーが設定画面で編集でき、%AppData%に保存される。
/// </summary>
public sealed class MapTable
{
    private readonly string _path;
    private readonly string _versionPath;
    private readonly IReadOnlyList<MapEntry> _defaults;

    public List<MapEntry> Entries { get; private set; }

    public MapTable(string fileName, IReadOnlyList<MapEntry> defaults, int version, string? storageDir = null)
    {
        _defaults = defaults;
        var dir = storageDir ?? DefaultDir;
        System.IO.Directory.CreateDirectory(dir);
        _path = Path.Combine(dir, fileName);
        _versionPath = Path.Combine(dir, fileName + ".version");

        Entries = TryLoad(_path) ?? new List<MapEntry>(defaults);

        // アプリ更新で既定値が増えたとき、未登録のキーだけ既存の表に足す
        // (消した項目は version が同じ間は復活しない)
        var savedVersion = TryReadVersion(_versionPath);
        if (savedVersion < version)
        {
            foreach (var d in defaults)
            {
                if (!Entries.Exists(e => string.Equals(e.Key, d.Key, StringComparison.OrdinalIgnoreCase)))
                    Entries.Add(d);
            }
            File.WriteAllText(_versionPath, version.ToString());
            Save();
        }
    }

    public static string DefaultDir =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PhotoForumPPTX");

    public void Save()
    {
        File.WriteAllText(_path, JsonSerializer.Serialize(Entries));
    }

    public void Reset()
    {
        Entries = new List<MapEntry>(_defaults);
        Save();
    }

    /// <summary>大文字小文字・前後空白を無視して引く。見つからなければnull</summary>
    public string? Lookup(string key)
    {
        var k = key.Trim();
        if (k.Length == 0) return null;
        foreach (var e in Entries)
        {
            if (string.Equals(e.Key.Trim(), k, StringComparison.OrdinalIgnoreCase)) return e.Value;
        }
        return null;
    }

    private static List<MapEntry>? TryLoad(string path)
    {
        if (!File.Exists(path)) return null;
        try { return JsonSerializer.Deserialize<List<MapEntry>>(File.ReadAllText(path)); }
        catch { return null; }
    }

    private static int TryReadVersion(string path)
    {
        if (!File.Exists(path)) return 0;
        return int.TryParse(File.ReadAllText(path), out var v) ? v : 0;
    }
}
