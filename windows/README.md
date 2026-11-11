# PhotoForumPPTX (Windows版)

写真を選ぶと、EXIFからカメラ名・レンズ名・焦点距離・SS・F値・ISOを読み取り、
「写真表現Ⅰ 写真投稿フォーラム」テンプレートの書式で pptx を作る Windows アプリ。
Mac版(`Sources/`, `Resources/`)の核心機能を .NET 8 / WPF に移植したもの。

## インストール・アップデート

[ルートのREADME](../README.md#windowsユーザー)を参照してください。

## 使い方

- 「写真を選択…」ボタンを押下または写真をドラッグ&ドロップして写真を追加します。写真ごとにEXIFの値を手修正できます(カメラ・レンズは入力中に候補が出ます)。
- 右のリストで、スライドに入れる項目を選択します(学籍番号・氏名・タイトルは初期値オフ)。リストの順番がそのままスライドでの表示順です。線で区切られたグループ内では▲▼ボタンで並べ替えられ、「並び順を初期値に戻す」ボタンで元の順番に戻せます。
- 設定(メイン画面の「設定…」ボタン)でカメラ型番→表示名、レンズ名の対応表と、スライドのフォント(欧文・和文)・行間・自動オープン設定を編集できます。
- 「書き出し (.pptx)」。保存先の初期値はダウンロードです。
- 写真を選ぶ画面と保存先を選ぶ画面は、2回目以降は前回のフォルダで開きます。

## 困ったとき・要望

メニュー **ヘルプ → 使い方** に説明があります。不具合や要望は
**ヘルプ → 不具合・要望を報告 (GitHub Issue)…** から [Issue](https://github.com/mksmkss/PhotoForumPPTX/issues) を立ててください。

## ビルド(.NET 8 SDKのみで可。Visual Studio不要)

```powershell
cd windows
dotnet build       # 開発時のビルド
dotnet test        # ユニットテスト(Core/Gui合わせて114件)
dotnet run --project src/PhotoForumPPTX.App   # 実行
./build.ps1        # dist/PhotoForumPPTX-win.zip を生成 (win-x64, 自己完結)
```

## リリース手順(開発者向け)

```powershell
# 1. windows/src/PhotoForumPPTX.Core/ReleaseNotes.cs の All の先頭に変更点を1件足す
# 2. windows/src/PhotoForumPPTX.Core/AppInfo.cs の Version を上げる
# 3. ビルドしてリリース
cd windows
./build.ps1
gh release create win-v1.1 dist/PhotoForumPPTX-win.zip --title "Windows v1.1" --notes "変更点" --target main --prerelease
```

`--prerelease` は必須です。付け忘れると `win-v*` がリポジトリの最新リリースになり、Mac版のインストールコマンド(`releases/latest/download/PhotoForumPPTX.zip`)が404になります。

アプリは GitHub Releases の`win-v`で始まるタグのうち最新のものと自身のバージョンを比べて、
新しければ `PhotoForumPPTX-win.zip` を取得します(Mac版の`v`タグとは別系統)。

## 構成

- `src/PhotoForumPPTX.Core` — コアロジック(class library、WPF非依存)
  - `PhotoInfo.cs` — EXIF読み取り([MetadataExtractor](https://github.com/drewnoakes/metadata-extractor-dotnet))・焦点距離のStep Crop換算・シャッター/絞り/ISOの文字列化
  - `MapTable.cs` / `DefaultTables.cs` — カメラ型番・レンズ名 → 表示名の対応表(`%AppData%\PhotoForumPPTX\*.json`)
  - `PptxBuilder.cs` — `Resources/template.pptx` を展開し、写真ごとにスライドXMLを生成して再packする(Mac版`PPTXBuilder.swift`のレイアウト計算を移植)
  - `FieldKind.cs` — スライドに入れる項目の種類と、文字サイズ・配置グループ(項目リストの並び順にも使う)
  - `AppSettings.cs` / `AppSettingsStore.cs` — 学籍番号・氏名・項目設定・フォント・行間等の永続化(`%AppData%\PhotoForumPPTX\settings.json`、アトミック書き込み)
  - `ReleaseNotes.cs` / `AppInfo.cs` / `UpdateCheck.cs` / `UpdateScriptBuilder.cs` — 新機能ポップアップ・自動更新のロジック
- `src/PhotoForumPPTX.Gui` — ViewModel層(MVVM、WPF非依存でテスト可能)
- `src/PhotoForumPPTX.App` — WPFアプリ本体(View・ダイアログ・実際の更新処理)
- `src/PhotoForumPPTX.Cli` — 動作確認用コンソールアプリ
- `tests/PhotoForumPPTX.Core.Tests`, `tests/PhotoForumPPTX.Gui.Tests` — xUnitテスト(計114件)

## Mac版との違い・制約

- 対応範囲はMac版v1.6相当と、書き出し後の自動オープンまで。v2.0の2段組み・背景色・レイアウト選択・レンズ記憶は未対応。
- zipからのアップデート(zipのドロップ・選択)は未対応。アプリ内のオンライン自動更新を使う。
- 「スライドに入れる項目」は、リストをスライドでの表示順に固定し、並べ替えを同じグループ内に限定している。Mac版はドラッグで自由に並べ替えられるが、スライド上の位置はグループで決まるため、書き出し結果は同じ。
- 画像デコードは`System.Drawing.Common`(Windows専用、GDI+)を使用。HEIC/RAWは非対応(JPEG/PNG/BMP/TIFF等のGDI+対応形式のみ)。写真の寸法の取得にもGDI+を使うため、HEIC/RAWは追加した時点で「読み込み失敗(スキップ)」になる。
- 未署名アプリのため、初回起動時にSmartScreenの警告が出る(コード署名証明書が無いため回避不可)。
