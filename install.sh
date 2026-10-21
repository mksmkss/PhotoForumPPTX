#!/bin/sh
# zipを受け取った人向け: このスクリプトと PhotoForumPPTX.zip を同じフォルダに置いて実行
#   sh install.sh
# Gatekeeperの隔離属性を外して /Applications に入れる(未公証アプリ用)
set -eu
cd "$(dirname "$0")"
rm -rf /Applications/PhotoForumPPTX.app
unzip -q PhotoForumPPTX.zip -d /Applications
xattr -dr com.apple.quarantine /Applications/PhotoForumPPTX.app
echo "Installed: /Applications/PhotoForumPPTX.app"
