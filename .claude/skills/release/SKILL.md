---
name: release
description: PhotoForumPPTX(Mac版・Windows版)の新バージョンを出すとき(バージョン更新・What's New・GitHub Release)の手順。リリース、バージョンを上げる、What's New を書く、と言われたら使う。
---

# PhotoForumPPTX リリース手順

1〜3はMac版の手順。Windows版は末尾の「4. Windows版」に従う。

## 1. What's New は毎回スクリーンショット付きにする

UIや出力に見た目の変化がある版では、**必ず `Resources/whatsnew/<name>.png` を用意し、`ReleaseNote` の `image:` に指定する**(過去: 1.3 help / 1.4 fonts)。
見た目が変わらない版(内部修正のみ)だけ省略してよい。迷ったら入れる。

- 撮るもの: 新しく入った設定タブ・画面(アプリを起動して撮る)。複数あれば1.x版に複数枚か、1枚に収まる代表画面を選ぶ
- 角の黒ずみ対策: 透過を保ったままPNGにする(過去に "black corners" を修正済み)。ウィンドウ撮影は影込み・角丸の透過のまま保存する
- ファイル名は機能名(`spacing.png` など)。`build.sh` が `Resources/whatsnew/*.png` を .app に同梱する
- スクショは先にユーザーに見せて確認を取る

## 2. バージョンと履歴を更新

1. `Sources/PhotoForumPPTX/WhatsNew.swift` の `ReleaseNotes.all` の**先頭**に `ReleaseNote(version:items:image:)` を追加
2. `build.sh` の `VERSION:-x.y` を更新

## 3. PR → リリース

1. ブランチを切ってコミット・push、PRを作る(コミット・PRはユーザーの指示があってから)
2. マージ後、`main` で `./build.sh` を実行(`dist/PhotoForumPPTX.zip` ができる)
3. `gh release create vX.Y dist/PhotoForumPPTX.zip --target main --title vX.Y --notes "…"` (ノートはWhat's Newと同じ内容)
   - アプリの自動更新はリリースの `PhotoForumPPTX.zip` を見るので、アセット名を変えない
   - リリース作成は利用者に更新バナーが出る公開操作。ユーザーの許可を得てから実行する

## 4. Windows版

Mac版とは別系統のバージョンで、タグは `win-vX.Y`、アセットは `PhotoForumPPTX-win.zip`。

1. `windows/src/PhotoForumPPTX.Core/ReleaseNotes.cs` の `All` の**先頭**に `ReleaseNote` を追加(スクリーンショットは `windows/src/PhotoForumPPTX.App/Resources/whatsnew/` に置く)
2. `windows/src/PhotoForumPPTX.Core/AppInfo.cs` の `Version` を更新
3. ブランチを切ってコミット・push、PRを作る(コミット・PRはユーザーの指示があってから)
4. マージ後、`main` の `windows/` で `./build.ps1` を実行(`dist/PhotoForumPPTX-win.zip` ができる)
5. `gh release create win-vX.Y dist/PhotoForumPPTX-win.zip --target main --title "Windows vX.Y" --notes "…" --prerelease`
   - **`--prerelease` は必須**。付け忘れると `win-v*` がリポジトリの最新リリースになり、Mac版のインストールコマンド(`releases/latest/download/PhotoForumPPTX.zip`)が404になる
   - アプリの自動更新は `win-v` タグの `PhotoForumPPTX-win.zip` を見るので、タグの接頭辞とアセット名を変えない
   - リリース作成は利用者に更新バナーが出る公開操作。ユーザーの許可を得てから実行する
