import Foundation

struct MapEntry: Codable, Identifiable, Equatable {
    var id = UUID()
    var key: String
    var value: String
}

/// EXIF値 → 表示名 の対応表。ユーザーが設定画面で編集でき、Application Supportに保存される。
final class MapTable: ObservableObject {
    @Published var entries: [MapEntry] { didSet { save() } }
    private let fileName: String
    private let defaults: [MapEntry]

    init(fileName: String, defaults: [MapEntry], version: Int) {
        self.fileName = fileName
        self.defaults = defaults
        if let data = try? Data(contentsOf: MapTable.dir.appendingPathComponent(fileName)),
           let e = try? JSONDecoder().decode([MapEntry].self, from: data) {
            entries = e
        } else {
            entries = defaults
        }
        // アプリ更新で既定値が増えたとき、未登録のキーだけ既存の表に足す(消した項目は version が同じ間は復活しない)
        let vKey = "defaultsVersion.\(fileName)"
        if UserDefaults.standard.integer(forKey: vKey) < version {
            for d in defaults where !entries.contains(where: { $0.key.lowercased() == d.key.lowercased() }) {
                entries.append(d)
            }
            UserDefaults.standard.set(version, forKey: vKey)
            save()
        }
    }

    static var dir: URL {
        let d = FileManager.default.urls(for: .applicationSupportDirectory, in: .userDomainMask)[0]
            .appendingPathComponent("PhotoForumPPTX")
        try? FileManager.default.createDirectory(at: d, withIntermediateDirectories: true)
        return d
    }

    private func save() {
        if let data = try? JSONEncoder().encode(entries) {
            try? data.write(to: MapTable.dir.appendingPathComponent(fileName))
        }
    }

    func reset() { entries = defaults }

    /// 大文字小文字・前後空白を無視して引く。見つからなければnil
    func lookup(_ key: String) -> String? {
        let k = key.trimmingCharacters(in: .whitespaces).lowercased()
        guard !k.isEmpty else { return nil }
        return entries.first { $0.key.trimmingCharacters(in: .whitespaces).lowercased() == k }?.value
    }
}

enum DefaultTables {
    static let cameras: [MapEntry] = [
        ("ILCE-1", "α1"), ("ILCE-1M2", "α1 II"),
        ("ILCE-9", "α9"), ("ILCE-9M2", "α9 II"), ("ILCE-9M3", "α9 III"),
        ("ILCE-7", "α7"), ("ILCE-7M2", "α7 II"), ("ILCE-7M3", "α7 III"), ("ILCE-7M4", "α7 IV"), ("ILCE-7M5", "α7 V"),
        ("ILCE-7R", "α7R"), ("ILCE-7RM2", "α7R II"), ("ILCE-7RM3", "α7R III"), ("ILCE-7RM3A", "α7R IIIA"),
        ("ILCE-7RM4", "α7R IV"), ("ILCE-7RM4A", "α7R IVA"), ("ILCE-7RM5", "α7R V"),
        ("ILCE-7S", "α7S"), ("ILCE-7SM2", "α7S II"), ("ILCE-7SM3", "α7S III"),
        ("ILCE-7C", "α7C"), ("ILCE-7CM2", "α7C II"), ("ILCE-7CR", "α7CR"),
        ("ILCE-6000", "α6000"), ("ILCE-6100", "α6100"), ("ILCE-6300", "α6300"), ("ILCE-6400", "α6400"),
        ("ILCE-6500", "α6500"), ("ILCE-6600", "α6600"), ("ILCE-6700", "α6700"),
        ("ILCE-5100", "α5100"), ("ILCE-3500", "α3500"),
        ("ZV-E10", "ZV-E10"), ("ZV-E10M2", "ZV-E10 II"), ("ZV-E1", "ZV-E1"), ("ZV-1", "ZV-1"), ("ZV-1M2", "ZV-1 II"),
        ("ILME-FX3", "FX3"), ("ILME-FX30", "FX30"),
        ("DSC-RX1R", "RX1R"), ("DSC-RX1RM2", "RX1R II"), ("DSC-RX1RM3", "RX1R III"),
        ("DSC-RX100", "RX100"), ("DSC-RX100M2", "RX100 II"), ("DSC-RX100M3", "RX100 III"),
        ("DSC-RX100M4", "RX100 IV"), ("DSC-RX100M5", "RX100 V"), ("DSC-RX100M5A", "RX100 VA"),
        ("DSC-RX100M6", "RX100 VI"), ("DSC-RX100M7", "RX100 VII"),
        ("DSC-RX10", "RX10"), ("DSC-RX10M2", "RX10 II"), ("DSC-RX10M3", "RX10 III"), ("DSC-RX10M4", "RX10 IV"),
        ("DSC-RX1", "RX1"), ("DSC-RX0", "RX0"), ("DSC-RX0M2", "RX0 II"),
        ("ILME-FX6", "FX6"), ("ILME-FX2", "FX2"), ("ILCE-6700", "α6700"), ("ILCE-7M3K", "α7 III"),
        ("ILCE-QX1", "QX1"), ("DSC-HX99", "HX99"),
    ].map { MapEntry(key: $0.0, value: $0.1) }

    /// レンズはEXIFのLensModelをそのまま使うのが基本。名前を直したいものだけここに足す
    static let lenses: [MapEntry] = [
        ("35mm F2.0", "Sonnar T* 35mm f/2.0"),     // RX1R III
    ].map { MapEntry(key: $0.0, value: $0.1) }

    /// 既定値を増やしたら上げる(既存ユーザーの表にも新しい既定値が入る)
    static let version = 3
}
