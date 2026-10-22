import AppKit
import ImageIO
import UniformTypeIdentifiers

enum FieldKind: String, CaseIterable, Codable, Identifiable {
    case studentID, name, title, camera, lens, focal, shutter, aperture, iso, note
    var id: String { rawValue }

    var label: String {
        switch self {
        case .studentID: return "学籍番号"
        case .name: return "氏名"
        case .title: return "作品タイトル"
        case .camera: return "カメラ名"
        case .lens: return "レンズ名"
        case .focal: return "焦点距離"
        case .shutter: return "シャッタースピード"
        case .aperture: return "F値"
        case .iso: return "ISO"
        case .note: return "備考(Step Cropなど)"
        }
    }

    /// 文字サイズ(pt)。テンプレートに合わせる
    var fontSize: Double {
        switch self {
        case .studentID, .name: return 10.5
        case .title: return 11
        case .camera, .lens: return 8
        case .focal: return 20
        case .shutter, .aperture, .iso, .note: return 7.5
        }
    }

    /// 同じグループ内は詰め、グループが変わるときだけ間隔を空ける
    var group: Int {
        switch self {
        case .studentID, .name, .title: return 0
        case .camera, .lens: return 1
        case .focal: return 3       // 右カラム中央に大きく
        case .shutter, .aperture, .iso, .note: return 2   // 赤線の下(左寄せ)
        }
    }
}

struct FieldSetting: Codable, Identifiable, Equatable {
    var kind: FieldKind
    var enabled: Bool
    var id: String { kind.rawValue }
}

final class Photo: ObservableObject, Identifiable {
    let id = UUID()
    let url: URL
    let thumbnail: NSImage?
    let pixelSize: CGSize      // 向き補正後
    let orientation: Int

    @Published var title = ""
    @Published var camera = ""
    @Published var lens = ""
    @Published var focal = ""
    @Published var shutter = ""
    @Published var aperture = ""
    @Published var iso = ""
    @Published var note = ""

    init?(url: URL, cameras: MapTable, lenses: MapTable) {
        guard let src = CGImageSourceCreateWithURL(url as CFURL, nil),
              let p = CGImageSourceCopyPropertiesAtIndex(src, 0, nil) as? [CFString: Any],
              let w = p[kCGImagePropertyPixelWidth] as? Double,
              let h = p[kCGImagePropertyPixelHeight] as? Double else { return nil }
        self.url = url
        let o = (p[kCGImagePropertyOrientation] as? Int) ?? 1
        orientation = o
        pixelSize = (5...8).contains(o) ? CGSize(width: h, height: w) : CGSize(width: w, height: h)

        let opts: [CFString: Any] = [
            kCGImageSourceCreateThumbnailFromImageAlways: true,
            kCGImageSourceCreateThumbnailWithTransform: true,
            kCGImageSourceThumbnailMaxPixelSize: 240,
        ]
        if let cg = CGImageSourceCreateThumbnailAtIndex(src, 0, opts as CFDictionary) {
            thumbnail = NSImage(cgImage: cg, size: .zero)
        } else {
            thumbnail = nil
        }

        let exif = p[kCGImagePropertyExifDictionary] as? [CFString: Any] ?? [:]
        let tiff = p[kCGImagePropertyTIFFDictionary] as? [CFString: Any] ?? [:]

        if let model = tiff[kCGImagePropertyTIFFModel] as? String {
            camera = Photo.cameraText(model: model, table: cameras)
        }
        if let l = exif[kCGImagePropertyExifLensModel] as? String {
            lens = lenses.lookup(l) ?? l
        }
        if let f = exif[kCGImagePropertyExifFocalLength] as? Double {
            // デジタルズーム(RX1R IIIのStep Cropなど)は実効焦点距離に掛ける
            let z = (exif[kCGImagePropertyExifDigitalZoomRatio] as? Double) ?? 1
            focal = String(Int((f * (z > 1 ? z : 1)).rounded()))
            if z > 1.01 { note = "*Step Crop: \(focal)mm equiv." }   // 備考を有効にしたときの初期値
        }
        if let t = exif[kCGImagePropertyExifExposureTime] as? Double { shutter = Photo.shutterText(t) }
        if let a = exif[kCGImagePropertyExifFNumber] as? Double { aperture = Photo.trim(a) }
        if let i = (exif[kCGImagePropertyExifISOSpeedRatings] as? [Int])?.first { iso = String(i) }
    }

    static func cameraText(model: String, table: MapTable) -> String {
        if let name = table.lookup(model) { return name == model ? model : "\(name) / \(model)" }
        return model
    }

