using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;

namespace PhotoForumPPTX.Core;

public sealed class PptxBuildException : Exception
{
    public PptxBuildException(string message) : base(message) { }
}

public static class PptxBuilder
{
    // テンプレートの寸法 (EMU)
    public const int SlideH = 5143500;
    public const int PhotoMaxW = 7721508; // 写真領域の最大幅(右カラムに食い込まない)
    public const int ColX = 7633500, ColW = 1584176;
    public const int StartY = 180783;
    public const int LowerX = 7817724, LowerW = 1326277;

    public static void Build(
        string templatePath, string outputPath, IReadOnlyList<PhotoInfo> photos, IReadOnlyList<FieldSetting> fields,
        string studentId, string name, SlideFonts? fonts = null, double lineSpacing = 1.0)
    {
        fonts ??= SlideFonts.Default;
        var work = Path.Combine(Path.GetTempPath(), "pfp-" + Guid.NewGuid());
        var pkg = Path.Combine(work, "pkg");
        try
        {
            System.IO.Directory.CreateDirectory(pkg);
            ZipFile.ExtractToDirectory(templatePath, pkg);

            // テンプレート内の既存スライド・変更履歴を撤去
            foreach (var d in new[] { "ppt/slides", "ppt/notesSlides", "ppt/changesInfos" })
            {
                var full = Path.Combine(pkg, d);
                if (System.IO.Directory.Exists(full)) System.IO.Directory.Delete(full, true);
            }
            var revisionInfo = Path.Combine(pkg, "ppt", "revisionInfo.xml");
            if (File.Exists(revisionInfo)) File.Delete(revisionInfo);
            foreach (var m in new[] { "image3.jpeg", "image4.jpeg" })
            {
                var f = Path.Combine(pkg, "ppt", "media", m);
                if (File.Exists(f)) File.Delete(f);
            }
            System.IO.Directory.CreateDirectory(Path.Combine(pkg, "ppt", "slides", "_rels"));

            var overrides = new StringBuilder();
            var presRels = new StringBuilder();
            var sldIds = new StringBuilder();

            for (var i = 0; i < photos.Count; i++)
            {
                var n = i + 1;
                var photo = photos[i];
                var imgName = $"photo{n}.jpeg";
                WriteJpeg(photo, Path.Combine(pkg, "ppt", "media", imgName));

                var xml = SlideXml(photo, fields, studentId, name, fonts, lineSpacing);
                WriteUtf8NoBom(Path.Combine(pkg, "ppt", "slides", $"slide{n}.xml"), xml);

                var rels = "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>\n" +
                    "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
                    "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/slideLayout\" Target=\"../slideLayouts/slideLayout37.xml\"/>" +
                    $"<Relationship Id=\"rId2\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/image\" Target=\"../media/{imgName}\"/></Relationships>";
                WriteUtf8NoBom(Path.Combine(pkg, "ppt", "slides", "_rels", $"slide{n}.xml.rels"), rels);

                overrides.Append($"<Override PartName=\"/ppt/slides/slide{n}.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.presentationml.slide+xml\"/>");
                presRels.Append($"<Relationship Id=\"rIdPF{n}\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/slide\" Target=\"slides/slide{n}.xml\"/>");
                sldIds.Append($"<p:sldId id=\"{255 + n}\" r:id=\"rIdPF{n}\"/>");
            }

            Edit(Path.Combine(pkg, "[Content_Types].xml"), s =>
            {
                s = Regex.Replace(s, "<Override PartName=\"/ppt/(slides|notesSlides|changesInfos)/[^\"]*\"[^>]*/>", "");
                s = Regex.Replace(s, "<Override PartName=\"/ppt/revisionInfo\\.xml\"[^>]*/>", "");
                return s.Replace("</Types>", overrides + "</Types>");
            });
            Edit(Path.Combine(pkg, "ppt", "_rels", "presentation.xml.rels"), s =>
            {
                s = Regex.Replace(s, "<Relationship [^>]*Target=\"(slides/|changesInfos/|revisionInfo)[^\"]*\"[^>]*/>", "");
                return s.Replace("</Relationships>", presRels + "</Relationships>");
            });
            Edit(Path.Combine(pkg, "ppt", "presentation.xml"), s =>
                Regex.Replace(s, "<p:sldIdLst>.*?</p:sldIdLst>", $"<p:sldIdLst>{sldIds}</p:sldIdLst>", RegexOptions.Singleline));

            if (File.Exists(outputPath)) File.Delete(outputPath);
            ZipFile.CreateFromDirectory(pkg, outputPath, CompressionLevel.Optimal, includeBaseDirectory: false);
        }
        finally
        {
            if (System.IO.Directory.Exists(work)) System.IO.Directory.Delete(work, true);
        }
    }

