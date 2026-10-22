import Foundation

/// GitHub Releases の最新版を問い合わせる(認証不要)
enum ReleaseCheck {
    static let repo = "mksmkss/PhotoForumPPTX"
    static let assetName = "PhotoForumPPTX.zip"

    struct Found { let version: String; let zipURL: URL }

    private struct Release: Decodable {
        struct Asset: Decodable { let name: String; let browser_download_url: String }
        let tag_name: String
        let assets: [Asset]
    }

    /// 現在より新しい版があれば返す。なければnil
    static func newer(than current: String) async throws -> Found? {
        var req = URLRequest(url: URL(string: "https://api.github.com/repos/\(repo)/releases/latest")!)
        req.setValue("application/vnd.github+json", forHTTPHeaderField: "Accept")
        req.timeoutInterval = 8
        let (data, _) = try await URLSession.shared.data(for: req)
        let r = try JSONDecoder().decode(Release.self, from: data)
        let v = r.tag_name.trimmingCharacters(in: CharacterSet(charactersIn: "vV"))
        guard isNewer(v, than: current),
              let a = r.assets.first(where: { $0.name == assetName }),
              let url = URL(string: a.browser_download_url) else { return nil }
        return Found(version: v, zipURL: url)
    }

    static func isNewer(_ a: String, than b: String) -> Bool {
        let x = a.split(separator: ".").map { Int($0) ?? 0 }, y = b.split(separator: ".").map { Int($0) ?? 0 }
        for i in 0..<max(x.count, y.count) {
            let p = i < x.count ? x[i] : 0, q = i < y.count ? y[i] : 0
            if p != q { return p > q }
        }
        return false
    }

    static func download(_ url: URL) async throws -> URL {
        let (tmp, _) = try await URLSession.shared.download(from: url)
        let dest = FileManager.default.temporaryDirectory.appendingPathComponent("PhotoForumPPTX-\(UUID().uuidString).zip")
        try FileManager.default.moveItem(at: tmp, to: dest)
        return dest
    }
}
