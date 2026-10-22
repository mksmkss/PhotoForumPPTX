import AppKit

/// 新しい PhotoForumPPTX.zip を渡すだけで、自分自身を置き換えて再起動する。
/// (公証なしのアプリなので、置き換え後に隔離属性も外す)
@MainActor
enum Updater {
    static var currentVersion: String {
        Bundle.main.object(forInfoDictionaryKey: "CFBundleShortVersionString") as? String ?? "?"
    }

    static func chooseZip() {
        let panel = NSOpenPanel()
        panel.message = "新しい PhotoForumPPTX.zip を選んでください"
        panel.allowedContentTypes = [.zip]
        panel.directoryURL = FileManager.default.urls(for: .downloadsDirectory, in: .userDomainMask).first
        if panel.runModal() == .OK, let url = panel.url { install(zip: url) }
    }

    static func install(zip: URL, confirm: Bool = true) {
        let fm = FileManager.default
        let work = fm.temporaryDirectory.appendingPathComponent("pfp-update-\(UUID().uuidString)")
        do {
            try fm.createDirectory(at: work, withIntermediateDirectories: true)
            try PPTXBuilder.run("/usr/bin/unzip", ["-q", zip.path, "-d", work.path])
            guard let newApp = findApp(in: work) else { throw PPTXError.tool("zipの中にPhotoForumPPTX.appが見つかりません") }
            let info = NSDictionary(contentsOf: newApp.appendingPathComponent("Contents/Info.plist"))
            let newVersion = info?["CFBundleShortVersionString"] as? String ?? "?"

            let dest = Bundle.main.bundleURL
            guard fm.isWritableFile(atPath: dest.deletingLastPathComponent().path) else {
                throw PPTXError.tool("\(dest.deletingLastPathComponent().path) に書き込めません。READMEの1行コマンドで更新してください")
            }

            if confirm {
                let alert = NSAlert()
                alert.messageText = "v\(currentVersion) → v\(newVersion) に更新しますか?"
                alert.informativeText = "アプリを終了して入れ替え、自動で再起動します。"
                alert.addButton(withTitle: "更新して再起動")
                alert.addButton(withTitle: "キャンセル")
                guard alert.runModal() == .alertFirstButtonReturn else { try? fm.removeItem(at: work); return }
            }

            // 自分の終了を待ってから入れ替える(終了後も動くよう別プロセスで)
            let script = work.appendingPathComponent("update.sh")
            let body = """
            #!/bin/sh
            while kill -0 \(ProcessInfo.processInfo.processIdentifier) 2>/dev/null; do sleep 0.2; done
            rm -rf "\(dest.path)"
            /usr/bin/ditto "\(newApp.path)" "\(dest.path)"
            /usr/bin/xattr -dr com.apple.quarantine "\(dest.path)"
            /usr/bin/open "\(dest.path)"
            rm -rf "\(work.path)"
            """
            try body.write(to: script, atomically: true, encoding: .utf8)
            let p = Process()
            p.executableURL = URL(fileURLWithPath: "/bin/sh")
            p.arguments = ["-c", "nohup /bin/sh '\(script.path)' >/dev/null 2>&1 &"]
            try p.run()
            NSApp.terminate(nil)
        } catch {
            try? fm.removeItem(at: work)
            let a = NSAlert()
            a.messageText = "更新に失敗しました"
            a.informativeText = error.localizedDescription
            a.runModal()
        }
    }

    private static func findApp(in dir: URL) -> URL? {
        guard let e = FileManager.default.enumerator(at: dir, includingPropertiesForKeys: nil) else { return nil }
        for case let u as URL in e where u.pathExtension == "app" && u.lastPathComponent == "PhotoForumPPTX.app" { return u }
        return nil
    }
}
