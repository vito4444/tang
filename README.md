# 凌烟（工作标题）

唐朝背景单机 RPG。核心不是战斗，是**身份**——官阶、名声、别人怎么看你。
时代锚点：唐高宗后期至武周（约公元 660–705 年）。

- 引擎：Unity 6000.0.76f1+（LTS）
- 平台：Windows（Steam 发售目标）
- 语言：简体中文 / English 双语
- 状态：规格阶段 0–6 完成，阶段 7 首版完成，阶段 8 进行中——
  四轨官制、Blender 精模坊景与作息世界、好感互动、对话分支、丝帛案双结局闭环、
  俸禄宅邸、切磋对决、存档 v3 迁移链与自动存档；引擎内截图见 PR #1（八轮实机验证）

## 快速上手

```bash
# 冒烟（无需 Unity）：域模型契约测试 + Unity 侧编译检查 + 端到端演示 + meta 完整性
./tools/smoke.sh

# 数据层端到端演示（建角 → 出档 → 读档 → 旧档迁移 → 坏档拒绝 → 考课迁转）
dotnet run --project core-dotnet/Lingyan.Demo

# Windows 出包（需装 Unity 6000.0.76f1+ 与已激活许可）
UNITY_PATH=/path/to/Unity ./tools/build_windows.sh     # macOS / Linux
# 或 Windows PowerShell：
#   $env:UNITY_PATH="C:\Program Files\Unity\Hub\Editor\6000.0.76f1\Editor\Unity.exe"
#   .\tools\build_windows.ps1
```

用 Unity Hub 打开 `LingyanUnity/` 即可进编辑器；打开 `Assets/Scenes/Boot.unity`
点 Play，主菜单由代码自举构建（场景本身是空的，不携带任何 GUID 引用）。

## 仓库结构

```
LingyanUnity/            Unity 工程
  Assets/Scripts/Core/     纯 C# 域模型（无引擎依赖，asmdef noEngineReferences）
  Assets/Scripts/Game/     Unity 界面与服务层（主菜单/建角/书房/设置，全代码构建 UI）
  Assets/Scripts/Editor/   一键构建脚本（BuildScript）
  Assets/Scripts/Tests/    单元测试（与 dotnet 侧共享同一份源码）+ 播放模式冒烟
  Assets/Resources/Data/   strings.json（双语目录）、glossary.json（Hucker 术语表）
  Assets/Resources/Fonts/  霞鹜文楷 + OFL 授权文本（见 docs/FONTS.md）
core-dotnet/             无 Unity 环境下的测试与验证工程（编译同一份源码）
  Lingyan.Core.Dotnet/     Core 源码镜像编译
  Lingyan.Core.Tests.Dotnet/ NUnit 契约测试（dotnet test 可跑）
  Lingyan.UnityStubs/      UnityEngine/TMPro/UnityEditor API 桩，仅供编译检查，不发行
  Lingyan.CompileCheck/    Game/Editor/PlayTests 源码对桩编译，抓拼写与签名错误
  Lingyan.Demo/            数据层端到端控制台演示
tools/                   smoke.sh（一条命令冒烟）、build_windows.sh/.ps1、gen_meta.py
docs/                    路线图、决策记录、术语政策、存档格式、字体登记、构建说明
```

## 已实现的系统（数据层）

- **四轨官制**：职事官 / 散官 / 勋官 / 爵四条独立轨道；文官线 12 阶、武官线 10 阶
  迁转序列；文武散官各 29 阶；勋官十二转；唐爵九等。
- **品阶**：九品三十阶（正/从 × 上/下），比较、汉字与学界缩写（4a1）双格式。
- **服色**：三品以上紫、四五品绯、六七品绿、八九品青、无品白衣，由散官品阶推导。
- **考课**：四善（德义有闻/清慎明著/公平可称/恪勤匪懈）+ 最 + 劣迹 → 《唐六典》九等
  映射；四年一任、四考皆及格且有中上方许迁转，上上特旨立迁。
- **三轨名誉**：官声 / 民望 / 江湖名望，带来源与时间的账本；同一份名声在官员、
  百姓、游侠眼中按权重读出不同观感。
- **五位主角**：明镜 / 白身 / 戍卒 / 女官 / 胡商；四维（体力/生命/力量/智慧）全示，
  仅白身可自由分配（基础 4×4 + 12 点池，上限 12）；白身入仕二选一（明经/进士/投军）。
- **唐历**：年号纪年（显庆至神龙 33 个年号）+ 十二时辰 + 二十四节气；
  显示"垂拱四年 三月十七 · 巳时"。
- **货币**：贯/文（1 贯 = 1000 文），显示"3 贯 420 文"。
- **存档**：schema v1 + 版本迁移链 + 严格校验。关键字段缺失一律响亮失败，
  绝不静默补零（见 docs/SAVE_FORMAT.md）。
- **术语表**：官职英译锁定 Hucker《A Dictionary of Official Titles in Imperial
  China》(1985)，校验器强制全局统一（见 docs/GLOSSARY.md）。

## 测试

```bash
dotnet test core-dotnet/Lingyan.sln    # 158 项契约测试
```

同一份测试源码挂在 Unity Test Runner（editmode）下，装好 Unity 后可在编辑器里复跑。

## 版权与授权

- 中文字体：霞鹜文楷（LXGW WenKai）v1.522，SIL OFL 1.1，商用安全，登记于 docs/FONTS.md。
- 本仓库游戏代码与文本版权归项目所有者，未附开源许可证。
