using PhotoForumPPTX.Core;
using PhotoForumPPTX.Gui;

namespace PhotoForumPPTX.Gui.Tests;

public class WhatsNewCoordinatorTests
{
    [Fact]
    public void FreshInstall_ShowsNothing()
    {
        var (pending, toPersist) = WhatsNewCoordinator.Compute(storedLastSeenVersion: "", hadExistingSettings: false);

        Assert.Empty(pending);
        Assert.Equal(AppInfo.Version, toPersist);
    }

    [Fact]
    public void ExistingUserWithNoRecord_SeesFullHistory()
    {
        var (pending, toPersist) = WhatsNewCoordinator.Compute(storedLastSeenVersion: "", hadExistingSettings: true);

        Assert.Equal(ReleaseNotes.All.Count, pending.Count);
        // 表示対象がある間は、未確認のまま次回も同じ内容を出せるよう「last」側を保持する
        Assert.Equal("0.0", toPersist);
    }

    [Fact]
    public void AlreadySeenCurrentVersion_ShowsNothing()
    {
        var (pending, toPersist) = WhatsNewCoordinator.Compute(storedLastSeenVersion: AppInfo.Version, hadExistingSettings: true);

        Assert.Empty(pending);
        Assert.Equal(AppInfo.Version, toPersist);
    }
}
