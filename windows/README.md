# PhotoForumPPTX (Windows版・コア機能のみ)

Mac版(`Sources/`, `Resources/`)の核心機能 — EXIF読み取り・カメラ/レンズ名の対応表・テンプレートpptx生成 — をWindows(.NET 8)に移植したもの。
GUIはまだ実装していない。現状は確認用のCLIのみ。

## 構成
- `src/PhotoForumPPTX.Core` — コアロジック(class library)
  - `PhotoInfo.cs` — EXIF読み取り([MetadataExtractor](https://github.com/drewnoakes/metadata-extractor-dotnet))・焦点距離のStep Crop換算・シャッター/絞り/ISOの文字列化
  - `MapTable.cs` / `DefaultTables.cs` — カメラ型番・レンズ名 → 表示名の対応表(`%AppData%\PhotoForumPPTX\*.json`に保存、Mac版の`cameras.json`/`lenses.json`と同形式)
  - `PptxBuilder.cs` — `Resources/template.pptx` を展開し、写真ごとにスライドXMLを生成して再packする(Mac版の`PPTXBuilder.swift`と同じレイアウト計算をC#に移植)
  - `FieldKind.cs` / `SlideFonts.cs` — フィールド定義・フォント設定
- `src/PhotoForumPPTX.Cli` — 動作確認用コンソールアプリ
- `tests/PhotoForumPPTX.Core.Tests` — xUnitテスト(MapTableの永続化・バージョン管理、EXIF非依存のレイアウト計算、pptx生成の統合テスト)

## Mac版との違い・未対応
- GUI(写真選択・項目チェックボックス・設定画面)は未実装。今回はCLIのみ。
- 自動更新・新機能ポップアップ・ヘルプ画面は未移植。
- 画像デコードは`System.Drawing.Common`(Windows専用、GDI+)を使用。HEIC/RAWは非対応(JPEG/PNG/TIFF等のGDI+対応形式のみ)。EXIF自体は`MetadataExtractor`で読むためHEICでも値は取れるが、スライドに貼る画像の再エンコードが必要な場合(向き補正が必要なJPEG以外)は失敗する。

## ビルド・テスト
```sh
cd windows
dotnet build
dotnet test
```

## CLIでの動作確認
```sh
cd windows
dotnet run --project src/PhotoForumPPTX.Cli -- <template.pptxのパス> <出力先.pptx> <写真1> [写真2 ...] [--student 学籍番号] [--name 氏名]
```
例:
```sh
dotnet run --project src/PhotoForumPPTX.Cli -- ../Resources/template.pptx out.pptx photo1.jpg photo2.jpg --student 12345 --name "三輪太郎"
```
カメラ・レンズ・焦点距離・シャッタースピード・F値・ISO・備考(Step Crop)は既定で有効、学籍番号・氏名は値を渡したときだけ有効、タイトルは今回のCLIでは常に無効(GUI実装時に追加予定)。

## 次にやること(GUI・補助機能)
- GUIフレームワークの選定(WPF想定)と、写真選択・プレビュー・項目チェックボックス・設定画面の実装
- 自動更新・新機能ポップアップ・ヘルプ画面の移植
- 配布形態の検討(`dotnet publish` による自己完結exe化)
