#!/bin/sh
# PhotoForumPPTX.app を作って配布用zipにまとめる (Xcode不要、Command Line Toolsだけで可)
set -e
cd "$(dirname "$0")"
mkdir -p .build
for a in arm64 x86_64; do
  swiftc -O -target $a-apple-macosx13.0 -o .build/PhotoForumPPTX-$a Sources/PhotoForumPPTX/*.swift
done
APP=dist/PhotoForumPPTX.app
rm -rf dist && mkdir -p $APP/Contents/MacOS $APP/Contents/Resources
lipo -create .build/PhotoForumPPTX-arm64 .build/PhotoForumPPTX-x86_64 -output $APP/Contents/MacOS/PhotoForumPPTX
cp Resources/template.pptx $APP/Contents/Resources/
cat > $APP/Contents/Info.plist <<P
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0"><dict>
<key>CFBundleName</key><string>写真フォーラムPPTX</string>
<key>CFBundleExecutable</key><string>PhotoForumPPTX</string>
<key>CFBundleIdentifier</key><string>local.photoforumpptx</string>
<key>CFBundlePackageType</key><string>APPL</string>
<key>CFBundleVersion</key><string>1</string>
<key>CFBundleShortVersionString</key><string>1.0</string>
<key>LSMinimumSystemVersion</key><string>13.0</string>
<key>NSHighResolutionCapable</key><true/>
</dict></plist>
P
codesign --force --deep --sign - $APP     # ad-hoc署名(Apple Silicon で起動に必須。公証ではない)
ditto -c -k --keepParent $APP dist/PhotoForumPPTX.zip
echo "OK: dist/PhotoForumPPTX.zip"
