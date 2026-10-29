# zip(PhotoForumPPTX-win.zip)を受け取った人向け:
# このスクリプトとzipを同じフォルダに置いて、PowerShellで次を実行してください
#   powershell -ExecutionPolicy Bypass -File install.ps1
# ダウンロードしたファイルに付く「インターネットからのファイルです」マークを外してから
# %LocalAppData%\Programs\PhotoForumPPTX に展開し、起動します(管理者権限は不要)。
$ErrorActionPreference = "Stop"
Set-Location $PSScriptRoot

$zip = Join-Path $PSScriptRoot "PhotoForumPPTX-win.zip"
if (-not (Test-Path $zip)) {
    Write-Error "PhotoForumPPTX-win.zip が見つかりません。このスクリプトと同じフォルダに置いてください。"
}

$dest = Join-Path $env:LOCALAPPDATA "Programs\PhotoForumPPTX"
if (Test-Path $dest) { Remove-Item -Recurse -Force $dest }
New-Item -ItemType Directory -Path $dest | Out-Null

Unblock-File $zip
Expand-Archive -Path $zip -DestinationPath $dest -Force
Get-ChildItem -Path $dest -Recurse | Unblock-File

$exe = Join-Path $dest "PhotoForumPPTX.App.exe"
Write-Output "Installed: $exe"
Start-Process $exe
