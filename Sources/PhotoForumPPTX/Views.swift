import SwiftUI

@main
struct PhotoForumApp: App {
    @StateObject private var model = AppModel()

    var body: some Scene {
        WindowGroup {
            ContentView().environmentObject(model).frame(minWidth: 820, minHeight: 560)
        }
        .commands {
            CommandGroup(after: .appInfo) {
                Button("アップデートを確認…") { Task { await model.checkForUpdate(manual: true) } }
                Button("zipファイルからアップデート…") { Updater.chooseZip() }
            }
            HelpCommands()
        }
        Window("使い方", id: "help") { HelpView() }
        Window("新機能", id: "whatsnew") { WhatsNewWindow() }
        Settings {
            SettingsView().environmentObject(model)
        }
    }
}

struct ContentView: View {
    @EnvironmentObject var model: AppModel
    @State private var targeted = false
    @State private var gapTarget: Int?
    @State private var slotTarget: UUID?

    var body: some View {
        VStack(spacing: 0) {
            if let u = model.update { updateBanner(u) }
            HStack(spacing: 0) {
                photoList
                Divider()
                sidebar.frame(width: 260)
            }
        }
        .task { await model.checkForUpdate() }
        .sheet(isPresented: Binding(get: { !model.pendingNotes.isEmpty }, set: { if !$0 { model.dismissNotes() } })) {
            WhatsNewView(notes: model.pendingNotes) { model.dismissNotes() }
        }
        .overlay(alignment: .bottom) {
            if !model.status.isEmpty {
                Text(model.status).padding(8).background(.regularMaterial, in: Capsule()).padding(.bottom, 8)
            }
        }
        .onDrop(of: [.fileURL], isTargeted: $targeted) { providers in
            for p in providers {
                _ = p.loadObject(ofClass: URL.self) { url, _ in
                    guard let url else { return }
                    Task { @MainActor in
                        // zip をドロップしたらアップデートとして扱う
                        if url.pathExtension.lowercased() == "zip" { Updater.install(zip: url) } else { model.add(urls: [url]) }
                    }
                }
            }
            return true
        }
        .overlay { if targeted { RoundedRectangle(cornerRadius: 8).stroke(Color.accentColor, lineWidth: 3).padding(4) } }
    }

    func updateBanner(_ u: ReleaseCheck.Found) -> some View {
        HStack {
            Image(systemName: "arrow.down.circle.fill")
            Text("新しい版 v\(u.version) があります(現在 v\(Updater.currentVersion))")
            Spacer()
            if model.updating { ProgressView().controlSize(.small); Text("更新中…") }
            else {
                Button("あとで") { model.update = nil }
                Button("今すぐ更新") { Task { await model.installUpdate() } }.buttonStyle(.borderedProminent)
            }
        }
        .padding(8).background(Color.accentColor.opacity(0.18))
    }

    var photoList: some View {
        VStack(spacing: 0) {
            HStack {
                Button("写真を選択…") { model.openPanel() }
                Text("(ドラッグ&ドロップも可 / 新しいzipを落とすとアップデート)").foregroundStyle(.secondary).font(.caption)
                Spacer()
                Button("書き出し (.pptx)") { model.export() }
                    .buttonStyle(.borderedProminent).disabled(model.photos.isEmpty)
            }.padding(10)
            Divider()
            if model.photos.isEmpty {
                Text("写真を選ぶと、EXIFを読み取って1枚1ページで並べます(写真をドラッグして2枚並べも可)").foregroundStyle(.secondary)
                    .frame(maxWidth: .infinity, maxHeight: .infinity)
            } else {
                ScrollView {
                    let slides = model.slides
                    LazyVStack(spacing: 0) {
                        ForEach(Array(slides.enumerated()), id: \.offset) { i, group in
                            gap(before: i)
                            slideCard(group, number: i + 1)
                        }
                        gap(before: slides.count)
                    }
                    .padding(.horizontal, 10)
                }
            }
        }
    }

