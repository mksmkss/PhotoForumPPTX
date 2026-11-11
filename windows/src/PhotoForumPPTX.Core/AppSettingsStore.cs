using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;

namespace PhotoForumPPTX.Core;

public static class AppSettingsStore
{
    private const string FileName = "settings.json";

    /// <summary>
    /// 項目の種類(Kind)を数値ではなく名前("Camera"など)で書き、ファイルを見て何の設定か分かるようにする。
    /// 読み込みは旧形式の数値("Kind":3)も受け付ける。
    /// 人が開いて読めるよう改行・インデントを入れ、フォント名などの日本語も\uXXXXにせずそのまま書く
    /// </summary>
    private static readonly JsonSerializerOptions Options = new()
    {
        Converters = { new JsonStringEnumConverter() },
        WriteIndented = true,
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
    };

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
                var loaded = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(path), Options);
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
            AtomicFile.Write(Path.Combine(dir, FileName), JsonSerializer.Serialize(settings, Options));
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}
