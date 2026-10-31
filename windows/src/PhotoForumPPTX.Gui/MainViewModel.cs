using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PhotoForumPPTX.Core;

namespace PhotoForumPPTX.Gui;

public sealed partial class MainViewModel : ObservableObject
{
    /// <summary>Mac版の既定(カメラ・レンズ・焦点距離・露出は有効、個人情報は無効)に合わせる</summary>
    private static readonly FieldSetting[] DefaultFieldSettings =
    {
        new(FieldKind.Camera, true),
        new(FieldKind.Lens, true),
        new(FieldKind.Focal, true),
        new(FieldKind.Shutter, true),
        new(FieldKind.Aperture, true),
        new(FieldKind.Iso, true),
        new(FieldKind.StudentId, false),
        new(FieldKind.Name, false),
        new(FieldKind.Title, false),
        new(FieldKind.Note, false),
    };

    private readonly MapTable _cameras;
    private readonly MapTable _lenses;
    private readonly string _templatePath;
    private readonly IDialogService _dialogService;
    private readonly AppSettings _settings;
    private readonly string? _settingsDir;

    public ObservableCollection<PhotoItemViewModel> Photos { get; } = new();
    public ObservableCollection<FieldSettingViewModel> Fields { get; }

    [ObservableProperty]
    private string _studentId;

    [ObservableProperty]
    private string _name;

    [ObservableProperty]
    private string _status = "";

    public bool HasPhotos => Photos.Count > 0;

    /// <summary>右サイドバーの学籍番号・氏名欄の表示判定に使う</summary>
    public FieldSettingViewModel StudentIdField => Fields.First(f => f.Kind == FieldKind.StudentId);
    public FieldSettingViewModel NameField => Fields.First(f => f.Kind == FieldKind.Name);

    /// <summary>写真ごとの行でタイトル・備考欄の表示判定に使う</summary>
    public FieldSettingViewModel TitleField => Fields.First(f => f.Kind == FieldKind.Title);
    public FieldSettingViewModel NoteField => Fields.First(f => f.Kind == FieldKind.Note);

    /// <summary>対応表の表示名候補(カメラは「表示名 / 型番」)</summary>
    public IEnumerable<string> CameraCandidates => _cameras.Entries
        .Where(e => !string.IsNullOrEmpty(e.Key))
        .Select(e => string.IsNullOrEmpty(e.Value) || e.Value == e.Key ? e.Key : $"{e.Value} / {e.Key}");

    /// <summary>対応表の表示名 + 今読み込んでいる写真のEXIFレンズ名(重複除く)</summary>
    public IEnumerable<string> LensCandidates => _lenses.Entries.Select(e => e.Value)
        .Concat(Photos.Select(p => p.Lens))
        .Where(s => !string.IsNullOrEmpty(s))
        .Distinct();

    public MainViewModel(MapTable cameras, MapTable lenses, string templatePath, IDialogService dialogService,
        AppSettings settings, string? settingsDir = null)
    {
        _cameras = cameras;
        _lenses = lenses;
        _templatePath = templatePath;
        _dialogService = dialogService;
        _settings = settings;
        _settingsDir = settingsDir;

        _studentId = settings.StudentId;
        _name = settings.Name;

        Fields = new ObservableCollection<FieldSettingViewModel>(LoadFields(settings));
        foreach (var f in Fields) f.PropertyChanged += (_, _) => PersistFields();
        Fields.CollectionChanged += (_, _) => PersistFields();

        Photos.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(HasPhotos));
            OnPropertyChanged(nameof(LensCandidates));
            ExportCommand.NotifyCanExecuteChanged();
        };
    }

    /// <summary>保存済みの項目設定に、追加された既定項目(未登録のkind)を末尾へ補う</summary>
    private static IEnumerable<FieldSettingViewModel> LoadFields(AppSettings settings)
    {
        var merged = new List<FieldSetting>(settings.Fields);
        foreach (var d in DefaultFieldSettings)
            if (!merged.Any(f => f.Kind == d.Kind)) merged.Add(d);
        return merged.Select(f => new FieldSettingViewModel(f.Kind, f.Enabled));
    }

    partial void OnStudentIdChanged(string value)
    {
        _settings.StudentId = value;
        AppSettingsStore.Save(_settings, _settingsDir);
    }

    partial void OnNameChanged(string value)
    {
        _settings.Name = value;
        AppSettingsStore.Save(_settings, _settingsDir);
    }

    private void PersistFields()
    {
        _settings.Fields = Fields.Select(f => f.ToFieldSetting()).ToList();
        AppSettingsStore.Save(_settings, _settingsDir);
    }

    /// <summary>写真を追加する。読み込みに失敗したファイルはスキップしてStatusに表示する</summary>
    public void AddPhotos(IEnumerable<string> paths)
    {
        var skipped = new List<string>();
        foreach (var path in paths)
        {
            var info = PhotoInfo.Load(path, _cameras, _lenses);
            if (info != null) Photos.Add(new PhotoItemViewModel(info));
            else skipped.Add(Path.GetFileName(path));
        }
        if (skipped.Count > 0) Status = $"読み込み失敗(スキップ): {string.Join(", ", skipped)}";
    }

    [RelayCommand]
    private void AddPhotosDialog()
    {
        var paths = _dialogService.PickPhotos();
        if (paths != null) AddPhotos(paths);
    }

    [RelayCommand]
    private void RemovePhoto(PhotoItemViewModel? photo)
    {
        if (photo != null) Photos.Remove(photo);
    }

    [RelayCommand]
    private void MovePhotoUp(PhotoItemViewModel? photo) => Move(Photos, photo, -1);

    [RelayCommand]
    private void MovePhotoDown(PhotoItemViewModel? photo) => Move(Photos, photo, +1);

    [RelayCommand]
    private void MoveFieldUp(FieldSettingViewModel? field) => Move(Fields, field, -1);

    [RelayCommand]
    private void MoveFieldDown(FieldSettingViewModel? field) => Move(Fields, field, +1);

    private static void Move<T>(ObservableCollection<T> list, T? item, int offset)
    {
        if (item == null) return;
        var index = list.IndexOf(item);
        if (index < 0) return;
        var newIndex = index + offset;
        if (newIndex < 0 || newIndex >= list.Count) return;
        list.Move(index, newIndex);
    }

    [RelayCommand(CanExecute = nameof(HasPhotos))]
    private async Task Export()
    {
        var output = _dialogService.PickSaveLocation("写真投稿フォーラム.pptx");
        if (output == null) return;
        await ExportAsync(output);
    }

    /// <summary>ダイアログを介さず直接呼べる書き出し本体(テストから直接検証するために公開)</summary>
    public async Task ExportAsync(string outputPath)
    {
        if (Photos.Count == 0) return;
        var fields = Fields.Select(f => f.ToFieldSetting()).ToList();
        var photoInfos = Photos.Select(p => p.Info).ToList();
        var fonts = new SlideFonts(_settings.FontLatin, _settings.FontEa);
        try
        {
            await Task.Run(() => PptxBuilder.Build(_templatePath, outputPath, photoInfos, fields, StudentId, Name, fonts, _settings.LineSpacing));
            Status = $"書き出しました: {Path.GetFileName(outputPath)}";
            if (_settings.OpenAfterExport) _dialogService.OpenFile(outputPath);
            else _dialogService.RevealInExplorer(outputPath);
        }
        catch (Exception ex)
        {
            Status = $"失敗: {ex.Message}";
        }
    }
}