    /// スライドの間。ここにドロップすると、その位置に単独スライドとして入る
    func gap(before index: Int) -> some View {
        Color.clear.frame(height: 14)
            .overlay(Capsule().fill(Color.accentColor).frame(height: 3).opacity(gapTarget == index ? 1 : 0))
            .contentShape(Rectangle())
            .dropDestination(for: String.self) { items, _ in
                guard let id = items.first.flatMap(UUID.init(uuidString:)) else { return false }
                model.place(id, beforeSlide: index); return true
            } isTargeted: { gapTarget = $0 ? index : (gapTarget == index ? nil : gapTarget) }
    }

    /// 1スライド分。写真が1枚なら右に「並べる」ためのドロップ枠を出す
    func slideCard(_ group: [Photo], number: Int) -> some View {
        VStack(alignment: .leading, spacing: 4) {
            Text("スライド \(number)").font(.caption).foregroundStyle(.secondary)
            HStack(alignment: .top, spacing: 10) {
                ForEach(group) { p in
                    PhotoRow(photo: p, compact: group.count == 2, onRemove: { model.remove(p.id) })
                        .frame(maxWidth: .infinity, alignment: .topLeading)
                }
                if group.count == 1, let only = group.first {
                    pairSlot(for: only)
                }
            }
        }
        .padding(8)
        .background(RoundedRectangle(cornerRadius: 8).fill(Color.secondary.opacity(0.08)))
    }

    func pairSlot(for target: Photo) -> some View {
        RoundedRectangle(cornerRadius: 6)
            .strokeBorder(style: StrokeStyle(lineWidth: 1.5, dash: [5]))
            .foregroundStyle(slotTarget == target.id ? Color.accentColor : Color.secondary.opacity(0.5))
            .frame(width: 96, height: 80)
            .overlay(Text("写真をここに\nドラッグして\n左右に並べる").font(.caption2).multilineTextAlignment(.center).foregroundStyle(.secondary))
            .dropDestination(for: String.self) { items, _ in
                guard let id = items.first.flatMap(UUID.init(uuidString:)) else { return false }
                model.place(id, besides: target.id); return true
            } isTargeted: { slotTarget = $0 ? target.id : (slotTarget == target.id ? nil : slotTarget) }
    }

    var sidebar: some View {
        VStack(alignment: .leading, spacing: 10) {
            Text("スライドに入れる項目").font(.headline)
            Text("学籍番号・氏名・タイトルは上から順に配置。カメラ・レンズ・露出情報は下部(赤線の上下)、焦点距離は右カラム中央に固定です。")
                .font(.caption).foregroundStyle(.secondary)
            List {
                ForEach($model.fields) { $f in
                    Toggle(f.kind.label, isOn: $f.enabled)
                }
                .onMove { model.fields.move(fromOffsets: $0, toOffset: $1) }
            }
            .frame(minHeight: 260)
            if model.fields.first(where: { $0.kind == .studentID })?.enabled == true {
                TextField("学籍番号", text: $model.studentID)
            }
            if model.fields.first(where: { $0.kind == .name })?.enabled == true {
                TextField("氏名", text: $model.name)
            }
            Spacer()
        }.padding(10)
    }
}

struct PhotoRow: View {
    @ObservedObject var photo: Photo
    @EnvironmentObject var model: AppModel
    /// 2段組みのとき。幅が半分になるので縦に並べる
    var compact = false
    var onRemove: () -> Void

