# PhotoForumPPTX

写真を選ぶと、EXIFからカメラ名・レンズ名・焦点距離・SS・F値・ISOを読み取り、
「写真表現Ⅰ 写真投稿フォーラム」テンプレートの書式で pptx を作る Mac アプリ。複数枚選ぶと複数ページになります。

## インストール(配布を受ける人向け)
ターミナルに次の**1行を貼り付けて Enter**するだけです。最新版のダウンロード → `/Applications` に配置 → 起動 まで自動で行います(環境構築は不要)。

```sh
curl -fL https://github.com/mksmkss/PhotoForumPPTX/releases/latest/download/PhotoForumPPTX.zip -o /tmp/PhotoForumPPTX.zip && unzip -oq /tmp/PhotoForumPPTX.zip -d /Applications && xattr -dr com.apple.quarantine /Applications/PhotoForumPPTX.app && open /Applications/PhotoForumPPTX.app
```

- 動作環境: macOS 13 以降(Apple Silicon / Intel 両対応)。
- アプリが起動中なら、先に終了してから実行してください。
- 入れた後は、起動時に新しい版を確認し、あればバナーの「今すぐ更新」ボタンだけで更新できます。
- `/Applications` に書き込めない場合は、`mkdir -p ~/Applications` を先に実行し、上の1行の `/Applications` を `~/Applications` に変えてください。
- ネットワークを使わずzipを直接受け取った場合は、zipを `~/Downloads` に置いて次の1行を実行します。
  ```sh
  unzip -oq ~/Downloads/PhotoForumPPTX.zip -d /Applications && xattr -dr com.apple.quarantine /Applications/PhotoForumPPTX.app && open /Applications/PhotoForumPPTX.app
  ```
- 公証していないため、Finderで展開して開くと初回に「開発元を検証できません」と出ます。その場合も上の1行で解決します。
  (「右クリック→開く」だけでは通らないことがあります。macOS Sequoia以降は、システム設定 → プライバシーとセキュリティ → 「このまま開く」が必要です)

## アップデート(すでに入れている人)
v1.2 以降は、新しい版が出ると起動時にバナーが出ます。「今すぐ更新」を押すだけで、ダウンロード → 入れ替え → 再起動まで自動です。
メニュー「写真フォーラムPPTX → アップデートを確認…」で手動確認もできます(オフライン時は「zipファイルからアップデート…」、またはzipをウィンドウにドロップ)。
v1.1 以前を使っている人は、一度だけ上の1行コマンドで最新版にしてください。

## リリース手順(開発者向け)
```sh
VERSION=1.3 ./build.sh
gh release create v1.3 dist/PhotoForumPPTX.zip --title "v1.3" --notes "変更点"
```
アプリは GitHub Releases の最新タグ(`vX.Y`)と自身のバージョンを比べて、新しければ `PhotoForumPPTX.zip` を取得します。

## 困ったとき・要望
メニューバーの **Help → 使い方** に説明があります。不具合や「こうなっていたら嬉しい」という要望は、
**Help → 不具合・要望を報告 (GitHub Issue)…** から [Issue](https://github.com/mksmkss/PhotoForumPPTX/issues) を立ててください(GitHubアカウントが必要。アプリのバージョンとmacOSの情報が入った状態で書き始められます)。

## 使い方
- 「写真を選択…」またはドラッグ&ドロップ。写真ごとにEXIFの値を手修正できます。
- 右のリストで、スライドに入れる項目を選択(学籍番号・氏名・タイトルは初期オフ)。
  - 学籍番号・氏名・タイトル: 上から順に配置
  - カメラ・レンズ: 赤線の上 / 焦点距離・SS・F値・ISO: 赤線の下(テンプレート通り)
- 設定(⌘,)でカメラ型番→表示名、レンズ名の対応表を編集できます。
- 「書き出し (.pptx)」。保存先の初期値はダウンロード。

## ビルド(Command Line Toolsのみで可)
```sh
./build.sh   # dist/PhotoForumPPTX.zip を生成 (arm64 + x86_64, ad-hoc署名, 公証なし)
```
`Resources/template.pptx` は元テンプレートから既存スライド・写真・作成者情報を除いた空版です。
