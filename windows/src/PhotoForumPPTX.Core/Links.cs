namespace PhotoForumPPTX.Core;

public static class Links
{
    public const string Repo = "mksmkss/PhotoForumPPTX";
    public static string RepoUrl => $"https://github.com/{Repo}";
    public static string IssuesUrl => $"https://github.com/{Repo}/issues";

    /// <summary>バージョンとOS情報を入れた状態でIssue作成ページを開くURL</summary>
    public static string NewIssueUrl(string osDescription)
    {
        var body = "## 内容\n(何をしたら、どうなったか / どうなってほしいか)\n\n" +
                   $"## 環境\n- アプリ: v{AppInfo.Version} (Windows)\n- OS: {osDescription}";
        return $"https://github.com/{Repo}/issues/new?body={Uri.EscapeDataString(body)}";
    }
}
