import SwiftUI
import AppKit
import QuickLookThumbnailing

/// 設定画面用: 実際の書き出しと同じ処理でサンプルのpptxを作り、QuickLookで1枚目を画像にする
@MainActor
enum LayoutPreview {
    static func render(model: AppModel, layout: SlideLayout, twoUp: Bool) async -> NSImage? {
        let fm = FileManager.default
        let dir = fm.temporaryDirectory.appendingPathComponent("pfp-preview-\(UUID().uuidString)")
        defer { try? fm.removeItem(at: dir) }
        guard (try? fm.createDirectory(at: dir, withIntermediateDirectories: true)) != nil else { return nil }

        var photos: [Photo] = []
        for (i, hue) in [(0, 0.58), (1, 0.95)].prefix(twoUp ? 2 : 1) {
            let url = dir.appendingPathComponent("sample\(i).jpg")
            guard writeSample(to: url, hue: hue, size: twoUp ? CGSize(width: 600, height: 800) : CGSize(width: 1200, height: 800)),
                  let p = Photo(url: url, cameras: model.cameras, lenses: model.lenses) else { return nil }
            p.camera = "α1 II / ILCE-1M2"; p.lens = "FE 50mm F1.2 GM"; p.focal = "50"
            p.shutter = "1/800"; p.aperture = "1.4"; p.iso = "400"
            photos.append(p)
        }
        if twoUp { photos[0].pairWithNext = true }

        // 情報欄の項目は、プレビューでは違いが分かるように全部オンにする
        let fields: [FieldSetting] = FieldKind.allCases.filter { $0 != .title && $0 != .note }.map { .init(kind: $0, enabled: true) }
        let template = Bundle.main.url(forResource: "template", withExtension: "pptx")
            ?? URL(fileURLWithPath: fm.currentDirectoryPath + "/Resources/template.pptx")
        let out = dir.appendingPathComponent("preview.pptx")
        do {
            try PPTXBuilder.build(template: template, output: out, photos: photos, fields: fields, studentID: "5226C019", name: "鈴木 柾孝",
                                  fonts: model.fonts, lineSpacing: model.lineSpacing, style: model.style, layout: layout)
        } catch { return nil }

        let req = QLThumbnailGenerator.Request(fileAt: out, size: CGSize(width: 960, height: 540), scale: 2, representationTypes: .thumbnail)
        return try? await QLThumbnailGenerator.shared.generateBestRepresentation(for: req).nsImage
    }

    private static func writeSample(to url: URL, hue: Double, size: CGSize) -> Bool {
        let img = NSImage(size: size, flipped: false) { rect in
            NSGradient(colors: [NSColor(hue: hue, saturation: 0.45, brightness: 0.4, alpha: 1),
                                NSColor(hue: hue + 0.12, saturation: 0.45, brightness: 0.9, alpha: 1)])?.draw(in: rect, angle: 90)
            return true
        }
        guard let tiff = img.tiffRepresentation, let rep = NSBitmapImageRep(data: tiff),
              let data = rep.representation(using: .jpeg, properties: [.compressionFactor: 0.8]) else { return false }
        return (try? data.write(to: url)) != nil
    }
}

/// 情報欄のデザインを選ぶ。選んだ設定(背景色・フォント・行間)を反映したプレビュー付き
struct LayoutSettings: View {
    @EnvironmentObject var model: AppModel
    @State private var twoUp = false
    @State private var image: NSImage?

    private var key: String { "\(model.layout)\(twoUp)\(model.bgHex)\(model.fonts.latin)\(model.fonts.ea)\(model.lineSpacing)" }

    var body: some View {
        Form {
            Picker("情報欄のデザイン", selection: $model.layout) {
                ForEach(SlideLayout.allCases) { Text($0.label).tag($0) }
            }.pickerStyle(.segmented)
            Picker("プレビュー", selection: $twoUp) {
                Text("1枚").tag(false)
                Text("2段組み").tag(true)
            }.pickerStyle(.segmented).frame(width: 220)
            ZStack {
                Color.secondary.opacity(0.12)
                if let image { Image(nsImage: image).resizable().scaledToFit() } else { ProgressView() }
            }
            .aspectRatio(16.0 / 9.0, contentMode: .fit)
            .frame(maxWidth: 480)
            .overlay(Rectangle().stroke(Color.secondary.opacity(0.4)))
            Text(model.layout == .forum
                 ? "フォーラム標準: 焦点距離を右の列の中央に大きく表示します。2段組みの写真は、どちらを選んでもコンパクトで出力されます。"
                 : "コンパクト: カメラ名を大きく、レンズ名と露出情報(焦点距離を含む)を小さくまとめます。")
                .font(.caption).foregroundStyle(.secondary)
        }
        .padding()
        .frame(maxHeight: .infinity, alignment: .top)
        .task(id: key) { image = await LayoutPreview.render(model: model, layout: model.layout, twoUp: twoUp) }
    }
}