    // MARK: - スライド生成

    public static string SlideXml(PhotoInfo photo, IReadOnlyList<FieldSetting> fields, string studentId, string name, SlideFonts fonts, double lineSpacing = 1.0)
    {
        // 写真: 左寄せ・高さいっぱい。横長すぎて右カラムに掛かる場合のみ幅に合わせて縮小
        var aspect = photo.PixelWidth / photo.PixelHeight;
        var w = (int)Math.Round(SlideH * aspect, MidpointRounding.AwayFromZero);
        var h = SlideH;
        if (w > PhotoMaxW)
        {
            w = PhotoMaxW;
            h = (int)Math.Round(PhotoMaxW / aspect, MidpointRounding.AwayFromZero);
        }
        var y0 = (SlideH - h) / 2;

        var shapes = new StringBuilder();
        shapes.Append(
            "<p:pic><p:nvPicPr><p:cNvPr id=\"2\" name=\"写真\"/><p:cNvPicPr><a:picLocks noChangeAspect=\"1\"/></p:cNvPicPr><p:nvPr/></p:nvPicPr>" +
            "<p:blipFill><a:blip r:embed=\"rId2\"/><a:stretch><a:fillRect/></a:stretch></p:blipFill>" +
            $"<p:spPr><a:xfrm><a:off x=\"0\" y=\"{y0}\"/><a:ext cx=\"{w}\" cy=\"{h}\"/></a:xfrm><a:prstGeom prst=\"rect\"><a:avLst/></a:prstGeom></p:spPr></p:pic>");

        // 右カラム: 個人情報(学籍番号/氏名/タイトル)は上から順に積み、
        // 撮影情報はテンプレート通り赤線を境に下部へ固定する(カメラ・レンズが線の上、露出が線の下)
        var id = 10;

        static int Height(FieldKind k, string text) =>
            (int)(EstimateLines(text, k.FontSize()) * k.FontSize() * 1.2 * 12700) + 91440;

        void Emit(FieldKind k, string text, int y, bool left = false)
        {
            shapes.Append(TextShape(id, y, Height(k, text), text, k.FontSize(), left, k == FieldKind.Note, fonts));
            id++;
        }

        var items = fields.Where(f => f.Enabled)
            .Select(f => (f.Kind, Text: LineText(f.Kind, photo, studentId, name)))
            .Where(t => !string.IsNullOrEmpty(t.Text))
            .Select(t => (t.Kind, Text: t.Text!))
            .ToList();

        const int inset = 45720; // テキストボックスの上下余白
        static int Step(FieldKind k, string t, Func<FieldKind, string, int> height) => height(k, t) - 2 * inset;

        var cursor = StartY;
        foreach (var (k, t) in items.Where(it => it.Kind.Group() == 0))
        {
            Emit(k, t, cursor);
            cursor += Step(k, t, Height);
        }

        // 焦点距離: 右カラム中央に大きく(完成図の位置)
        var focalItem = items.FirstOrDefault(it => it.Kind.Group() == 3);
        if (focalItem.Text != null)
        {
            Emit(focalItem.Kind, focalItem.Text, 2744000 - Height(focalItem.Kind, focalItem.Text) / 2);
        }

        var upper = items.Where(it => it.Kind.Group() == 1).ToList();
        var lower = items.Where(it => it.Kind.Group() == 2).ToList();
        if (upper.Count > 0 || lower.Count > 0)
        {
            const int lineY = 4412530;
            shapes.Append(LineShape(id, lineY));
            id++;
            var edge = lineY - 25000; // 本文の下端(線の少し上)
            for (var i = upper.Count - 1; i >= 0; i--)
            {
                var (k, t) = upper[i];
                var y = edge + inset - Height(k, t);
                Emit(k, t, y);
                edge = y + inset;
            }
            var down = lineY + 30000 - inset;
            if (lower.Count > 0) // 線の下は1つのテキストボックスにまとめ、左寄せ
            {
                var lh = LowerHeight(lower, lineSpacing);
                shapes.Append(LowerShape(id, down, lh, lower, fonts, lineSpacing));
                id++;
            }
        }

        return "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>\n" +
            "<p:sld xmlns:a=\"http://schemas.openxmlformats.org/drawingml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\" xmlns:p=\"http://schemas.openxmlformats.org/presentationml/2006/main\">" +
            "<p:cSld><p:spTree><p:nvGrpSpPr><p:cNvPr id=\"1\" name=\"\"/><p:cNvGrpSpPr/><p:nvPr/></p:nvGrpSpPr>" +
            "<p:grpSpPr><a:xfrm><a:off x=\"0\" y=\"0\"/><a:ext cx=\"0\" cy=\"0\"/><a:chOff x=\"0\" y=\"0\"/><a:chExt cx=\"0\" cy=\"0\"/></a:xfrm></p:grpSpPr>" +
            $"{shapes}</p:spTree></p:cSld><p:clrMapOvr><a:masterClrMapping/></p:clrMapOvr></p:sld>";
    }

