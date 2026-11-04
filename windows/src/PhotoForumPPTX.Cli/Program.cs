using PhotoForumPPTX.Core;

if (args.Length < 3)
{
    Console.Error.WriteLine("使い方: PhotoForumPPTX.Cli <template.pptx> <output.pptx> <photo1> [photo2 ...] [--student 学籍番号] [--name 氏名]");
    return 1;
}

var templatePath = args[0];
var outputPath = args[1];

var photoPaths = new List<string>();
string studentId = "", name = "";
for (var i = 2; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--student" when i + 1 < args.Length:
            studentId = args[++i];
            break;
        case "--name" when i + 1 < args.Length:
            name = args[++i];
            break;
        default:
            photoPaths.Add(args[i]);
            break;
    }
}

if (!File.Exists(templatePath))
{
    Console.Error.WriteLine($"テンプレートが見つかりません: {templatePath}");
    return 1;
}

var cameras = new MapTable("cameras.json", DefaultTables.Cameras, DefaultTables.Version);
var lenses = new MapTable("lenses.json", DefaultTables.Lenses, DefaultTables.Version);

var photos = new List<PhotoInfo>();
foreach (var path in photoPaths)
{
    var photo = PhotoInfo.Load(path, cameras, lenses);
    if (photo != null) photos.Add(photo);
    else Console.Error.WriteLine($"読み込み失敗(スキップ): {path}");
}

if (photos.Count == 0)
{
    Console.Error.WriteLine("有効な写真がありません");
    return 1;
}

var fields = new List<FieldSetting>
{
    new(FieldKind.Camera, true),
    new(FieldKind.Lens, true),
    new(FieldKind.Focal, true),
    new(FieldKind.Shutter, true),
    new(FieldKind.Aperture, true),
    new(FieldKind.Iso, true),
    new(FieldKind.StudentId, studentId.Length > 0),
    new(FieldKind.Name, name.Length > 0),
    new(FieldKind.Title, false),
    new(FieldKind.Note, true),
};

try
{
    PptxBuilder.Build(templatePath, outputPath, photos, fields, studentId, name);
    Console.WriteLine($"書き出しました: {outputPath} ({photos.Count}枚)");
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"失敗: {ex.Message}");
    return 1;
}
