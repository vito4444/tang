#!/usr/bin/env bash
# 冒烟：一条命令跑完数据层测试、Unity 侧编译检查、术语/存档契约、meta 完整性。
# 任一环节失败即非零退出。装了 Unity（UNITY_PATH）时追加 Unity editmode 测试。
set -euo pipefail
cd "$(dirname "$0")/.."

echo "== 1/4 dotnet 测试（域模型契约 + 术语校验 + 存档迁移链）=="
dotnet test core-dotnet/Lingyan.sln --nologo -v q

echo "== 2/4 Unity 侧代码编译检查（对 API 桩）=="
dotnet build core-dotnet/Lingyan.CompileCheck --nologo -v q

echo "== 3/4 数据层端到端演示 =="
dotnet run --project core-dotnet/Lingyan.Demo --nologo -v q

echo "== 4/4 .meta 完整性 =="
python3 tools/gen_meta.py --check

if [[ -n "${UNITY_PATH:-}" && -x "${UNITY_PATH:-}" ]]; then
  echo "== 附加：Unity editmode 测试 =="
  "$UNITY_PATH" -batchmode -nographics -projectPath LingyanUnity \
    -runTests -testPlatform editmode \
    -testResults "$(pwd)/Builds/logs/editmode-results.xml" \
    -logFile "$(pwd)/Builds/logs/editmode.log"
  echo "Unity editmode 测试通过"
else
  echo "（未设 UNITY_PATH，跳过 Unity editmode 测试）"
fi

echo "== 冒烟全过 =="
