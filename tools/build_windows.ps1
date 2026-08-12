# 一键出 Windows x64 包（Windows PowerShell 版）。
# 需要 Unity 6000.0.76f1+（含 Windows Build Support）并已激活许可。
# 用法:
#   $env:UNITY_PATH = "C:\Program Files\Unity\Hub\Editor\6000.0.76f1\Editor\Unity.exe"
#   .\tools\build_windows.ps1
$ErrorActionPreference = "Stop"
Set-Location (Join-Path $PSScriptRoot "..")

$unity = $env:UNITY_PATH
if (-not $unity) {
    $candidates = Get-ChildItem "C:\Program Files\Unity\Hub\Editor\6000.0.*\Editor\Unity.exe" -ErrorAction SilentlyContinue
    if ($candidates) { $unity = $candidates[0].FullName }
}
if (-not $unity -or -not (Test-Path $unity)) {
    Write-Error "找不到 Unity。请设置 UNITY_PATH 环境变量。"
    exit 2
}

New-Item -ItemType Directory -Force -Path "Builds\logs" | Out-Null
$log = Join-Path (Get-Location) "Builds\logs\build_win64.log"

Write-Host "Unity: $unity"
Write-Host "日志:  $log"

& $unity -batchmode -nographics -quit `
    -projectPath (Join-Path (Get-Location) "LingyanUnity") `
    -buildTarget Win64 `
    -executeMethod Lingyan.EditorTools.BuildScript.BuildWindows `
    -logFile $log

if ($LASTEXITCODE -ne 0) {
    Write-Host "构建失败（退出码 $LASTEXITCODE），日志末 40 行："
    Get-Content $log -Tail 40
    exit $LASTEXITCODE
}

Write-Host "构建成功：Builds\Win64\Lingyan\Lingyan.exe"
