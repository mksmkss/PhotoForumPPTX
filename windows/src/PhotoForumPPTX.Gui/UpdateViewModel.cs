using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PhotoForumPPTX.Core;

namespace PhotoForumPPTX.Gui;

public sealed partial class UpdateViewModel : ObservableObject
{
    private readonly IReleaseFeed _feed;

    [ObservableProperty]
    private UpdateCheck.Found? _available;

    [ObservableProperty]
    private string _status = "";

    [ObservableProperty]
    private bool _checking;

    public UpdateViewModel(IReleaseFeed feed) => _feed = feed;

    /// <summary>起動時などの自動チェックから直接呼べる本体(manual=trueのときだけ「最新です」等もStatusに出す)</summary>
    public async Task CheckAsync(bool manual = false)
    {
        Checking = true;
        try
        {
            var releases = await _feed.FetchAsync();
            var found = UpdateCheck.FindUpdate(releases, AppInfo.Version);
            Available = found;
            if (manual) Status = found != null ? $"新しい版 v{found.Version} があります" : $"最新版です (v{AppInfo.Version})";
        }
        catch (Exception ex)
        {
            if (manual) Status = $"更新の確認に失敗しました: {ex.Message}";
        }
        finally
        {
            Checking = false;
        }
    }

    [RelayCommand]
    private Task Check() => CheckAsync(manual: true);

    [RelayCommand]
    private void Dismiss() => Available = null;
}
