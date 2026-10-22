import SwiftUI
import AppKit

struct ReleaseNote: Identifiable {
    let version: String
    let items: [String]
    var image: String? = nil          // Resources/whatsnew/<image>.png
    var id: String { version }
}

/// 更新内容。新しい版を出すときは、先頭に1件足す(スクリーンショットは Resources/whatsnew/ に置く)
enum ReleaseNotes {
    static let all: [ReleaseNote] = [
        ReleaseNote(version: "1.5", items: [
            "アップデート後に「新機能」を表示するようになりました(このウィンドウ)",
            "メニュー Help → 新機能 で、いつでも見返せます",
            "カメラ名の既定値を拡充(RX100 / RX10 / RX1 / RX0 / FX シリーズなど)",
        ]),
        ReleaseNote(version: "1.4", items: [
            "設定(⌘,)にフォントの選択を追加。欧文と和文を別々に選べます",
        ], image: "fonts"),
        ReleaseNote(version: "1.3", items: [
            "Help メニューに「使い方」と、GitHub Issue(不具合・要望)への導線を追加",
        ], image: "help"),
        ReleaseNote(version: "1.2", items: [
            "起動時に新しい版を確認し、バナーの「今すぐ更新」ボタンだけで更新できるように",
        ]),
        ReleaseNote(version: "1.1", items: [
            "焦点距離を右カラム中央に大きく表示",
            "赤線の下(SS・F値・ISO)を左寄せに",
            "備考欄(*Step Crop など)を追加。デジタルズームで撮った写真は自動入力",
            "レンズ名の既定値に Sonnar T* 35mm f/2.0 を追加",
        ]),
    ]

    /// last より新しく、current 以下の版(新しい順)
    static func between(_ last: String, _ current: String) -> [ReleaseNote] {
        all.filter { ReleaseCheck.isNewer($0.version, than: last) && !ReleaseCheck.isNewer($0.version, than: current) }
    }
}

struct WhatsNewView: View {
    let notes: [ReleaseNote]
    var onClose: () -> Void

    var body: some View {
        VStack(spacing: 0) {
            ScrollView {
                VStack(alignment: .leading, spacing: 18) {
                    HStack(spacing: 10) {
                        Image(nsImage: NSApp.applicationIconImage).resizable().frame(width: 44, height: 44)
                        VStack(alignment: .leading) {
                            Text("新機能").font(.title).bold()
                            Text("v\(notes.first?.version ?? "") にアップデートしました").foregroundStyle(.secondary)
                        }
                    }
                    ForEach(notes) { n in
                        VStack(alignment: .leading, spacing: 6) {
                            Text("v\(n.version)").font(.headline)
                            ForEach(n.items, id: \.self) { Text("・" + $0).fixedSize(horizontal: false, vertical: true) }
                            if let name = n.image, let img = Self.image(name) {
                                Image(nsImage: img).resizable().scaledToFit()
                                    .frame(maxWidth: 460)
                                    .overlay(RoundedRectangle(cornerRadius: 6).stroke(Color.secondary.opacity(0.4)))
                                    .padding(.top, 4)
                            }
                        }
                    }
                }
                .padding(24).frame(maxWidth: .infinity, alignment: .leading)
            }
            Divider()
            HStack { Spacer(); Button("OK", action: onClose).keyboardShortcut(.defaultAction) }.padding(12)
        }
        .frame(width: 540, height: 560)
    }

    static func image(_ name: String) -> NSImage? {
        Bundle.main.url(forResource: name, withExtension: "png", subdirectory: "whatsnew").flatMap { NSImage(contentsOf: $0) }
    }
}
