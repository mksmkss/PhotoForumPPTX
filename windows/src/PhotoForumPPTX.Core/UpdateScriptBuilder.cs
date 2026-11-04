namespace PhotoForumPPTX.Core;

/// <summary>
/// 自分自身(実行中のexe)の終了を待ってから新しい版に入れ替え、再起動するPowerShellスクリプトを組み立てる。
/// Windowsは実行中のexeを直接上書きできないため、別プロセスとして起動したこのスクリプトが代わりに入れ替える。
/// </summary>
public static class UpdateScriptBuilder
{
    /// <param name="waitForProcessId">終了を待つ現在のプロセスID</param>
    /// <param name="sourceDir">展開済みの新しい版のフォルダ</param>
    /// <param name="destDir">現在インストールされているフォルダ(ここに上書きする)</param>
    /// <param name="destExePath">入れ替え後に再起動するexeのフルパス</param>
    /// <param name="workDir">ダウンロード・展開に使った作業用一時フォルダ(終了後に削除する)</param>
    public static string Build(int waitForProcessId, string sourceDir, string destDir, string destExePath, string workDir) =>
        $"""
        $ErrorActionPreference = 'SilentlyContinue'
        Wait-Process -Id {waitForProcessId} -Timeout 30
        Start-Sleep -Milliseconds 300
        robocopy "{sourceDir}" "{destDir}" /MIR /NFL /NDL /NJH /NJS /R:3 /W:1
        Start-Process -FilePath "{destExePath}"
        Remove-Item -Recurse -Force "{workDir}"
        """;
}
