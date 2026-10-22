import SwiftUI
import AppKit

enum Links {
    static var version: String { Bundle.main.object(forInfoDictionaryKey: "CFBundleShortVersionString") as? String ?? "?" }
    static let repo = URL(string: "https://github.com/\(ReleaseCheck.repo)")!
    static let issues = URL(string: "https://github.com/\(ReleaseCheck.repo)/issues")!

    /// バージョンとmacOSを入れた状態でIssue作成ページを開く
    static var newIssue: URL {
        let os = ProcessInfo.processInfo.operatingSystemVersionString
        let body = """
        ## 内容
        (何をしたら、どうなったか / どうなってほしいか)

        ## 環境
        - アプリ: v\(version)
        - macOS: \(os)
        """
        var c = URLComponents(string: "https://github.com/\(ReleaseCheck.repo)/issues/new")!
        c.queryItems = [URLQueryItem(name: "body", value: body)]
        return c.url!
    }
}

/// メニューバーの Help を差し替える
struct HelpCommands: Commands {
    @Environment(\.openWindow) private var openWindow

    var body: some Commands {
        CommandGroup(replacing: .help) {
            Button("使い方") { openWindow(id: "help") }
            Divider()
            Button("不具合・要望を報告 (GitHub Issue)…") { NSWorkspace.shared.open(Links.newIssue) }
            Button("GitHub リポジトリを開く") { NSWorkspace.shared.open(Links.repo) }
        }
    }
}

struct HelpView: View {
    var body: some View {
        ScrollView {
            VStack(alignment: .leading, spacing: 14) {
                Text("写真フォーラムPPTX  v\(Updater.currentVersion)").font(.title2).bold()
                Text("写真を選ぶと、EXIFからカメラ名・レンズ名・焦点距離・SS・F値・ISOを読み取り、写真投稿フォーラムのテンプレートの書式でpptxを作ります。複数枚選ぶと複数ページになります。")

                section("基本の使い方", [
                    "「写真を選択…」またはドラッグ&ドロップで写真を追加します(ページの並びはドラッグで変更)。",
                    "右のリストで、スライドに入れる項目をチェックします。",
                    "EXIFが空の項目(マニュアルレンズなど)は、写真ごとの欄に手入力できます。入力中に候補が出ます。",
                    "「書き出し (.pptx)」で保存します(保存先の初期値はダウンロード)。",
                ])
                section("スライドの配置", [
                    "学籍番号・氏名・タイトル: 右カラムの上から順に並びます。",
                    "カメラ名・レンズ名は赤線の上、SS・F値・ISO・備考は赤線の下(左寄せ)。",
                    "焦点距離は右カラムの中央に大きく表示します。",
                    "デジタルズーム(Step Crop)で撮った写真は、焦点距離にズーム比を掛けた値を入れ、備考に「*Step Crop: ○mm equiv.」を用意します(備考をオンにしたときに表示)。",
                ])
                section("対応表(カメラ名・レンズ名の表示名)", [
                    "メニュー「写真フォーラムPPTX → Settings…」(⌘,)で編集できます。",
                    "例: EXIFの型番 ILCE-1M2 → 表示名 α1 II(スライドには「α1 II / ILCE-1M2」と出ます)。",
                    "変更は、次に読み込む写真から反映されます。",
                ])
                section("アップデート", [
                    "新しい版が出ると、起動時に上部にバナーが出ます。「今すぐ更新」を押すだけで自動で更新・再起動します。",
                    "メニュー「写真フォーラムPPTX → アップデートを確認…」でも確認できます。",
                ])

                Divider()
                Text("困ったとき・要望があるとき").font(.headline)
                Text("不具合の報告や「こうなっていたら嬉しい」という要望は、GitHub の Issue で気軽に立ててください(GitHubアカウントが必要です)。下のボタンから開くと、アプリのバージョンとmacOSの情報が入った状態で書き始められます。")
                HStack {
                    Button("Issue を立てる") { NSWorkspace.shared.open(Links.newIssue) }.buttonStyle(.borderedProminent)
                    Button("既存のIssueを見る") { NSWorkspace.shared.open(Links.issues) }
                    Button("リポジトリを開く") { NSWorkspace.shared.open(Links.repo) }
                }
                Text("起動時に「開発元を検証できません」と出る場合は、READMEのインストール手順(1行コマンド)をお試しください。")
                    .font(.caption).foregroundStyle(.secondary)
            }
            .padding(20)
            .frame(maxWidth: .infinity, alignment: .leading)
        }
        .frame(minWidth: 520, minHeight: 520)
    }

    func section(_ title: String, _ items: [String]) -> some View {
        VStack(alignment: .leading, spacing: 4) {
            Text(title).font(.headline)
            ForEach(items, id: \.self) { Text("・" + $0).fixedSize(horizontal: false, vertical: true) }
        }
    }
}
