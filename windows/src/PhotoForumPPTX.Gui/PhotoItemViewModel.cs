using CommunityToolkit.Mvvm.ComponentModel;
using PhotoForumPPTX.Core;

namespace PhotoForumPPTX.Gui;

/// <summary>写真1枚分。EXIFから読み取った値はPhotoInfoに保持し、GUIからの手修正もそこへ反映する</summary>
public sealed class PhotoItemViewModel : ObservableObject
{
    public PhotoInfo Info { get; }

    public string FilePath => Info.FilePath;
    public string FileName => Path.GetFileName(Info.FilePath);
    public int Orientation => Info.Orientation;

    public PhotoItemViewModel(PhotoInfo info)
    {
        Info = info;
    }

    public string Title
    {
        get => Info.Title;
        set => SetInfo(Info.Title, value, v => Info.Title = v);
    }

    public string Camera
    {
        get => Info.Camera;
        set => SetInfo(Info.Camera, value, v => Info.Camera = v);
    }

    public string Lens
    {
        get => Info.Lens;
        set => SetInfo(Info.Lens, value, v => Info.Lens = v);
    }

    public string Focal
    {
        get => Info.Focal;
        set => SetInfo(Info.Focal, value, v => Info.Focal = v);
    }

    public string Shutter
    {
        get => Info.Shutter;
        set => SetInfo(Info.Shutter, value, v => Info.Shutter = v);
    }

    public string Aperture
    {
        get => Info.Aperture;
        set => SetInfo(Info.Aperture, value, v => Info.Aperture = v);
    }

    public string Iso
    {
        get => Info.Iso;
        set => SetInfo(Info.Iso, value, v => Info.Iso = v);
    }

    public string Note
    {
        get => Info.Note;
        set => SetInfo(Info.Note, value, v => Info.Note = v);
    }

    private void SetInfo(string current, string value, Action<string> assign, [System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
    {
        if (current == value) return;
        assign(value);
        OnPropertyChanged(propertyName);
    }
}