    var body: some View {
        let layout = compact ? AnyLayout(VStackLayout(alignment: .leading, spacing: 8)) : AnyLayout(HStackLayout(alignment: .top, spacing: 12))
        layout {
            Group {
                if let t = photo.thumbnail { Image(nsImage: t).resizable().scaledToFit() } else { Color.gray }
            }
            .frame(width: compact ? nil : 110, height: 80).frame(maxWidth: compact ? .infinity : nil).background(Color.black)
            .draggable(photo.id.uuidString) {
                if let t = photo.thumbnail { Image(nsImage: t).resizable().scaledToFit().frame(width: 90, height: 66) }
            }
            .help("ドラッグして並べ替え・2段組み")
            VStack(alignment: .leading, spacing: 4) {
                HStack {
                    Text(photo.url.lastPathComponent).font(.caption).foregroundStyle(.secondary).lineLimit(1)
                    Spacer()
                    Button(role: .destructive, action: onRemove) { Image(systemName: "xmark.circle") }.buttonStyle(.borderless)
                }
                if model.fields.first(where: { $0.kind == .title })?.enabled == true {
                    TextField("作品タイトル", text: $photo.title)
                }
                if model.fields.first(where: { $0.kind == .note })?.enabled == true {
                    TextField("備考 (例: *Step Crop: 70mm equiv.)", text: $photo.note)
                }
                if compact {
                    suggestField("カメラ", $photo.camera, cameraCandidates)
                    SuggestField(label: "レンズ", text: $photo.lens, candidates: lensCandidates, allowsBreak: true, onCommit: model.rememberLens)
                    HStack { field("焦点距離(mm)", $photo.focal); field("SS", $photo.shutter) }
                    HStack { field("F", $photo.aperture); field("ISO", $photo.iso) }
                } else {
                    Grid(alignment: .leading, horizontalSpacing: 6, verticalSpacing: 3) {
                        GridRow { suggestField("カメラ", $photo.camera, cameraCandidates); SuggestField(label: "レンズ", text: $photo.lens, candidates: lensCandidates, allowsBreak: true, onCommit: model.rememberLens) }
                        GridRow {
                            HStack { field("焦点距離(mm)", $photo.focal); field("SS", $photo.shutter) }
                            HStack { field("F", $photo.aperture); field("ISO", $photo.iso) }
                        }
                    }
                }
            }
        }.padding(.vertical, 4)
    }

    /// 対応表の表示名(カメラは「表示名 / 型番」)
    var cameraCandidates: [String] {
        model.cameras.entries.filter { !$0.key.isEmpty }.map { e in
            e.value.isEmpty || e.value == e.key ? e.key : "\(e.value) / \(e.key)"
        }
    }

    /// 対応表の表示名 + 今読み込んでいる写真のEXIFレンズ名(重複除く)
    var lensCandidates: [String] {
        var out: [String] = []
        for c in model.lenses.entries.map(\.value) + model.rememberedLenses + model.photos.map(\.lens) where !c.isEmpty && !out.contains(c) { out.append(c) }
        return out
    }

    func suggestField(_ label: String, _ b: Binding<String>, _ candidates: [String]) -> some View {
        SuggestField(label: label, text: b, candidates: candidates)
    }

    func field(_ label: String, _ b: Binding<String>) -> some View {
        TextField(label, text: b).textFieldStyle(.roundedBorder).font(.caption)
    }
}

struct SettingsView: View {
    @EnvironmentObject var model: AppModel
    var body: some View {
        TabView {
            GeneralSettings().tabItem { Label("一般", systemImage: "gearshape") }
            TableEditor(title: "EXIFのカメラ型番 → 表示名\n(例: ILCE-1M2 → α1 II。スライドには「α1 II / ILCE-1M2」と出ます)",
                        keyLabel: "EXIFの型番", valueLabel: "表示名", table: model.cameras)
                .tabItem { Label("カメラ名", systemImage: "camera") }
            TableEditor(title: "EXIFのレンズ名 → 表示名\n(未登録ならEXIFの名前をそのまま使います)",
                        keyLabel: "EXIFのレンズ名", valueLabel: "表示名", table: model.lenses)
                .tabItem { Label("レンズ名", systemImage: "camera.aperture") }
            FontSettings().tabItem { Label("フォント", systemImage: "textformat") }
            LayoutSettings().tabItem { Label("レイアウト", systemImage: "rectangle.split.2x1") }
            BackgroundSettings().tabItem { Label("背景", systemImage: "paintpalette") }
            SpacingSettings().tabItem { Label("行間", systemImage: "text.alignleft") }
            AboutSettings().tabItem { Label("情報", systemImage: "info.circle") }
        }
        .frame(width: 620, height: 520).padding()
    }
}

