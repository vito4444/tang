#!/usr/bin/env bash
# 一键出 Windows x64 包。
# 需要装有 Unity 6000.0.76f1+（含 Windows Build Support）并已激活许可。
# 用法:
#   UNITY_PATH=/path/to/Unity ./tools/build_windows.sh
#   （macOS 例：/Applications/Unity/Hub/Editor/6000.0.76f1/Unity.app/Contents/MacOS/Unity）
set -euo pipefail
cd "$(dirname "$0")/.."

UNITY_PATH="${UNITY_PATH:-}"
if [[ -z "$UNITY_PATH" ]]; then
  # 常见 Hub 安装位置探测
  for candidate in \
    "$HOME"/Unity/Hub/Editor/6000.0.*/Editor/Unity \
    /opt/unity/editors/6000.0.*/Editor/Unity; do
    if [[ -x "$candidate" ]]; then
      UNITY_PATH="$candidate"
      break
    fi
  done
fi
if [[ -z "$UNITY_PATH" || ! -x "$UNITY_PATH" ]]; then
  echo "错误：找不到 Unity。请设 UNITY_PATH 环境变量。" >&2
  exit 2
fi

mkdir -p Builds/logs
LOG="$(pwd)/Builds/logs/build_win64.log"

echo "Unity: $UNITY_PATH"
echo "日志:  $LOG"

set +e
"$UNITY_PATH" -batchmode -nographics -quit \
  -projectPath "$(pwd)/LingyanUnity" \
  -buildTarget Win64 \
  -executeMethod Lingyan.EditorTools.BuildScript.BuildWindows \
  -logFile "$LOG"
CODE=$?
set -e

if [[ $CODE -ne 0 ]]; then
  echo "构建失败（退出码 $CODE），日志末 40 行：" >&2
  tail -40 "$LOG" >&2
  exit $CODE
fi

echo "构建成功：Builds/Win64/Lingyan/Lingyan.exe"
