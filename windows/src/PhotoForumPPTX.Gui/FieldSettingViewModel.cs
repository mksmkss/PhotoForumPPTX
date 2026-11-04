using CommunityToolkit.Mvvm.ComponentModel;
using PhotoForumPPTX.Core;

namespace PhotoForumPPTX.Gui;

/// <summary>右サイドバーの「スライドに入れる項目」1行分</summary>
public sealed partial class FieldSettingViewModel : ObservableObject
{
    public FieldKind Kind { get; }
    public string Label => Kind.Label();

    [ObservableProperty]
    private bool _enabled;

    public FieldSettingViewModel(FieldKind kind, bool enabled)
    {
        Kind = kind;
        _enabled = enabled;
    }

    public FieldSetting ToFieldSetting() => new(Kind, Enabled);
}
