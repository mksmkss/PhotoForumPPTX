using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using PhotoForumPPTX.Gui;

namespace PhotoForumPPTX.App;

public sealed class InverseBooleanToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public sealed class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value != null ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>サムネイル表示用。回転(90/180/270)のみ補正し、左右反転を伴う向きは未対応(書き出し結果には影響しない)</summary>
public sealed class ThumbnailConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not PhotoItemViewModel photo) return null;
        try
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(photo.FilePath);
            bitmap.DecodePixelWidth = 160;
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
            bitmap.EndInit();
            bitmap.Freeze();

            var angle = photo.Orientation switch { 3 => 180, 6 => 90, 8 => 270, _ => 0 };
            if (angle == 0) return bitmap;

            var rotated = new TransformedBitmap(bitmap, new System.Windows.Media.RotateTransform(angle));
            rotated.Freeze();
            return rotated;
        }
        catch
        {
            return null;
        }
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>設定画面「行間」タブのデモ表示用。実際のpptxのサイズ比(フォントサイズ×1.2×行間)を1.6倍に拡大して見せる</summary>
public sealed class LineSpacingToHeightConverter : IValueConverter
{
    private const double DemoFontSize = 7.5 * 1.6; // pptx本体の撮影情報フォントサイズをデモ表示用に拡大

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var spacing = value is double d ? d : 1.0;
        return DemoFontSize * 1.2 * spacing;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
