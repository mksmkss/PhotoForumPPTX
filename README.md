# PhotoForumPPTX

写真を選ぶと、EXIFからカメラ名・レンズ名・焦点距離・SS・F値・ISOを読み取り、
「写真表現Ⅰ 写真投稿フォーラム」テンプレートの書式で pptx を作る Mac / Windows アプリ。複数枚選ぶと複数ページになります。

- [macOSユーザー](#macosユーザー): macOS 13 以降(Apple Silicon / Intel 両対応)
- [Windowsユーザー](#windowsユーザー): Windows 10/11 (64bit)。Mac版v1.6相当の機能に対応(2段組みなどv2.0の機能は未対応)

# macOSユーザー

## インストール
ターミナルに次の**1行を貼り付けて Enter**するだけです。最新版のダウンロード → `/Applications` に配置 → 起動 まで自動で行います(環境構築は不要)。

```sh
curl -fL https://github.com/mksmkss/PhotoForumPPTX/releases/latest/download/PhotoForumPPTX.zip -o /tmp/PhotoForumPPTX.zip && unzip -oq /tmp/PhotoForumPPTX.zip -d /Applications && xattr -dr com.apple.quarantine /Applications/PhotoForumPPTX.app && open /Applications/PhotoForumPPTX.app
```

- アプリが起動中なら、先に終了してから実行してください。
- 入れた後は、起動時に新しい版を確認し、あればバナーの「今すぐ更新」ボタンだけで更新できます。
- `/Applications` に書き込めない場合は、`mkdir -p ~/Applications` を先に実行し、上の1行の `/Applications` を `~/Applications` に変えてください。
- ネットワークを使わずzipを直接受け取った場合は、zipを `~/Downloads` に置いて次の1行を実行します。
  ```sh
  unzip -oq ~/Downloads/PhotoForumPPTX.zip -d /Applications && xattr -dr com.apple.quarantine /Applications/PhotoForumPPTX.app && open /Applications/PhotoForumPPTX.app
  ```
- 公証していないため、Finderで展開して開くと初回に「開発元を検証できません」と出ます。その場合も上の1行で解決します。
  (「右クリック→開く」だけでは通らないことがあります。macOS Sequoia以降は、システム設定 → プライバシーとセキュリティ → 「このまま開く」が必要です)

## アップデート
v1.2 以降は、新しい版が出ると起動時にバナーが出ます。「今すぐ更新」を押すだけで、ダウンロード → 入れ替え → 再起動まで自動です。
メニュー「写真フォーラムPPTX → アップデートを確認…」で手動確認もできます(オフライン時は「zipファイルからアップデート…」、またはzipをウィンドウにドロップ)。
v1.1 以前を使っている人は、一度だけ上の1行コマンドで最新版にしてください。

アップデート後の初回起動で、変更点(スクリーンショット付き)が「新機能」ウィンドウに出ます。メニュー Help → 新機能(更新履歴)でいつでも見返せます。

# Windowsユーザー

## インストール
PowerShellを開いて、次のコマンドをまとめて貼り付けてEnterするだけです。
最新版のダウンロード → `%LocalAppData%\Programs\PhotoForumPPTX` に配置 → 起動 まで自動で行います(環境構築は不要)。

```powershell
$r = (Invoke-RestMethod "https://api.github.com/repos/mksmkss/PhotoForumPPTX/releases") | Where-Object { $_.tag_name -like "win-v*" } | Select-Object -First 1
$asset = $r.assets | Where-Object { $_.name -eq "PhotoForumPPTX-win.zip" }
$dest = "$env:LOCALAPPDATA\Programs\PhotoForumPPTX"
if (Test-Path $dest) { Remove-Item -Recurse -Force $dest }
Invoke-WebRequest $asset.browser_download_url -OutFile "$env:TEMP\PhotoForumPPTX-win.zip"
Expand-Archive "$env:TEMP\PhotoForumPPTX-win.zip" -DestinationPath $dest -Force
Get-ChildItem $dest -Recurse | Unblock-File
Start-Process "$dest\PhotoForumPPTX.App.exe"
```

- .NET のインストールは不要です(自己完結パッケージ)。
- アプリが起動中なら、先に終了してから実行してください。
- 初回起動時に「Windows によって PC が保護されました」と出ることがあります(未署名アプリのため)。「詳細情報」→「実行」で起動できます。
- デスクトップにショートカットが欲しい場合は、`%LocalAppData%\Programs\PhotoForumPPTX\PhotoForumPPTX.App.exe` を右クリックして「ショートカットの作成」してください。
- ネットワークを使わずzipを直接受け取った場合は、[`windows/install.ps1`](windows/install.ps1) と `PhotoForumPPTX-win.zip` を同じフォルダに置いて次を実行します。
  ```powershell
  powershell -ExecutionPolicy Bypass -File install.ps1
  ```

## アップデート
新しい版が出ると、起動時に上部にバナーが出ます。「今すぐ更新」を押すだけで、ダウンロード → 入れ替え → 再起動まで自動です。
設定 → 情報タブで手動確認もできます。

アップデート後の初回起動で、変更点が「新機能」ウィンドウに出ます。メニュー ヘルプ → 新機能(更新履歴)でいつでも見返せます。

# 使い方
- 「写真を選択…」またはドラッグ&ドロップ。写真ごとにEXIFの値を手修正できます。
- 右のリストで、スライドに入れる項目を選択(学籍番号・氏名・タイトルは初期オフ)。Windows版は▲▼ボタンで並べ替えられます。
  - 学籍番号・氏名・タイトル: 上から順に配置
  - カメラ・レンズ: 赤線の上 / 焦点距離・SS・F値・ISO: 赤線の下(テンプレート通り)
- 設定(Macは⌘,、Windowsはメイン画面の「設定…」ボタン)でカメラ型番→表示名、レンズ名の対応表と、スライドのフォント(欧文・和文)を編集できます。
- 「書き出し (.pptx)」。保存先の初期値はダウンロード。

# 困ったとき・要望
メニューの **Help(ヘルプ) → 使い方** に説明があります。不具合や「こうなっていたら嬉しい」という要望は、
**Help(ヘルプ) → 不具合・要望を報告 (GitHub Issue)…** から [Issue](https://github.com/mksmkss/PhotoForumPPTX/issues) を立ててください(GitHubアカウントが必要。アプリのバージョンとOSの情報が入った状態で書き始められます)。

# 開発者向け

## macOS

### ビルド(Command Line Toolsのみで可)
```sh
./build.sh   # dist/PhotoForumPPTX.zip を生成 (arm64 + x86_64, ad-hoc署名, 公証なし)
```
`Resources/template.pptx` は元テンプレートから既存スライド・写真・作成者情報を除いた空版です。

### リリース手順
```sh
# 1. Sources/PhotoForumPPTX/WhatsNew.swift の ReleaseNotes.all の先頭に変更点を1件足す
#    (スクリーンショットは Resources/whatsnew/<名前>.png に置き、image: "<名前>" で参照)
# 2. ビルドしてリリース
VERSION=1.6 ./build.sh
gh release create v1.6 dist/PhotoForumPPTX.zip --title "v1.6" --notes "変更点"
```
アプリは GitHub Releases の最新タグ(`vX.Y`)と自身のバージョンを比べて、新しければ `PhotoForumPPTX.zip` を取得します。

## Windows
ビルド・リリース手順・構成は [windows/README.md](windows/README.md) を参照してください。
