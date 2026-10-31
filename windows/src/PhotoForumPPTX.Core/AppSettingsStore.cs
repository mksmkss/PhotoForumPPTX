using System.Text.Json;

namespace PhotoForumPPTX.Core;

public static class AppSettingsStore
{
    private const string FileName = "settings.json";

    /// <summary>設定ファイルが既に存在するか(= 新規インストールか既存ユーザーかの判定に使う)</summary>
    public static bool Exists(string? storageDir = null) =>
        File.Exists(Path.Combine(storageDir ?? MapTable.DefaultDir, FileName));

    public static AppSettings Load(string? storageDir = null)
    {
        var path = Path.Combine(storageDir ?? MapTable.DefaultDir, FileName);
        if (File.Exists(path))
        {
            try
            {
                var loaded = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(path));
                if (loaded != null) return loaded;
            }
            catch { /* 壊れていれば既定値から始める */ }
        }
        return new AppSettings();
    }

    /// <summary>ディスク容量不足や権限エラーなどで保存に失敗しても、アプリ本体は落とさない</summary>
    public static void Save(AppSettings settings, string? storageDir = null)
    {
        var dir = storageDir ?? MapTable.DefaultDir;
        try
        {
            Directory.CreateDirectory(dir);
            AtomicFile.Write(Path.Combine(dir, FileName), JsonSerializer.Serialize(settings));
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}