    static func trim(_ v: Double) -> String {
        let s = String(format: "%.1f", v)
        return s.hasSuffix(".0") ? String(s.dropLast(2)) : s
    }

    static func shutterText(_ t: Double) -> String {
        if t >= 1 { return trim(t) }
        let f = NumberFormatter()
        f.numberStyle = .decimal
        return "1/" + (f.string(from: NSNumber(value: Int((1 / t).rounded()))) ?? "\(Int((1 / t).rounded()))")
    }
}

@MainActor
final class AppModel: ObservableObject {
    @Published var photos: [Photo] = []
    @Published var fields: [FieldSetting] { didSet { save() } }
    @Published var studentID: String { didSet { save() } }
    @Published var name: String { didSet { save() } }
    @Published var status = ""
    @Published var update: ReleaseCheck.Found?
    @Published var updating = false

    let cameras = MapTable(fileName: "cameras.json", defaults: DefaultTables.cameras, version: DefaultTables.version)
    let lenses = MapTable(fileName: "lenses.json", defaults: DefaultTables.lenses, version: DefaultTables.version)

    init() {
        let d = UserDefaults.standard
        studentID = d.string(forKey: "studentID") ?? ""
        name = d.string(forKey: "name") ?? ""
        let defaultFields: [FieldSetting] = [
            .init(kind: .camera, enabled: true), .init(kind: .lens, enabled: true),
            .init(kind: .focal, enabled: true), .init(kind: .shutter, enabled: true),
            .init(kind: .aperture, enabled: true), .init(kind: .iso, enabled: true),
            .init(kind: .studentID, enabled: false), .init(kind: .name, enabled: false),
            .init(kind: .title, enabled: false), .init(kind: .note, enabled: false),
        ]
        if let data = d.data(forKey: "fields"),
           var saved = try? JSONDecoder().decode([FieldSetting].self, from: data) {
            for f in defaultFields where !saved.contains(where: { $0.kind == f.kind }) { saved.append(f) }
            fields = saved
        } else {
            fields = defaultFields
        }
    }

    private func save() {
        let d = UserDefaults.standard
        d.set(studentID, forKey: "studentID")
        d.set(name, forKey: "name")
        if let data = try? JSONEncoder().encode(fields) { d.set(data, forKey: "fields") }
    }

    func add(urls: [URL]) {
        for u in urls {
            if let p = Photo(url: u, cameras: cameras, lenses: lenses) { photos.append(p) }
        }
    }

    func openPanel() {
        let panel = NSOpenPanel()
        panel.allowsMultipleSelection = true
        panel.canChooseDirectories = false
        panel.allowedContentTypes = [.image]
        if panel.runModal() == .OK { add(urls: panel.urls) }
    }

    func export() {
        guard !photos.isEmpty else { return }
        let panel = NSSavePanel()
        panel.allowedContentTypes = [UTType(filenameExtension: "pptx")!]
        panel.nameFieldStringValue = "写真投稿フォーラム.pptx"
        panel.directoryURL = FileManager.default.urls(for: .downloadsDirectory, in: .userDomainMask).first
        guard panel.runModal() == .OK, let out = panel.url else { return }
        do {
            let template = Bundle.main.url(forResource: "template", withExtension: "pptx")
                ?? URL(fileURLWithPath: FileManager.default.currentDirectoryPath + "/Resources/template.pptx")
            try PPTXBuilder.build(template: template, output: out, photos: photos, fields: fields,
                                  studentID: studentID, name: name)
            status = "書き出しました: \(out.lastPathComponent)"
            NSWorkspace.shared.activateFileViewerSelecting([out])
        } catch {
            status = "失敗: \(error.localizedDescription)"
        }
    }

    /// 最新版の確認。manual=true のときは「最新です」「失敗」も表示する
    func checkForUpdate(manual: Bool = false) async {
        do {
            if let f = try await ReleaseCheck.newer(than: Updater.currentVersion) {
                update = f
            } else if manual {
                status = "最新版です (v\(Updater.currentVersion))"
            }
        } catch {
            if manual { status = "更新の確認に失敗しました: \(error.localizedDescription)" }
        }
    }

    /// ボタンひとつ: ダウンロード → 入れ替え → 再起動
    func installUpdate() async {
        guard let f = update, !updating else { return }
        updating = true
        defer { updating = false }
        do {
            let zip = try await ReleaseCheck.download(f.zipURL)
            Updater.install(zip: zip, confirm: false)
        } catch {
            status = "ダウンロードに失敗しました: \(error.localizedDescription)"
        }
    }
}
