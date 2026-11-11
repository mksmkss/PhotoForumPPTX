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

    /// <summary>同じグループ内で上に移動できるか(グループの先頭なら▲を出さない)</summary>
    [ObservableProperty]
    private bool _canMoveUp;

    /// <summary>同じグループ内で下に移動できるか(グループの末尾なら▼を出さない)</summary>
    [ObservableProperty]
    private bool _canMoveDown;

    /// <summary>直前の項目と別グループか(上に区切り線を引く)。リスト先頭はfalse</summary>
    [ObservableProperty]
    private bool _startsNewGroup;

    public FieldSettingViewModel(FieldKind kind, bool enabled)
    {
        Kind = kind;
        _enabled = enabled;
    }

    public FieldSetting ToFieldSetting() => new(Kind, Enabled);
}
