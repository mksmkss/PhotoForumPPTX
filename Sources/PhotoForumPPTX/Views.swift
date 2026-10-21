import SwiftUI

@main
struct PhotoForumApp: App {
    @StateObject private var model = AppModel()

    var body: some Scene {
        WindowGroup {
            ContentView().environmentObject(model).frame(minWidth: 820, minHeight: 560)
        }
        Settings {
            SettingsView().environmentObject(model)
        }
    }
}

struct ContentView: View {
    @EnvironmentObject var model: AppModel
    @State private var targeted = false

    var body: some View {
        HStack(spacing: 0) {
            photoList
            Divider()
            sidebar.frame(width: 260)
        }
        .overlay(alignment: .bottom) {
            if !model.status.isEmpty {
                Text(model.status).padding(8).background(.regularMaterial, in: Capsule()).padding(.bottom, 8)
            }
        }
        .onDrop(of: [.fileURL], isTargeted: $targeted) { providers in
            for p in providers {
                _ = p.loadObject(ofClass: URL.self) { url, _ in
                    if let url { Task { @MainActor in model.add(urls: [url]) } }
                }
            }
            return true
        }
        .overlay { if targeted { RoundedRectangle(cornerRadius: 8).stroke(Color.accentColor, lineWidth: 3).padding(4) } }
    }

    var photoList: some View {
        VStack(spacing: 0) {
            HStack {
                Button("写真を選択…") { model.openPanel() }
                Text("(ドラッグ&ドロップも可)").foregroundStyle(.secondary).font(.caption)
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
            Text("学籍番号・氏名・タイトルは上から順に配置。カメラ・レンズ・露出情報はテンプレート通り下部(赤線の上下)に固定です。")
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
                Grid(alignment: .leading, horizontalSpacing: 6, verticalSpacing: 3) {
                    GridRow { field("カメラ", $photo.camera); field("レンズ", $photo.lens) }
                    GridRow {
                        HStack { field("焦点距離(mm)", $photo.focal); field("SS", $photo.shutter) }
                        HStack { field("F", $photo.aperture); field("ISO", $photo.iso) }
                    }
                }
            }
            Button(role: .destructive, action: onRemove) { Image(systemName: "xmark.circle") }.buttonStyle(.borderless)
        }.padding(.vertical, 4)
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