struct TableEditor: View {
    let title: String, keyLabel: String, valueLabel: String
    @ObservedObject var table: MapTable

    var body: some View {
        VStack(alignment: .leading) {
            Text(title).font(.caption).foregroundStyle(.secondary)
            List {
                ForEach($table.entries) { $e in
                    HStack {
                        TextField(keyLabel, text: $e.key)
                        Image(systemName: "arrow.right").foregroundStyle(.secondary)
                        TextField(valueLabel, text: $e.value)
                        Button {
                            table.entries.removeAll { $0.id == e.id }
                        } label: { Image(systemName: "minus.circle.fill").foregroundStyle(.red) }
                            .buttonStyle(.borderless)
                    }
                }
                .onDelete { table.entries.remove(atOffsets: $0) }
            }
            HStack {
                Button("追加") { table.entries.insert(MapEntry(key: "", value: ""), at: 0) }
                Button("初期値に戻す") { table.reset() }
                Spacer()
                Text("変更は次に読み込む写真から反映されます").font(.caption).foregroundStyle(.secondary)
            }
        }.padding()
    }
}

/// 入力欄 + 横の候補メニュー + 入力中に下へ出る候補リスト(入力文字で絞り込み)
struct SuggestField: View {
    let label: String
    @Binding var text: String
    let candidates: [String]
    /// レンズ名など、改行(2段表示)を入れられる欄
    var allowsBreak = false
    /// 入力が終わった(フォーカスが外れた)とき
    var onCommit: (String) -> Void = { _ in }
    @FocusState private var focused: Bool
    @State private var open = false

    private var matches: [String] {
        let q = text.trimmingCharacters(in: .whitespaces).lowercased()
        guard !q.isEmpty else { return [] }
        return candidates.filter { $0.lowercased().contains(q) && $0 != text }
    }

    var body: some View {
        HStack(spacing: 2) {
            TextField(label, text: $text, axis: .vertical)
                .lineLimit(1...2)
                .textFieldStyle(.roundedBorder).font(.caption)
                .focused($focused)
            Menu {
                if candidates.isEmpty { Text("候補なし") }
                ForEach(candidates, id: \.self) { c in Button(c) { text = c } }
            } label: { Image(systemName: "chevron.down.circle") }
                .menuStyle(.borderlessButton).menuIndicator(.hidden).fixedSize()
            if allowsBreak {
                Button { text = text.contains("\n") ? text.replacingOccurrences(of: "\n", with: " ") : text + "\n" } label: { Image(systemName: "return") }
                    .buttonStyle(.borderless).help("2段にする(1行目は左寄せ・2行目は右寄せ)/ 解除")
            }
        }
        // 候補はポップオーバーで欄の下に出す(レイアウトに影響せず、Listの行にも切られない)
        .popover(isPresented: Binding(get: { open && !matches.isEmpty }, set: { open = $0 }),
                 attachmentAnchor: .rect(.bounds), arrowEdge: .bottom) {
            VStack(alignment: .leading, spacing: 0) {
                ForEach(matches.prefix(6), id: \.self) { c in
                    Button { text = c; open = false } label: {
                        Text(c).font(.caption).frame(maxWidth: .infinity, alignment: .leading)
                            .padding(.horizontal, 8).padding(.vertical, 4).contentShape(Rectangle())
                    }.buttonStyle(.plain)
                }
            }
            .frame(minWidth: 220).padding(4)
        }
        .onChange(of: focused) { f in
            if f { open = true } else {
                onCommit(text)
                // クリックで候補を選べるよう、フォーカスが外れてから少し待って閉じる
                DispatchQueue.main.asyncAfter(deadline: .now() + 0.2) { if !focused { open = false } }
            }
        }
        .onChange(of: text) { _ in if focused { open = true } }
    }
}

