using System.Text.Json;

namespace PhotoForumPPTX.Core;

public static class AppSettingsStore
{
    private const string FileName = "settings.json";

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

    public static void Save(AppSettings settings, string? storageDir = null)
    {
        var dir = storageDir ?? MapTable.DefaultDir;
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, FileName), JsonSerializer.Serialize(settings));
    }
}
