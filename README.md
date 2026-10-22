# PhotoForumPPTX

写真を選ぶと、EXIFからカメラ名・レンズ名・焦点距離・SS・F値・ISOを読み取り、
「写真表現Ⅰ 写真投稿フォーラム」テンプレートの書式で pptx を作る Mac アプリ。複数枚選ぶと複数ページになります。

## インストール(配布を受ける人)
`PhotoForumPPTX.zip` を `~/Downloads` に置いて、ターミナルに次の1行を貼り付けます。
展開 → `/Applications` に配置 → 隔離属性の解除 → 起動 まで一度に終わります。更新版も同じ1行で上書きできます。

```sh
unzip -oq ~/Downloads/PhotoForumPPTX.zip -d /Applications && xattr -dr com.apple.quarantine /Applications/PhotoForumPPTX.app && open /Applications/PhotoForumPPTX.app
```

- アプリが起動中なら、先に終了してから実行してください。
- zipの名前が `PhotoForumPPTX (1).zip` などになっている場合は、パスをその名前に読み替えてください。
- `/Applications` に書き込めない場合は、`mkdir -p ~/Applications` を先に実行し、上の1行の `/Applications` を `~/Applications` に変えてください。
- 公証していないため、Finderで展開した場合は初回に「開発元を検証できません」と出ます。その場合も上の1行で解決します。
  (「右クリック→開く」だけでは通らないことがあります。macOS Sequoia以降は、システム設定 → プライバシーとセキュリティ → 「このまま開く」が必要です)

## アップデート(すでに入れている人)
v1.1 以降は、アプリ内でアップデートできます。新しい `PhotoForumPPTX.zip` を**アプリのウィンドウにドロップ**するか、
メニュー「写真フォーラムPPTX → アップデート…」でzipを選ぶと、入れ替えて自動で再起動します(ターミナル不要)。
v1.0 を使っている人は、一度だけ上の1行コマンドで v1.1 にしてください。

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
