using System.Drawing;
using System.Globalization;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;

namespace PhotoForumPPTX.Core;

/// <summary>EXIFから読み取った写真1枚分の情報。</summary>
public sealed class PhotoInfo
{
    public string FilePath { get; }

    /// <summary>向き補正後のピクセルサイズ</summary>
    public double PixelWidth { get; }
    public double PixelHeight { get; }
    public int Orientation { get; }

    public string Title { get; set; } = "";
    public string Camera { get; set; } = "";
    public string Lens { get; set; } = "";
    public string Focal { get; set; } = "";
    public string Shutter { get; set; } = "";
    public string Aperture { get; set; } = "";
    public string Iso { get; set; } = "";
    public string Note { get; set; } = "";

    private PhotoInfo(string filePath, double width, double height, int orientation)
    {
        FilePath = filePath;
        Orientation = orientation;
        if (orientation is >= 5 and <= 8)
        {
            PixelWidth = height;
            PixelHeight = width;
        }
        else
        {
            PixelWidth = width;
            PixelHeight = height;
        }
    }

    /// <summary>テスト用: EXIF読み取りを経由せずインスタンスを作る</summary>
    internal static PhotoInfo ForTesting(string filePath, double width, double height, int orientation) =>
        new(filePath, width, height, orientation);

    /// <summary>読み取りに失敗した場合はnull</summary>
    public static PhotoInfo? Load(string path, MapTable cameras, MapTable lenses)
    {
        IReadOnlyList<MetadataExtractor.Directory> dirs;
        try { dirs = ImageMetadataReader.ReadMetadata(path); }
        catch { return null; }

        int width, height;
        try
        {
            using var img = Image.FromFile(path);
            width = img.Width;
            height = img.Height;
        }
        catch { return null; }

        var ifd0 = dirs.OfType<ExifIfd0Directory>().FirstOrDefault();
        var sub = dirs.OfType<ExifSubIfdDirectory>().FirstOrDefault();

        var orientation = ifd0 != null && ifd0.TryGetInt32(ExifDirectoryBase.TagOrientation, out var o) ? o : 1;
        var photo = new PhotoInfo(path, width, height, orientation);

        var model = ifd0?.GetDescription(ExifDirectoryBase.TagModel);
        if (!string.IsNullOrWhiteSpace(model)) photo.Camera = CameraText(model!, cameras);

        var lensModel = sub?.GetDescription(ExifDirectoryBase.TagLensModel);
        if (!string.IsNullOrWhiteSpace(lensModel)) photo.Lens = lenses.Lookup(lensModel!) ?? lensModel!;

        if (sub != null && sub.TryGetDouble(ExifDirectoryBase.TagFocalLength, out var focal))
        {
            // デジタルズーム(RX1R IIIのStep Cropなど)は実効焦点距離に掛ける
            var zoom = sub.TryGetDouble(ExifDirectoryBase.TagDigitalZoomRatio, out var z) ? z : 1;
            var effective = focal * (zoom > 1 ? zoom : 1);
            photo.Focal = Math.Round(effective, MidpointRounding.AwayFromZero).ToString("F0", CultureInfo.InvariantCulture);
            if (zoom > 1.01) photo.Note = $"*Step Crop: {photo.Focal}mm equiv.";
        }

        if (sub != null && sub.TryGetDouble(ExifDirectoryBase.TagExposureTime, out var exposure))
            photo.Shutter = ShutterText(exposure);

        if (sub != null && sub.TryGetDouble(ExifDirectoryBase.TagFNumber, out var fNumber))
            photo.Aperture = Trim(fNumber);

        var iso = ReadIso(sub);
        if (iso.HasValue) photo.Iso = iso.Value.ToString(CultureInfo.InvariantCulture);

        return photo;
    }

    private static int? ReadIso(ExifSubIfdDirectory? sub)
    {
        if (sub == null) return null;
        if (sub.TryGetInt32(ExifDirectoryBase.TagIsoEquivalent, out var single)) return single;
        if (sub.GetObject(ExifDirectoryBase.TagIsoEquivalent) is Array arr && arr.Length > 0)
            return Convert.ToInt32(arr.GetValue(0), CultureInfo.InvariantCulture);
        return null;
    }

    private static string CameraText(string model, MapTable table)
    {
        var name = table.Lookup(model);
        if (name == null) return model;
        return name == model ? model : $"{name} / {model}";
    }

    private static string Trim(double v)
    {
        var s = v.ToString("F1", CultureInfo.InvariantCulture);
        return s.EndsWith(".0", StringComparison.Ordinal) ? s[..^2] : s;
    }

    private static string ShutterText(double t)
    {
        if (t >= 1) return Trim(t);
        var denom = (int)Math.Round(1 / t, MidpointRounding.AwayFromZero);
        return "1/" + denom.ToString("N0", CultureInfo.InvariantCulture);
    }
}
