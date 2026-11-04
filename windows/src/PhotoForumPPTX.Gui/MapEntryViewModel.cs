using CommunityToolkit.Mvvm.ComponentModel;
using PhotoForumPPTX.Core;

namespace PhotoForumPPTX.Gui;

/// <summary>対応表1行分(EXIFの値 → 表示名)。MapEntryはレコードで不変なため、編集用にミュータブルなラッパーを用意する</summary>
public sealed partial class MapEntryViewModel : ObservableObject
{
    [ObservableProperty]
    private string _key;

    [ObservableProperty]
    private string _value;

    public MapEntryViewModel(MapEntry entry)
    {
        _key = entry.Key;
        _value = entry.Value;
    }

    public MapEntry ToMapEntry() => new(Key, Value);
}
