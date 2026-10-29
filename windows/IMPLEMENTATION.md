# Windows版 実装まとめ (核心機能フェーズ)

対象コミット: `windows-core` ブランチ (`d858b22`)。Mac版(`Sources/`, `Resources/`, Swift/AppKit/SwiftUI)はこの作業では変更していない。

## 背景・方針

Mac版は SwiftUI / AppKit / ImageIO という macOS専用フレームワークに依存しており、そのままではWindowsで動かせない。
今回は「核心機能(EXIF読み取り → 対応表変換 → テンプレートpptx生成)を先に動く形にし、GUIや補助機能は後回し」という方針のもとWindows版の土台を作った。

### 技術選定

| 項目 | 選定 | 理由 |
|---|---|---|
| 言語・ランタイム | C# / .NET 8 (`net8.0-windows`) | Windows専用アプリとして割り切り、将来GUIをWPFで組みやすい。`dotnet publish`で自己完結exeも作れ、配布体験がMac版のzip配布に近い |
| EXIF読み取り | [MetadataExtractor](https://github.com/drewnoakes/metadata-extractor-dotnet) (NuGet) | 純C#実装でJPEG/TIFFのExif IFD0・Exif SubIFDを型安全に読める。macOSの`ImageIO`相当 |
| 画像デコード・再エンコード | `System.Drawing.Common` (GDI+, Windows専用) | Windows専用に限定したことでライセンス上の制約なく使える。`RotateFlip`でEXIF Orientationの焼き込みができ、Mac版の`CGImageSourceCreateThumbnailWithTransform`相当の処理になる |
| pptx組み立て | `System.IO.Compression.ZipFile` + 文字列/正規表現によるXML編集 | Mac版が`/usr/bin/zip`・`/usr/bin/unzip`をシェル実行していた部分を.NET標準APIに置き換え、外部コマンド依存を排除(Mac版より依存が減り、むしろ改善) |

.NET SDKは当初未インストールだったため、`winget install Microsoft.DotNet.SDK.8`でセットアップ済み(.NET 8.0.427)。

## ディレクトリ構成

```
windows/
  PhotoForumPPTX.sln
  README.md                    使い方・既知の制約
  IMPLEMENTATION.md             このファイル
  src/
    PhotoForumPPTX.Core/        コアロジック(class library)
    PhotoForumPPTX.Cli/         動作確認用コンソールアプリ
  tests/
    PhotoForumPPTX.Core.Tests/  xUnitテスト
```

## Mac版 → Windows版 の対応表

| Mac (Swift) | Windows (C#) | 内容 |
|---|---|---|
| `Model.swift` の `FieldKind` | `FieldKind.cs` | フィールド種別・ラベル・フォントサイズ・グループ(レイアウト上の配置グループ)。enum + 拡張メソッドで同じ分岐ロジックを再現 |
| `Model.swift` の `Photo` (EXIF抽出部分) | `PhotoInfo.cs` | EXIFからカメラ・レンズ・焦点距離・シャッター・絞り・ISOを抽出。GUI用の`@Published`やサムネイル生成は対象外(今回のフェーズでは不要) |
| `Tables.swift` の `MapTable` / `DefaultTables` | `MapTable.cs` / `DefaultTables.cs` | カメラ型番・レンズ名 → 表示名の対応表。`%AppData%\PhotoForumPPTX\cameras.json` / `lenses.json` に保存。既定値のバージョンアップ時に未登録キーだけ追加するロジックも移植(バージョン番号は`<file>.json.version`という別ファイルに保存。Mac版はUserDefaultsのキーで管理しているため保存先の仕組みだけ異なり、挙動は同じ) |
| `PPTXBuilder.swift` | `PptxBuilder.cs` | テンプレートpptxの展開 → 既存スライド等の削除 → 写真ごとにスライドXML生成 → `[Content_Types].xml` / `presentation.xml` / `presentation.xml.rels` の書き換え → zip再構築。EMU単位のレイアウト計算(写真サイズ・テキストボックス位置・赤線の上下でのグルーピング・行数推定`estimateLines`など)は数値レベルで1:1移植 |
| `SlideFonts` | `SlideFonts.cs` | 既定フォント(欧文Arial / 和文テンプレート追従)。record型で不変値として表現 |
| `Views.swift` / `HelpView.swift` / `WhatsNew.swift` / `Updater.swift` / `ReleaseCheck.swift` | (未移植) | GUI・ヘルプ・自動更新。今回のフェーズでは対象外 |

### 挙動面での細かい留意点
- シャッタースピードの文字列化 (`ShutterText`) は Mac版の `NumberFormatter(.decimal)` によるカンマ区切り(例: `1/8,000`)をそのまま`ToString("N0")`で再現。意図的に変えていない。
- EXIF Orientation (1–8) の縦横入れ替え・回転焼き込みロジックは Swift の `(5...8).contains(o)` 分岐、`RotateFlip`マッピングともにMac版の向き補正と等価になるよう対応づけた。
- デジタルズーム(RX1R IIIのStep Cropなど)による焦点距離の実効値換算・備考欄への自動補完もそのまま移植。
- XMLエスケープは`&` `<` `>`のみ(Mac版も同様、属性に利用者文字列を入れないため`"`のエスケープは元々不要)。

## 新規に追加したテスト用の仕組み

- `PhotoInfo` の本来のコンストラクタは `private`(EXIF読み取りを経由した`Load`のみが公開API)だが、EXIF抽出を介さずにレイアウト計算だけを単体テストしたいため、`internal static PhotoInfo ForTesting(...)` を追加し、`InternalsVisibleTo`でテストプロジェクトにのみ公開した。
- テスト用フィクスチャとして `tests/.../Fixtures/sample.jpg`(320×240の単色JPEG、PowerShell上で`System.Drawing`を使って生成)をリポジトリに追加。個人情報・実写真は含まない。
- `Resources/template.pptx` はテストプロジェクトから`Link`参照してビルド時に出力ディレクトリへコピーする設定にし、テンプレート本体の複製は作っていない。

## 動作確認

1. **ユニット/統合テスト**: `dotnet test` → 24件すべて成功。
   - `FieldKindTests` — ラベル・フォントサイズ・グループがテンプレート仕様と一致すること
   - `MapTableTests` — 大文字小文字・空白無視のlookup、バージョンアップ時の既定値マージ、削除したエントリがバージョン不変の間は復活しないこと
   - `PptxBuilderTests` — XMLエスケープ、タイトルの「」補完、露出情報の文言フォーマット、行数推定、そして**2枚の写真(うち1枚は90度回転のEXIF Orientation=6)からpptxを生成し、`slide1.xml`/`slide2.xml`・`photo1.jpeg`/`photo2.jpeg`がzip内に存在し`slide3.xml`は存在しないこと**を検証する統合テスト
2. **CLIでの実行確認**: `dotnet run --project src/PhotoForumPPTX.Cli -- <template> <output> <photo> --student 12345 --name "三輪太郎"` を実行し、pptxが生成されることを確認。
3. **実際にPowerPointで開けるか確認**: 生成したpptxをPowerPoint COM Automation (`New-Object -ComObject PowerPoint.Application`) 経由で開き、`SlideCount=1`が返ることを確認済み(ファイルが壊れていない・OOXMLとして正しいことの実証)。

## 既知の制約(次フェーズ以降で対応)

- **GUIなし**: 写真選択・項目チェックボックス・設定画面は未実装。現状は`PhotoForumPPTX.Cli`での動作確認のみ。
- **HEIC/RAW非対応**: `System.Drawing.Common`(GDI+)がデコードできる形式(JPEG/PNG/TIFF/BMP/GIFなど)のみ画像埋め込みに対応。EXIF自体は`MetadataExtractor`がHEICも読めるが、画像の再エンコードが必要なケース(向き補正が必要な非JPEG)では失敗する。
- **自動更新・新機能ポップアップ・ヘルプ画面**: 未移植(`Updater.swift` / `ReleaseCheck.swift` / `WhatsNew.swift` / `HelpView.swift` 相当)。
- **配布形態未検討**: `dotnet publish --self-contained -p:PublishSingleFile=true` 等による単一exe化はまだ試していない。

## push/PR について

リポジトリ `mksmkss/PhotoForumPPTX` への書き込み権限がこの環境の git認証(`MiwaHiroki`)には無く、`git push`が403で拒否された。
そのため `windows-core` ブランチはローカルに作成済み(`main`は`origin/main`のまま変更していない)で、push・PR作成はユーザー側に依頼した。

```sh
git push -u origin windows-core
gh pr create --title "Windows版の核心機能(.NET 8)を追加" --body "..."
```
