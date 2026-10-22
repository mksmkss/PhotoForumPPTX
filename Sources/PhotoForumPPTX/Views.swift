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
        Settings {
            SettingsView().environmentObject(model)
        }
    }
}

struct ContentView: View {
    @EnvironmentObject var model: AppModel
    @State private var targeted = false

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
                Text("写真を選ぶと、EXIFを読み取って1枚1ページで並べます").foregroundStyle(.secondary)
                    .frame(maxWidth: .infinity, maxHeight: .infinity)
            } else {
                List {
                    ForEach(model.photos) { p in
                        PhotoRow(photo: p, onRemove: { model.photos.removeAll { $0.id == p.id } })
                    }
                    .onMove { model.photos.move(fromOffsets: $0, toOffset: $1) }
                }
            }
        }
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
    var onRemove: () -> Void

    var body: some View {
        HStack(alignment: .top, spacing: 12) {
            Group {
                if let t = photo.thumbnail { Image(nsImage: t).resizable().scaledToFit() } else { Color.gray }
            }.frame(width: 110, height: 80).background(Color.black)
            VStack(alignment: .leading, spacing: 4) {
                Text(photo.url.lastPathComponent).font(.caption).foregroundStyle(.secondary)
                if model.fields.first(where: { $0.kind == .title })?.enabled == true {
                    TextField("作品タイトル", text: $photo.title)
                }
                if model.fields.first(where: { $0.kind == .note })?.enabled == true {
                    TextField("備考 (例: *Step Crop: 70mm equiv.)", text: $photo.note)
                }
                Grid(alignment: .leading, horizontalSpacing: 6, verticalSpacing: 3) {
                    GridRow { suggestField("カメラ", $photo.camera, cameraCandidates); suggestField("レンズ", $photo.lens, lensCandidates) }
                    GridRow {
                        HStack { field("焦点距離(mm)", $photo.focal); field("SS", $photo.shutter) }
                        HStack { field("F", $photo.aperture); field("ISO", $photo.iso) }
                    }
                }
            }
            Button(role: .destructive, action: onRemove) { Image(systemName: "xmark.circle") }.buttonStyle(.borderless)
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
        for c in model.lenses.entries.map(\.value) + model.photos.map(\.lens) where !c.isEmpty && !out.contains(c) { out.append(c) }
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
            TableEditor(title: "EXIFのカメラ型番 → 表示名\n(例: ILCE-1M2 → α1 II。スライドには「α1 II / ILCE-1M2」と出ます)",
                        keyLabel: "EXIFの型番", valueLabel: "表示名", table: model.cameras)
                .tabItem { Text("カメラ名") }
            TableEditor(title: "EXIFのレンズ名 → 表示名\n(未登録ならEXIFの名前をそのまま使います)",
                        keyLabel: "EXIFのレンズ名", valueLabel: "表示名", table: model.lenses)
                .tabItem { Text("レンズ名") }
            FontSettings().tabItem { Text("フォント") }
        }
        .frame(width: 560, height: 460).padding()
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
    @FocusState private var focused: Bool
    @State private var open = false

    private var matches: [String] {
        let q = text.trimmingCharacters(in: .whitespaces).lowercased()
        guard !q.isEmpty else { return [] }
        return candidates.filter { $0.lowercased().contains(q) && $0 != text }
    }

    var body: some View {
        HStack(spacing: 2) {
            TextField(label, text: $text)
                .textFieldStyle(.roundedBorder).font(.caption)
                .focused($focused)
            Menu {
                if candidates.isEmpty { Text("候補なし") }
                ForEach(candidates, id: \.self) { c in Button(c) { text = c } }
            } label: { Image(systemName: "chevron.down.circle") }
                .menuStyle(.borderlessButton).menuIndicator(.hidden).fixedSize()
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
                Button("テンプレートの既定に戻す") { model.fonts = .template }
                Spacer()
            }
            Text("pptxを開く環境に、選んだフォントが入っていないと別のフォントで表示されます。配布や提出には、Calibri や游ゴシックなど標準的なものがおすすめです。")
                .font(.caption).foregroundStyle(.secondary)
        }
        .padding()
        .frame(maxHeight: .infinity, alignment: .top)
    }
}