/// スライドに使うフォントの初期値(欧文・和文)
struct FontSettings: View {
    @EnvironmentObject var model: AppModel
    private let families = NSFontManager.shared.availableFontFamilies.sorted()

    /// 一覧にない名前(テンプレート既定のMS Pゴシックなど)も選択肢に残す
    private func options(_ current: String) -> [String] {
        families.contains(current) ? families : [current] + families
    }

    var body: some View {
        Form {
            Picker("欧文フォント", selection: $model.fonts.latin) {
                ForEach(options(model.fonts.latin), id: \.self) { Text($0).tag($0) }
            }
            Picker("和文フォント", selection: $model.fonts.ea) {
                ForEach(options(model.fonts.ea), id: \.self) { Text($0).tag($0) }
            }
            Text("Shutter Speed: 1/800 sec   α1 II / ILCE-1M2   「作品タイトル」")
                .font(.custom(model.fonts.latin, size: 14)).padding(.vertical, 6)
            HStack {
                Button("初期値(Arial)に戻す") { model.fonts = .default }
                Spacer()
            }
            Text("pptxを開く環境に、選んだフォントが入っていないと別のフォントで表示されます。配布や提出には、Calibri や游ゴシックなど標準的なものがおすすめです。")
                .font(.caption).foregroundStyle(.secondary)
        }
        .padding()
        .frame(maxHeight: .infinity, alignment: .top)
    }
}

/// スライドの背景色。文字色は背景の明るさから自動で白/黒になる
struct BackgroundSettings: View {
    @EnvironmentObject var model: AppModel

    private var color: Binding<Color> {
        Binding(
            get: { Color(hex: model.bgHex) },
            set: { model.bgHex = $0.hex }
        )
    }

    var body: some View {
        Form {
            HStack {
                Button("黒") { model.bgHex = SlideStyle.black.bgHex }
                Button("ライトグレー") { model.bgHex = SlideStyle.lightGray.bgHex }
                ColorPicker("カスタム", selection: color, supportsOpacity: false)
                Spacer()
                Text("#\(model.bgHex)").monospaced().foregroundStyle(.secondary)
            }
            // プレビュー(実際のスライドの文字色)
            VStack(spacing: 4) {
                Text("5226C019").font(.system(size: 17))
                Text("α1 II / ILCE-1M2").font(.system(size: 13))
                Text("Shutter Speed: 1/800 sec").font(.system(size: 12))
            }
            .foregroundStyle(Color(hex: model.style.textHex))
            .frame(maxWidth: .infinity, minHeight: 130)
            .background(Color(hex: model.bgHex))
            .overlay(Rectangle().stroke(Color.secondary.opacity(0.4)))
            Text("カラーパネルの「スライダー」タブでRGB・CMYKの数値を指定でき、「カラーパレット」などから選ぶこともできます。文字色は背景が明るいと黒、暗いと白に自動で切り替わります。")
                .font(.caption).foregroundStyle(.secondary)
        }
        .padding()
        .frame(maxHeight: .infinity, alignment: .top)
    }
}

struct GeneralSettings: View {
    @EnvironmentObject var model: AppModel
    var body: some View {
        Form {
            Toggle("書き出したらpptxを自動で開く", isOn: $model.openAfterExport)
            Text("オフにすると、書き出したファイルをFinderで表示します。")
                .font(.caption).foregroundStyle(.secondary)
        }
        .padding()
        .frame(maxHeight: .infinity, alignment: .top)
    }
}

