using System.IO.Compression;
using PhotoForumPPTX.Core;

namespace PhotoForumPPTX.Core.Tests;

public class PptxBuilderTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "pfp-test-" + Guid.NewGuid());

    public PptxBuilderTests() => Directory.CreateDirectory(_dir);

    public void Dispose()
    {
        if (Directory.Exists(_dir)) Directory.Delete(_dir, true);
    }

    [Fact]
    public void Esc_EscapesXmlSpecialCharacters()
    {
        Assert.Equal("a &amp; b &lt;c&gt;", PptxBuilder.Esc("a & b <c>"));
    }

    [Fact]
    public void LineText_WrapsTitleInJapaneseQuotesUnlessAlreadyPresent()
    {
        var photo = PhotoInfo.ForTesting(Path.Combine(_dir, "x.jpg"), 100, 100, 1);
        photo.Title = "夕焼け";
        Assert.Equal("「夕焼け」", PptxBuilder.LineText(FieldKind.Title, photo, "", ""));

        photo.Title = "「すでに引用符」";
        Assert.Equal("「すでに引用符」", PptxBuilder.LineText(FieldKind.Title, photo, "", ""));
    }

    [Fact]
    public void LineText_FormatsExposureFields()
    {
        var photo = PhotoInfo.ForTesting(Path.Combine(_dir, "x.jpg"), 100, 100, 1);
        photo.Shutter = "1/125";
        photo.Aperture = "2.8";
        photo.Iso = "400";
        Assert.Equal("Shutter Speed: 1/125 sec", PptxBuilder.LineText(FieldKind.Shutter, photo, "", ""));
        Assert.Equal("Aperture: f/2.8", PptxBuilder.LineText(FieldKind.Aperture, photo, "", ""));
        Assert.Equal("ISO: 400", PptxBuilder.LineText(FieldKind.Iso, photo, "", ""));
    }

    [Fact]
    public void LineText_ReturnsNullForBlankValues()
    {
        var photo = PhotoInfo.ForTesting(Path.Combine(_dir, "x.jpg"), 100, 100, 1);
        Assert.Null(PptxBuilder.LineText(FieldKind.Camera, photo, "", ""));
        Assert.Null(PptxBuilder.LineText(FieldKind.StudentId, photo, "   ", ""));
    }

    [Fact]
    public void EstimateLines_WrapsWhenTextExceedsColumnWidth()
    {
        Assert.Equal(1, PptxBuilder.EstimateLines("short", 10));
        Assert.True(PptxBuilder.EstimateLines(new string('x', 200), 10) > 1);
    }

    [Fact]
    public void Build_ProducesOneSlidePerPhotoWithEmbeddedImages()
    {
        var templatePath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "template.pptx");
        var samplePath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "sample.jpg");
        Assert.True(File.Exists(templatePath), "テンプレートが見つかりません: " + templatePath);
        Assert.True(File.Exists(samplePath), "サンプル画像が見つかりません: " + samplePath);

        var photo1 = PhotoInfo.ForTesting(samplePath, 320, 240, 1);
        photo1.Camera = "α7 IV";
        photo1.Lens = "FE 35mm F1.4 GM";
        photo1.Focal = "35";
        photo1.Shutter = "1/250";
        photo1.Aperture = "1.4";
        photo1.Iso = "200";

        var photo2 = PhotoInfo.ForTesting(samplePath, 240, 320, 6); // 90度回転
        photo2.Camera = "α7 IV";

        var fields = new List<FieldSetting>
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

        var output = Path.Combine(_dir, "out.pptx");
        PptxBuilder.Build(templatePath, output, new[] { photo1, photo2 }, fields, "", "");

        Assert.True(File.Exists(output));
        using var zip = ZipFile.OpenRead(output);
        var names = zip.Entries.Select(e => e.FullName.Replace('\\', '/')).ToHashSet();
        Assert.Contains("ppt/slides/slide1.xml", names);
        Assert.Contains("ppt/slides/slide2.xml", names);
        Assert.DoesNotContain("ppt/slides/slide3.xml", names);
        Assert.Contains("ppt/media/photo1.jpeg", names);
        Assert.Contains("ppt/media/photo2.jpeg", names);

        var presentation = ReadEntry(zip, "ppt/presentation.xml");
        Assert.Contains("rIdPF1", presentation);
        Assert.Contains("rIdPF2", presentation);
    }

    private static string ReadEntry(ZipArchive zip, string name)
    {
        var entry = zip.GetEntry(name) ?? throw new InvalidOperationException($"entry not found: {name}");
        using var reader = new StreamReader(entry.Open());
        return reader.ReadToEnd();
    }
}
