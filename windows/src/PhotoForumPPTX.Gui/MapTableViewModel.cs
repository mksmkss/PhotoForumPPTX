using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PhotoForumPPTX.Core;

namespace PhotoForumPPTX.Gui;

/// <summary>設定画面の「カメラ名」「レンズ名」タブ1枚分</summary>
public sealed partial class MapTableViewModel : ObservableObject
{
    private readonly MapTable _table;

    public string Caption { get; }
    public ObservableCollection<MapEntryViewModel> Entries { get; } = new();

    public MapTableViewModel(MapTable table, string caption)
    {
        _table = table;
        Caption = caption;
        foreach (var e in table.Entries)
        {
            var vm = new MapEntryViewModel(e);
            vm.PropertyChanged += OnEntryPropertyChanged;
            Entries.Add(vm);
        }
        Entries.CollectionChanged += OnEntriesCollectionChanged;
    }

    private void OnEntriesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
            foreach (MapEntryViewModel item in e.NewItems) item.PropertyChanged += OnEntryPropertyChanged;
        if (e.OldItems != null)
            foreach (MapEntryViewModel item in e.OldItems) item.PropertyChanged -= OnEntryPropertyChanged;
        Persist();
    }

    private void OnEntryPropertyChanged(object? sender, PropertyChangedEventArgs e) => Persist();

    [RelayCommand]
    private void AddEntry() => Entries.Insert(0, new MapEntryViewModel(new MapEntry("", "")));

    [RelayCommand]
    private void RemoveEntry(MapEntryViewModel? entry)
    {
        if (entry != null) Entries.Remove(entry);
    }

    [RelayCommand]
    private void ResetEntries()
    {
        _table.Reset();
        // Clear()はPersist()を即座に発火させ_tableを上書きするため、既定値を先に退避しておく
        var defaults = _table.Entries.ToList();
        Entries.Clear();
        foreach (var e in defaults) Entries.Add(new MapEntryViewModel(e));
    }

    private void Persist() => _table.Replace(Entries.Select(e => e.ToMapEntry()));
}