extension Color {
    init(hex: String) {
        let v = Int(hex, radix: 16) ?? 0
        self.init(.sRGB, red: Double((v >> 16) & 255) / 255, green: Double((v >> 8) & 255) / 255, blue: Double(v & 255) / 255)
    }
    /// sRGBに変換したRRGGBB
    var hex: String {
        let c = NSColor(self).usingColorSpace(.sRGB) ?? .black
        return String(format: "%02X%02X%02X", Int((c.redComponent * 255).rounded()), Int((c.greenComponent * 255).rounded()), Int((c.blueComponent * 255).rounded()))
    }
}

/// 撮影情報ボックス(Shutter Speed / Aperture / ISO)の行間。デモテキストで見ながら調整する
struct SpacingSettings: View {
    @EnvironmentObject var model: AppModel
    private let demo: [(String, Double)] = [
        ("Shutter Speed: 1/800 sec", 7.5), ("Aperture: f/2.8", 7.5), ("ISO: 400", 7.5), ("Step Crop", 7.5),
    ]

    var body: some View {
        Form {
            HStack {
                Slider(value: $model.lineSpacing, in: 0.8...2.0, step: 0.05)
                Text("\(Int((model.lineSpacing * 100).rounded()))%").monospacedDigit().frame(width: 48, alignment: .trailing)
            }
            // スライドの撮影情報ボックスと同じ見た目(黒背景・白文字・左寄せ)。1pt=1.6倍に拡大して表示
            VStack(alignment: .leading, spacing: 0) {
                ForEach(demo, id: \.0) { t, size in
                    Text(t).font(.custom(model.fonts.latin, size: size * 1.6))
                        .frame(minHeight: size * 1.6 * 1.2 * model.lineSpacing, alignment: .leading)
                }
            }
            .foregroundStyle(.white).padding(8).frame(width: 220, alignment: .leading).background(.black)
            HStack {
                Button("100%に戻す") { model.lineSpacing = 1.0 }
                Spacer()
            }
            Text("線の下の撮影情報(Shutter Speed / Aperture / ISO / 備考)を1つのテキストボックスにまとめ、その中の行間を指定します。")
                .font(.caption).foregroundStyle(.secondary)
        }
        .padding()
        .frame(maxHeight: .infinity, alignment: .top)
    }
}

/// アプリ情報(バージョン・アップデート・リンク)
struct AboutSettings: View {
    @EnvironmentObject var model: AppModel
    @Environment(\.openWindow) private var openWindow

    var body: some View {
        VStack(spacing: 10) {
            Image(nsImage: NSApp.applicationIconImage).resizable().frame(width: 72, height: 72)
            Text("写真フォーラムPPTX").font(.title2).bold()
            Text("バージョン \(Updater.currentVersion)").foregroundStyle(.secondary)
            if let u = model.update {
                HStack {
                    Text("新しい版 v\(u.version) があります")
                    if model.updating { ProgressView().controlSize(.small) }
                    else { Button("今すぐ更新") { Task { await model.installUpdate() } }.buttonStyle(.borderedProminent) }
                }
            } else {
                HStack {
                    Button("アップデートを確認") { Task { await model.checkForUpdate(manual: true) } }
                    if !model.status.isEmpty { Text(model.status).font(.caption).foregroundStyle(.secondary) }
                }
            }
            Divider().padding(.vertical, 6)
            HStack {
                Button("新機能(更新履歴)") { openWindow(id: "whatsnew") }
                Button("使い方") { openWindow(id: "help") }
            }
            HStack {
                Button("不具合・要望を報告") { NSWorkspace.shared.open(Links.newIssue) }
                Button("GitHub リポジトリ") { NSWorkspace.shared.open(Links.repo) }
            }
        }
        .padding()
        .frame(maxWidth: .infinity, maxHeight: .infinity, alignment: .top)
    }
}

/// Help → 新機能 から開く(全履歴)
struct WhatsNewWindow: View {
    @Environment(\.dismiss) private var dismiss
    var body: some View { WhatsNewView(notes: ReleaseNotes.all) { dismiss() } }
}
