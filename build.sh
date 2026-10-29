#!/bin/sh
# PhotoForumPPTX.app を作って配布用zipにまとめる (Xcode不要、Command Line Toolsだけで可)
set -e
VERSION=${VERSION:-1.6}   # リリース時: VERSION=1.2 ./build.sh
cd "$(dirname "$0")"
mkdir -p .build
for a in arm64 x86_64; do
  swiftc -O -target $a-apple-macosx13.0 -o .build/PhotoForumPPTX-$a Sources/PhotoForumPPTX/*.swift
done
APP=dist/PhotoForumPPTX.app
rm -rf dist && mkdir -p $APP/Contents/MacOS $APP/Contents/Resources
lipo -create .build/PhotoForumPPTX-arm64 .build/PhotoForumPPTX-x86_64 -output $APP/Contents/MacOS/PhotoForumPPTX
cp Resources/template.pptx Resources/AppIcon.icns $APP/Contents/Resources/
mkdir -p $APP/Contents/Resources/whatsnew && cp Resources/whatsnew/*.png $APP/Contents/Resources/whatsnew/ 2>/dev/null || true
cat > $APP/Contents/Info.plist <<P
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0"><dict>
<key>CFBundleName</key><string>写真フォーラムPPTX</string>
<key>CFBundleExecutable</key><string>PhotoForumPPTX</string>
<key>CFBundleIdentifier</key><string>local.photoforumpptx</string>
<key>CFBundleIconFile</key><string>AppIcon</string>
<key>CFBundlePackageType</key><string>APPL</string>
<key>CFBundleVersion</key><string>$VERSION</string>
<key>CFBundleShortVersionString</key><string>$VERSION</string>
<key>LSMinimumSystemVersion</key><string>13.0</string>
<key>NSHighResolutionCapable</key><true/>
</dict></plist>
P
xattr -cr $APP                            # 拡張属性を除去(zipに ._ ファイルが混ざると署名が壊れる)
codesign --force --deep --sign - $APP     # ad-hoc署名(Apple Silicon で起動に必須。公証ではない)
ditto -c -k --keepParent --norsrc --noextattr --noacl $APP dist/PhotoForumPPTX.zip
echo "OK: dist/PhotoForumPPTX.zip"
