# 构建与验证

## 一条命令冒烟（无需 Unity）

```bash
./tools/smoke.sh
```

依次执行：

1. `dotnet test core-dotnet/Lingyan.sln` — 101 项契约测试（官制/考课/历法/货币/
   名誉/建角/双语并齐/术语一致/存档迁移链/设置）。
2. `dotnet build core-dotnet/Lingyan.CompileCheck` — Unity 侧全部源码对 API 桩编译，
   抓拼写与签名错误。
3. `dotnet run --project core-dotnet/Lingyan.Demo` — 数据层端到端演示
   （建角 → 出档 → 读档 → v0 迁移 → 坏档拒绝 → 考课迁转）。
4. `python3 tools/gen_meta.py --check` — `.meta` 完整性。
5. 若设置了 `UNITY_PATH`，追加 Unity editmode 测试（同一份测试源码在编辑器内复跑）。

## Windows 出包

前置：Unity 6000.0.76f1+，装 Windows Build Support（Mono），许可已激活。

```bash
UNITY_PATH=/path/to/Unity ./tools/build_windows.sh
# 或 Windows：.\tools\build_windows.ps1
```

- 构建入口：`Lingyan.EditorTools.BuildScript.BuildWindows`（批处理，失败退出码非零）。
- 编辑器内菜单：凌烟 → Build Windows x64。
- 输出：`Builds/Win64/Lingyan/Lingyan.exe`；日志 `Builds/logs/build_win64.log`。
- PlayerSettings（公司名/产品名/版本/关 Unity 启动画面/1920×1080）由构建脚本统一写入，
  不依赖手工点选。

## 界面预览渲染器（自动化验收截图工具的雏形）

```bash
python3 tools/render_mockups.py
```

输出 `artifacts/screenshots/*.png`（主菜单中英、建角、书房、设置）与 `metrics.json`，
终端打印每屏明度 P5/中位/P95 与饱和度均值——规格第十四节要求的"可比较的数字"，
调色改动前后跑两次即可对比，不靠眼睛记忆。

定位说明：文案、术语、色板、字体、布局比例与游戏同源
（strings.json / glossary.json / InkPalette 同值、霞鹜文楷同一份 TTF），
但光栅化走 FreeType，与 Unity TMP 的 SDF 渲染存在字形微差；
背景生成算法（渐变 + 三层远山 + 纸纹 + 晕影）与 `UiKit.InkSprite()` 同构同参。
引擎内截图在 Unity 许可就位后以同一指标口径复核。

## Unity 内测试

- editmode：Window → General → Test Runner → EditMode，`Lingyan.Core.Tests`
  程序集（与 dotnet 侧同一份源码，由 `TestData` 探测数据目录）。
- playmode：`Lingyan.PlayTests.PlaySmokeTests.Boot_BringsUpMainMenu` —
  Boot 场景自举出主菜单，断言标题可见、按钮可用（断言契约，不断言实现细节）。

## 云端/CI 的 Unity 许可

本仓库的云代理环境无 Unity 许可，故阶段 0 的"首次出包"由上述脚本承载、
待有许可的环境执行。要让云代理直接出包验证，在 Cursor Dashboard
（Cloud Agents → Secrets）加：

- `UNITY_LICENSE` = Unity 个人版 .ulf 文件全文；或
- `UNITY_EMAIL` + `UNITY_PASSWORD`（必要时加 `UNITY_SERIAL`）。

配好后可让代理走 GameCI 流程（下载对应版本编辑器 → 激活 → `tools/smoke.sh` 带
editmode → `tools/build_windows.sh`）。
