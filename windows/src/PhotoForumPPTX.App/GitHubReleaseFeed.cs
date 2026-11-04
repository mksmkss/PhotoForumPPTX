using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using PhotoForumPPTX.Core;
using PhotoForumPPTX.Gui;

namespace PhotoForumPPTX.App;

public sealed class GitHubReleaseFeed : IReleaseFeed
{
    private const string Repo = "mksmkss/PhotoForumPPTX";
    private static readonly HttpClient Http = CreateClient();

    private static HttpClient CreateClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("PhotoForumPPTX-Windows", AppInfo.Version));
        return client;
    }

    public async Task<IReadOnlyList<GitHubRelease>> FetchAsync()
    {
        var dtoList = await Http.GetFromJsonAsync<List<ReleaseDto>>($"https://api.github.com/repos/{Repo}/releases")
                      ?? new List<ReleaseDto>();
        return dtoList
            .Select(d => new GitHubRelease(d.TagName, d.Assets.Select(a => new GitHubReleaseAsset(a.Name, a.BrowserDownloadUrl)).ToList()))
            .ToList();
    }

    private sealed class ReleaseDto
    {
        [JsonPropertyName("tag_name")] public string TagName { get; set; } = "";
        [JsonPropertyName("assets")] public List<AssetDto> Assets { get; set; } = new();
    }

    private sealed class AssetDto
    {
        [JsonPropertyName("name")] public string Name { get; set; } = "";
        [JsonPropertyName("browser_download_url")] public string BrowserDownloadUrl { get; set; } = "";
    }
}