    public static string? LineText(FieldKind k, PhotoInfo photo, string studentId, string name)
    {
        static string? V(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
        switch (k)
        {
            case FieldKind.StudentId: return V(studentId);
            case FieldKind.Name: return V(name);
            case FieldKind.Title:
                var t = V(photo.Title);
                if (t == null) return null;
                return t.StartsWith("「", StringComparison.Ordinal) ? t : $"「{t}」";
            case FieldKind.Camera: return V(photo.Camera);
            case FieldKind.Lens: return V(photo.Lens);
            case FieldKind.Focal: { var v = V(photo.Focal); return v == null ? null : $"{v}mm"; }
            case FieldKind.Shutter: { var v = V(photo.Shutter); return v == null ? null : $"Shutter Speed: {v} sec"; }
            case FieldKind.Aperture: { var v = V(photo.Aperture); return v == null ? null : $"Aperture: f/{v}"; }
            case FieldKind.Iso: { var v = V(photo.Iso); return v == null ? null : $"ISO: {v}"; }
            case FieldKind.Note: return V(photo.Note);
            default: throw new ArgumentOutOfRangeException(nameof(k));
        }
    }

    public static int LowerHeight(IReadOnlyList<(FieldKind Kind, string Text)> items, double spacing)
    {
        var body = items.Sum(it => EstimateLines(it.Text, it.Kind.FontSize(), LowerW) * it.Kind.FontSize() * 1.2 * spacing);
        return (int)(body * 12700) + 91440;
    }

    /// <summary>撮影情報(シャッタースピード/F値/ISO/備考)を1つのテキストボックスにまとめる。行間は spacing(1.0 = 100%)</summary>
    public static string LowerShape(int id, int y, int h, IReadOnlyList<(FieldKind Kind, string Text)> items, SlideFonts fonts, double spacing)
    {
        var paras = new StringBuilder();
        foreach (var (k, t) in items)
        {
            var sz = (int)(k.FontSize() * 100);
            var marL = k == FieldKind.Note ? " marL=\"54000\"" : "";
            var ppr = $"<a:pPr algn=\"l\"{marL}><a:lnSpc><a:spcPct val=\"{(int)Math.Round(spacing * 100000, MidpointRounding.AwayFromZero)}\"/></a:lnSpc></a:pPr>";
            paras.Append($"<a:p>{ppr}<a:r><a:rPr lang=\"ja-JP\" altLang=\"en-US\" sz=\"{sz}\" dirty=\"0\"><a:solidFill><a:prstClr val=\"white\"/></a:solidFill><a:latin typeface=\"{Esc(fonts.Latin)}\"/><a:ea typeface=\"{Esc(fonts.Ea)}\"/></a:rPr><a:t>{Esc(t)}</a:t></a:r></a:p>");
        }
        return $"<p:sp><p:nvSpPr><p:cNvPr id=\"{id}\" name=\"撮影情報\"/><p:cNvSpPr txBox=\"1\"/><p:nvPr/></p:nvSpPr>" +
            $"<p:spPr><a:xfrm><a:off x=\"{LowerX}\" y=\"{y}\"/><a:ext cx=\"{LowerW}\" cy=\"{h}\"/></a:xfrm><a:prstGeom prst=\"rect\"><a:avLst/></a:prstGeom><a:noFill/></p:spPr>" +
            $"<p:txBody><a:bodyPr wrap=\"square\" rtlCol=\"0\"><a:spAutoFit/></a:bodyPr><a:lstStyle/>{paras}</p:txBody></p:sp>";
    }

    public static int EstimateLines(string s, double size, int colWidth = ColW)
    {
        var avail = colWidth / 12700.0 - 14.4;
        double width = 0, lines = 1;
        foreach (var rune in s.EnumerateRunes())
        {
            var cw = rune.Value < 0x2000 ? size * 0.52 : size;
            if (width + cw > avail) { lines += 1; width = 0; }
            width += cw;
        }
        return (int)lines;
    }

    public static string Esc(string s) =>
        s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");

    public static string TextShape(int id, int y, int h, string text, double size, bool left = false, bool indent = false, SlideFonts? fonts = null)
    {
        fonts ??= SlideFonts.Default;
        var sz = (int)(size * 100);
        // 左寄せはテンプレートの露出情報ボックスと同じ位置・幅(赤線の左端に文字頭を揃える)
        var x = left ? LowerX : ColX;
        var w = left ? LowerW : ColW;
        var ppr = left ? $"<a:pPr algn=\"l\"{(indent ? " marL=\"54000\"" : "")}/>" : "<a:pPr algn=\"ctr\"/>";
        return $"<p:sp><p:nvSpPr><p:cNvPr id=\"{id}\" name=\"テキスト {id}\"/><p:cNvSpPr txBox=\"1\"/><p:nvPr/></p:nvSpPr>" +
            $"<p:spPr><a:xfrm><a:off x=\"{x}\" y=\"{y}\"/><a:ext cx=\"{w}\" cy=\"{h}\"/></a:xfrm><a:prstGeom prst=\"rect\"><a:avLst/></a:prstGeom><a:noFill/></p:spPr>" +
            $"<p:txBody><a:bodyPr wrap=\"square\" rtlCol=\"0\"><a:spAutoFit/></a:bodyPr><a:lstStyle/><a:p>{ppr}<a:r><a:rPr lang=\"ja-JP\" altLang=\"en-US\" sz=\"{sz}\" dirty=\"0\"><a:solidFill><a:prstClr val=\"white\"/></a:solidFill><a:latin typeface=\"{Esc(fonts.Latin)}\"/><a:ea typeface=\"{Esc(fonts.Ea)}\"/></a:rPr><a:t>{Esc(text)}</a:t></a:r></a:p></p:txBody></p:sp>";
    }

    public static string LineShape(int id, int y) =>
        $"<p:cxnSp><p:nvCxnSpPr><p:cNvPr id=\"{id}\" name=\"直線 {id}\"/><p:cNvCxnSpPr/><p:nvPr/></p:nvCxnSpPr>" +
        $"<p:spPr><a:xfrm><a:off x=\"7817723\" y=\"{y}\"/><a:ext cx=\"1215730\" cy=\"0\"/></a:xfrm><a:prstGeom prst=\"line\"><a:avLst/></a:prstGeom><a:ln><a:solidFill><a:srgbClr val=\"FF6600\"/></a:solidFill></a:ln></p:spPr></p:cxnSp>";

    // MARK: - 補助

    /// <summary>JPEGで向き補正が不要ならそのまま、それ以外(回転ありなど)は向きを焼き込んでJPEG化</summary>
    public static void WriteJpeg(PhotoInfo photo, string dest)
    {
        var ext = Path.GetExtension(photo.FilePath).ToLowerInvariant();
        if ((ext == ".jpg" || ext == ".jpeg") && photo.Orientation == 1)
        {
            File.Copy(photo.FilePath, dest, true);
            return;
        }

        try
        {
            using var src = Image.FromFile(photo.FilePath);
            ApplyOrientation(src, photo.Orientation);
            var encoder = JpegEncoderInfo();
            using var parameters = new EncoderParameters(1);
            parameters.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 95L);
            src.Save(dest, encoder, parameters);
        }
        catch (Exception ex) when (ex is not PptxBuildException)
        {
            throw new PptxBuildException($"画像を処理できません: {Path.GetFileName(photo.FilePath)}");
        }
    }

    private static void ApplyOrientation(Image img, int orientation)
    {
        var flip = orientation switch
        {
            2 => RotateFlipType.RotateNoneFlipX,
            3 => RotateFlipType.Rotate180FlipNone,
            4 => RotateFlipType.RotateNoneFlipY,
            5 => RotateFlipType.Rotate90FlipX,
            6 => RotateFlipType.Rotate90FlipNone,
            7 => RotateFlipType.Rotate270FlipX,
            8 => RotateFlipType.Rotate270FlipNone,
            _ => RotateFlipType.RotateNoneFlipNone,
        };
        if (flip != RotateFlipType.RotateNoneFlipNone) img.RotateFlip(flip);
    }

    private static ImageCodecInfo JpegEncoderInfo() =>
        ImageCodecInfo.GetImageEncoders().First(c => c.FormatID == ImageFormat.Jpeg.Guid);

    private static void Edit(string path, Func<string, string> transform)
    {
        var s = File.ReadAllText(path);
        s = transform(s);
        WriteUtf8NoBom(path, s);
    }

    private static void WriteUtf8NoBom(string path, string content) =>
        File.WriteAllText(path, content, new UTF8Encoding(false));
}
