# Windows版 実装まとめ

対象: `windows-core` ブランチ。Mac版(`Sources/`, `Resources/`, Swift/AppKit/SwiftUI)はこの作業では変更していない。

対応範囲はMac版v1.6相当と、書き出し後の自動オープンまで。v2.0の2段組み・背景色・レイアウト選択・レンズ記憶は未対応で、後続の作業とする。

## 背景・方針

Mac版は SwiftUI / AppKit / ImageIO という macOS専用フレームワークに依存しており、そのままではWindowsで動かせない。
「核心機能(EXIF読み取り → 対応表変換 → テンプレートpptx生成)を先に動く形にし、GUIや補助機能は後で積み上げる」という方針で、
核心機能 → GUIメイン画面 → 設定画面 → ヘルプ/新機能ポップアップ → 自動更新 → 配布、の順に実装した。

## 技術選定

| 項目 | 選定 | 理由 |
|---|---|---|
| 言語・ランタイム | C# / .NET 8 (`net8.0-windows`) | Windows専用アプリとして割り切り、WPFでGUIを組める。`dotnet publish`で自己完結exeも作れ、配布体験がMac版のzip配布に近い |
| GUIフレームワーク | WPF + [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet) | MVVMパターンでViewとロジックを分離し、WPFを参照しないViewModel層をユニットテスト可能にする(後述「テスト方針」) |
| EXIF読み取り | [MetadataExtractor](https://github.com/drewnoakes/metadata-extractor-dotnet) (NuGet) | 純C#実装でJPEG/TIFFのExif IFD0・Exif SubIFDを型安全に読める。macOSの`ImageIO`相当 |
| 画像デコード・再エンコード | `System.Drawing.Common` (GDI+, Windows専用) | Windows専用に限定したことでライセンス上の制約なく使える。`RotateFlip`でEXIF Orientationの焼き込みができ、Mac版の`CGImageSourceCreateThumbnailWithTransform`相当の処理になる |
| pptx組み立て | `System.IO.Compression.ZipFile` + 文字列/正規表現によるXML編集 | Mac版が`/usr/bin/zip`・`/usr/bin/unzip`をシェル実行していた部分を.NET標準APIに置き換え、外部コマンド依存を排除(Mac版より依存が減り、むしろ改善) |
| 設定・対応表の永続化 | `System.Text.Json` + `%AppData%\PhotoForumPPTX\*.json`(自作、アトミック書き込み) | レジストリ/app.config/Velopack等のインストーラー型配布/ClickOnceと比較検討し、依存追加なし・デバッグしやすい・MapTableと一貫した方式として採用。詳細は後述「設定の永続化」 |
| 自動更新 | 自作(別プロセスのPowerShellスクリプトに入れ替えを委譲) | MSIX/ClickOnce/Velopack/winget配布と比較検討し、インストーラー不要・学生がコマンドを打たなくて済む「案A」を採用。詳細は後述「自動更新」 |

.NET SDKは当初未インストールだったため、`winget install Microsoft.DotNet.SDK.8`でセットアップ済み(.NET 8.0.427)。

## ディレクトリ構成

```
windows/
  PhotoForumPPTX.sln
  README.md              配布・使い方・ビルド・リリース手順
  IMPLEMENTATION.md       このファイル
  build.ps1               発行してdist/PhotoForumPPTX-win.zipを作る
  install.ps1              zipを直接受け取った人向けのオフラインインストーラー
  src/
    PhotoForumPPTX.Core/  コアロジック(class library、WPF非依存)
    PhotoForumPPTX.Gui/   ViewModel層(MVVM、WPF非依存でテスト可能)
    PhotoForumPPTX.App/   WPFアプリ本体(View・ダイアログ・実際の更新処理)
    PhotoForumPPTX.Cli/   動作確認用コンソールアプリ
  tests/
    PhotoForumPPTX.Core.Tests/  xUnitテスト(47件)
    PhotoForumPPTX.Gui.Tests/   xUnitテスト(36件)
```

## Mac版 → Windows版 の対応表

| Mac (Swift) | Windows (C#) | 内容 |
|---|---|---|
| `Model.swift` の `FieldKind` | `Core/FieldKind.cs` | フィールド種別・ラベル・フォントサイズ・グループ(レイアウト上の配置グループ) |
| `Model.swift` の `Photo` (EXIF抽出部分) | `Core/PhotoInfo.cs` | EXIFからカメラ・レンズ・焦点距離・シャッター・絞り・ISOを抽出 |
| `Tables.swift` の `MapTable` / `DefaultTables` | `Core/MapTable.cs` / `DefaultTables.cs` | カメラ型番・レンズ名 → 表示名の対応表。`%AppData%\PhotoForumPPTX\cameras.json` / `lenses.json` |
| `PPTXBuilder.swift` | `Core/PptxBuilder.cs` | テンプレートpptxの展開 → スライドXML生成 → zip再構築。EMU単位のレイアウト計算を数値レベルで1:1移植 |
| `Model.swift` の `AppModel`(設定の永続化・写真一覧・書き出し) | `Gui/MainViewModel.cs` + `Core/AppSettings.cs`/`AppSettingsStore.cs` | 写真一覧・項目設定・学籍番号氏名・書き出しはMainViewModel、永続化はAppSettingsStoreに分離 |
| `Views.swift` の `ContentView` / `PhotoRow` / `SuggestField` | `App/MainWindow.xaml` | 写真選択・ドラッグ&ドロップ・一覧・候補コンボボックス・項目ON/OFFと並べ替え |
| `Views.swift` の `SettingsView` / `TableEditor` / `FontSettings` / `SpacingSettings` / `GeneralSettings` | `App/SettingsWindow.xaml` + `Gui/SettingsViewModel.cs` / `MapTableViewModel.cs` | 一般・カメラ名・レンズ名・フォント・行間・情報タブ |
| `HelpView.swift` | `App/HelpWindow.xaml` | 使い方説明・Issue報告導線 |
| `WhatsNew.swift` | `App/WhatsNewWindow.xaml` + `Gui/WhatsNewCoordinator.cs` + `Core/ReleaseNotes.cs` | 新機能ポップアップ(既存ユーザーの初回起動時のみ自動表示) |
| `ReleaseCheck.swift` | `Core/UpdateCheck.cs` + `Core/GitHubRelease.cs` + `App/GitHubReleaseFeed.cs` | GitHub Releasesの確認・バージョン比較 |
| `Updater.swift` | `Core/UpdateScriptBuilder.cs` + `App/WindowsUpdateInstaller.cs` | ダウンロード→入れ替え→再起動。Windows特有の制約があり方式が異なる(後述) |

### 挙動面での細かい留意点
- シャッタースピードの文字列化 (`ShutterText`) は Mac版の `NumberFormatter(.decimal)` によるカンマ区切り(例: `1/8,000`)をそのまま`ToString("N0")`で再現。
- EXIF Orientation (1–8) の縦横入れ替え・回転焼き込みロジックは Swift の `(5...8).contains(o)` 分岐、`RotateFlip`マッピングともにMac版の向き補正と等価になるよう対応づけた。
- デジタルズーム(RX1R IIIのStep Cropなど)による焦点距離の実効値換算・備考欄への自動補完もそのまま移植。
- フォントプレビュー(設定画面)は、WPFの`TextBlock.FontFamily`が文字列全体に一括適用されるため、pptx本体と同じ基準(文字コード0x2000以上を全角相当とみなす)で`Run`を分割し、欧文/和文フォントを個別に割り当てる`FontPreview`添付ビヘイビアを実装した。

## アーキテクチャとテスト方針

Core → Gui → App の3層構成で、GUI機能の「実装 → 単体/結合テスト → 合成データでのテスト → 実機テスト」という手順を徹底した。

- **Core**: ファイルI/Oはあるが、WPFに依存しない純粋なロジック・データモデル。`dotnet test`で直接検証できる。
- **Gui**: MVVMのViewModel層。WPFには依存しない(`ObservableObject`/`RelayCommand`は`CommunityToolkit.Mvvm`のみに依存)。ファイル選択ダイアログ・GitHub通信・実際の更新処理などOS/外部依存はすべて`IDialogService` / `IFontCatalog` / `IReleaseFeed` / `IUpdateInstaller`のインターフェース越しに呼び、テストでは偽実装(`Fake*`)を注入する。これにより**ネイティブダイアログや実際の通信を一切発生させずに画面の振る舞いをテストできる**。
- **App**: WPFのView(.xaml)とOS依存の実装(`WpfDialogService` / `WpfFontCatalog` / `GitHubReleaseFeed` / `WindowsUpdateInstaller`)。ここは自動テスト対象外(方式A)。

### 「Claudeによるテスト」と「実機テスト」の役割分担
この環境ではUI AutomationによるWPFウィンドウの自動クリック操作やウィンドウ検出が機能しないという制約が判明した(写真選択ダイアログ・設定ウィンドウ・新機能ポップアップいずれも、コード自体は正しく実行されている ─ デバッグログで実証済み ─ にもかかわらず、UI Automation越しの検出・操作だけが機能しない)。そのため:
- Claudeが担当: ViewModel層のユニットテスト、合成画像/合成データでのCore層の統合テスト(pptx生成・更新スクリプトのローカルシミュレーション等)、実行ファイルの起動確認(クラッシュの有無・UI Automationでの構造確認)
- ユーザーが担当: 実際にウィンドウを見ての操作感・見た目の確認、実際のGitHub Releasesを使った自動更新の実機テスト

## 設定の永続化

`Core/AppSettings.cs`(学籍番号・氏名・項目設定・フォント・行間・自動オープン設定・新機能ポップアップの既読バージョン)を`Core/AppSettingsStore.cs`で`%AppData%\PhotoForumPPTX\settings.json`に保存する。カメラ名・レンズ名対応表(`MapTable`)と同じ考え方。

- **アトミック書き込み**(`Core/AtomicFile.cs`): 一時ファイルへ書いてから`File.Move`で置き換え、書き込み中のクラッシュ・電源断で設定ファイルが壊れないようにした。
- **I/O障害耐性**: ディスク容量不足や権限エラーで保存に失敗してもアプリ本体はクラッシュしない(その回の保存だけ失敗する)。

## 自動更新

### 設計判断: 自作スクリプト方式(案A)を採用
Windowsは実行中のexeを直接上書きできない(Mac版は`rm -rf`で丸ごと入れ替えてもプロセスが生き続けられる)。方式を比較検討した結果:

| 方式 | 判定 | 理由 |
|---|---|---|
| **自作スクリプト(採用)** | ◎ | 追加依存なし。配布形態(zip展開するだけ)を変えずに済む |
| Velopack(旧Squirrel.Windows) | △ | 堅牢だが配布形態がインストーラー化し「zipを解凍するだけ」の手軽さが失われる |
| MSIX + App Installer | △ | 署名証明書が必須でユーザー側の信頼設定の手間が増える |
| ClickOnce | △ | 自己完結・単一ファイル発行との相性が悪い |
| winget/Chocolatey配布 | × | アプリ内の自動更新バナーが実現できず、コミュニティリポジトリへの登録・審査が要る |

### バージョン管理
Mac版は同じリポジトリで`v1.x`タグを使っているため、Windows版は**`win-v`プレフィックス**(`win-v0.1`など)で明確に区別し、アセット名は`PhotoForumPPTX-win.zip`で固定した。`Core/AppInfo.Version`を単一の真実源とし、`Core/ReleaseNotes.cs`に手でエントリを追加する(新機能ポップアップと共用)。

Mac版は`releases/latest`を参照するため、`win-v`のリリースは**必ずプレリリースとして作成する**。通常リリースにすると`win-v`がリポジトリの最新リリースになり、Mac版のインストールコマンドが404になる。Windows版は`/releases`の一覧からタグで絞り込むので、プレリリースでも取得できる。

新リポジトリに分割すれば`releases/latest/download/<ファイル名>`という静的URLがMac版同様に使え、インストール1行コマンドも更新確認ロジックも単純化できる、という検討も行ったが、新リポジトリ作成・Git履歴移行・Issue管理分離のコストに見合わないため現状維持(同一リポジトリ+`win-v`接頭辞)とした。

### 入れ替え機構
`WindowsUpdateInstaller`(App) がダウンロード→zip展開→書き込み可否チェックを行い、`UpdateScriptBuilder`(Core、パス文字列からスクリプト本文を組み立てる純粋ロジック)が生成したPowerShellスクリプトを別プロセスとして起動してからアプリを終了する。スクリプトは「自プロセスの終了を待つ(`Wait-Process`)→`robocopy /MIR`でインストール先を新しい版にミラー→再起動→作業用一時フォルダを削除」という内容。

**合成テスト**: ダミーの新旧インストールフォルダをローカルに用意し、生成したスクリプトを実機で実行して、新内容への更新・新規ファイル追加・旧ファイル削除(`/MIR`)・存在しないPIDでの`Wait-Process`が正しく動くことを確認した(実際のGitHubには接続しない)。

**実機テスト**: `win-v0.2`・`win-v0.3`のGitHub Releaseを作成し、デスクトップに展開したv0.1のビルドから実際に自動更新を実行して動作確認した。

### 実機テストで見つかった不具合と修正
1. **ダウンロードタイムアウト**: GitHub Releaseアセットへの初回アクセス時のCDN遅延により、66MBのzipダウンロードが当初の120秒タイムアウトを超えて失敗していた(2回目はCDNにキャッシュされ高速化されたため成功)。10分に延長し、全量メモリ展開からストリームコピーに変更した。
2. **エラー表示漏れ**: 更新失敗時のメッセージが設定の情報タブにしか表示されず、メイン画面のバナーには出ていなかった。バナーにも表示するよう修正した。
3. **`IncludeNativeLibrariesForSelfExtract=true`の罠**: 単一ファイル発行でこのオプションを付けると、`Resources/template.pptx`のようなコンテンツファイルが実行時に展開されない形でバンドルされ、消えてしまうことが判明。`build.ps1`ではこのオプションを使わない。

## 配布

- **オンライン1行インストール**(README記載): PowerShellに貼り付けるだけで、GitHub APIで`win-v`タグの最新リリースを検索 → ダウンロード → `%LocalAppData%\Programs\PhotoForumPPTX`に展開 → 起動(管理者権限不要)。Mac版のcurl一行インストールと同じ思想。実機で動作確認済み。
- **オフラインインストール**(`install.ps1`): zipを直接受け取った人向け。`Unblock-File`で「インターネットからのファイルです」マーク(Mark of the Web)を外してから展開・起動する(Mac版`install.sh`の隔離属性除去に相当)。
- **ビルド**(`build.ps1`): `dotnet publish`で自己完結・単一ファイル・win-x64発行し、`dist/PhotoForumPPTX-win.zip`を作る。Mac版`build.sh`と同じ構造。
- 未署名アプリのため、初回起動時にSmartScreenの警告(「Windows によって PC が保護されました」)が出る。コード署名証明書が無いため回避はできず、README/HelpViewで案内するに留める。

## 既知の制約

- **HEIC/RAW非対応**: `System.Drawing.Common`(GDI+)がデコードできる形式(JPEG/PNG/TIFF/BMP/GIFなど)のみ画像埋め込みに対応。EXIF自体は`MetadataExtractor`がHEICも読めるが、画像の再エンコードが必要なケース(向き補正が必要な非JPEG)では失敗する。
- 写真一覧・項目設定のドラッグ&ドロップ並べ替えは未実装(▲▼ボタンでの並べ替えのみ)。
- zipからのアップデート(zipのドロップ・選択)は未実装。アプリ内のオンライン自動更新のみ。
- メイン画面のカメラ/レンズ候補はWPF標準の編集可能コンボボックスで、Mac版のような入力中の部分一致フィルタリングはしていない。

## 更新履歴

| コミット | 内容 |
|---|---|
| `Windows版の核心機能(.NET 8)を追加` | EXIF読み取り・カメラ/レンズ対応表・pptx生成・CLIの土台を実装 |
| `Windows版GUIメイン画面(WPF)を追加` | MVVM構成で写真選択・一覧・項目ON/OFF・書き出しを実装 |
| `Windows版 設定画面を追加` | 一般・カメラ名・レンズ名・フォント・行間タブを実装 |
| `設定画面「フォント」「行間」タブのプレビュー不具合を修正` | 行間スライダーの刻み幅・プレビュー未反映の不具合を修正 |
| `フォントプレビューで和文フォントが反映されない不具合を修正` | `FontPreview`添付ビヘイビアで文字種ごとにフォントを分けて表示 |
| `設定・対応表の永続化を堅牢化` | アトミック書き込み・I/O障害耐性を追加 |
| `Windows版 ヘルプ・新機能ポップアップを追加` | 使い方説明・更新履歴ポップアップ・Issue報告導線を実装 |
| `自動更新 ステップ1〜4` | 更新確認ロジック・ダウンロード/入れ替え処理・バナー/情報タブ・配布zip作成スクリプトを実装 |
| `自動更新: ダウンロードタイムアウト延長とエラー表示の不具合を修正` | 実機テストで発見した2件の不具合を修正(上記参照) |
| `Windows版の配布手順を整備` | オンライン1行インストール・`install.ps1`・README整備 |
| `Windows版のリリース手順を修正し、v1.0に更新` | `win-v`のプレリリース必須化・`main`からのリリースへ変更、releaseスキルにWindows版の手順を追記、正式版v1.0へ更新 |

## push/PR について
当初このリポジトリへの書き込み権限が無かったため作業はローカルのみで進めていたが、途中で権限が付与され、以降は`windows-core`ブランチへ適宜pushしている(pushは都度ユーザーに確認)。PRのマージ後、正式版`win-v1.0`をプレリリースとして公開し、テスト用の`win-v0.2`・`win-v0.3`は削除する。
