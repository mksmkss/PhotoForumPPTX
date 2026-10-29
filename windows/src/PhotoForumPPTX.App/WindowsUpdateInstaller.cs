using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Windows;
using PhotoForumPPTX.Core;
using PhotoForumPPTX.Gui;

namespace PhotoForumPPTX.App;

/// <summary>
/// 新しい版のzipをダウンロードし、自分自身の終了を待つPowerShellスクリプトを起動して入れ替える。
/// Windowsは実行中のexeを直接上書きできないため、スクリプトに後を託してこのプロセスは終了する。
/// </summary>
public sealed class WindowsUpdateInstaller : IUpdateInstaller
{
    public async Task InstallAsync(string zipUrl)
    {
        var work = Path.Combine(Path.GetTempPath(), "pfp-update-" + Guid.NewGuid());
        Directory.CreateDirectory(work);

        var zipPath = Path.Combine(work, "update.zip");
        using (var http = new HttpClient { Timeout = TimeSpan.FromSeconds(120) })
        {
            http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("PhotoForumPPTX-Windows", AppInfo.Version));
            var bytes = await http.GetByteArrayAsync(zipUrl);
            await File.WriteAllBytesAsync(zipPath, bytes);
        }

        var extractDir = Path.Combine(work, "extracted");
        ZipFile.ExtractToDirectory(zipPath, extractDir);

        var destDir = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar);
        var exeName = Path.GetFileName(Environment.ProcessPath) ?? "PhotoForumPPTX.App.exe";
        var newExePath = Path.Combine(extractDir, exeName);
        if (!File.Exists(newExePath))
            throw new InvalidOperationException($"ダウンロードしたzipの中に{exeName}が見つかりません");

        if (!CanWrite(destDir))
            throw new InvalidOperationException($"{destDir} に書き込めません。デスクトップなど書き込み可能な場所に展開し直してください");

        var destExePath = Path.Combine(destDir, exeName);
        var script = UpdateScriptBuilder.Build(Environment.ProcessId, extractDir, destDir, destExePath, work);
        var scriptPath = Path.Combine(work, "apply-update.ps1");
        // Windows PowerShellが非ASCII文字(日本語パス等)を正しく読めるよう、BOM付きUTF-8で保存する
        await File.WriteAllTextAsync(scriptPath, script, new UTF8Encoding(true));

        var psi = new ProcessStartInfo("powershell.exe")
        {
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        psi.ArgumentList.Add("-NoProfile");
        psi.ArgumentList.Add("-ExecutionPolicy");
        psi.ArgumentList.Add("Bypass");
        psi.ArgumentList.Add("-WindowStyle");
        psi.ArgumentList.Add("Hidden");
        psi.ArgumentList.Add("-File");
        psi.ArgumentList.Add(scriptPath);
        Process.Start(psi);

        Application.Current.Shutdown();
    }

    private static bool CanWrite(string dir)
    {
        try
        {
            var probe = Path.Combine(dir, ".write-test-" + Guid.NewGuid());
            File.WriteAllText(probe, "");
            File.Delete(probe);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
