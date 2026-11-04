# PhotoForumPPTX (Windows版) を自己完結exeとして発行し、配布用zipにまとめる
# .NET SDKのみで可。GitHub Releasesに添付する際は "win-vX.Y" タグ名で、
# アセット名は PhotoForumPPTX-win.zip のままにすること(自動更新がこの名前を探す)
$ErrorActionPreference = "Stop"
Set-Location $PSScriptRoot

$dist = Join-Path $PSScriptRoot "dist"
if (Test-Path $dist) { Remove-Item -Recurse -Force $dist }
$publishDir = Join-Path $dist "PhotoForumPPTX"

dotnet publish "src/PhotoForumPPTX.App/PhotoForumPPTX.App.csproj" `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -o $publishDir
# 注意: IncludeNativeLibrariesForSelfExtract=true は付けない。
# 付けるとResources配下のコンテンツファイル(template.pptx)が実行時に展開されない形で
# バンドルされてしまい、起動後に見つからなくなる(WPFのネイティブdllは少数そのまま隣に残る)。

$zipPath = Join-Path $dist "PhotoForumPPTX-win.zip"
Compress-Archive -Path (Join-Path $publishDir "*") -DestinationPath $zipPath -Force

Write-Output "OK: $zipPath"
