# 决策记录

## D1 工作标题：《凌烟》/ Lingyan
凌烟阁为唐代功臣画像所在，"爬向凌烟阁"即本作主题（身份与升迁）。占位性质，
改名只需动 `strings.json` 的 `game.title/subtitle` 与 Steam 物料，代码无硬编码标题。

## D2 Unity 版本：6000.0.76f1（LTS）
写作时 6000.0 LTS 最新为 6000.0.80f1；锚定略旧的 .76f1（2026-05）使多数玩家/CI
环境可无痛向上升级。ProjectVersion.txt 只写版本号，不编造修订哈希。

## D3 场景零 GUID 依赖，UI 全代码构建
Boot.unity 是空场景；相机、EventSystem、画布、全部界面由 `GameBootstrap` →
`GameController` 在运行时构建。理由：场景 YAML 手工维护极易在合并与升级时坏档，
而代码构建可被编译检查与测试覆盖。`.meta` 由 `tools/gen_meta.py` 以
md5(路径) 生成确定性 GUID，`EditorBuildSettings.asset` 里的场景 GUID 因此可预先写死。

## D4 JSON 库：Newtonsoft（com.unity.nuget.newtonsoft-json 3.2.1）
Unity 官方托管包，IL2CPP 兼容性有既往案例；dotnet 侧用同名 NuGet 包（13.0.3），
两边同一 API。System.Text.Json 未随 Unity 发行，不选。

## D5 双语系统自研（JSON 目录），不用 Unity Localization 包
需求是"键 → {zh, en}"加术语表强校验。自研目录可在无 Unity 环境下被 `dotnet test`
逐键校验（双语并齐、术语一致），Unity Localization 的资产表做不到这一点。
缺键返回可见哨兵 `⟦key⟧` 并记录，不静默。

## D6 域模型与引擎分离
`Lingyan.Core`（asmdef `noEngineReferences: true`）承载全部规则：官制、品阶、服色、
考课、名誉、历法、货币、存档。同一份源码被 `core-dotnet/` 镜像编译并测试；
Unity 侧 `Lingyan.Game` 只做呈现与文件 IO。收益：规则可在本仓库任何机器上验证，
不被 Unity 许可阻塞。

## D7 Unity API 桩编译检查
`core-dotnet/Lingyan.UnityStubs` 手写了 Game/Editor/PlayTests 用到的最小 UnityEngine/
TMPro/UnityEditor API 面（签名对齐、空实现），`Lingyan.CompileCheck` 对桩编译全部
Unity 侧源码。它抓拼写、签名、语法层错误；**不等于** Unity 真实导入验证，
后者仍需装编辑器跑（见 ROADMAP 缺口）。桩不发行、不进 Unity 工程。

## D8 历法简化
每月固定 30 日、每年 12 月 360 日，节气每 15 日一换、正月初一起于立春；
不排真实朔闰与置闰。年号更替由剧情脚本驱动（`TangDate.SetEra`），不做年中自动改元
（垂拱四年之后若剧情未改元则显示垂拱五年，与史实改元永昌的时点由剧本负责对齐）。
理由：真实唐历需要历表数据库，对玩法无增益；显示格式（"垂拱四年 三月十七 · 巳时"）
按规格锁定并有测试。

## D9 品阶数据的取舍（规格 vs 史料）
- 规格明文给出的品阶一律照规格（县丞正八品下、监察御史正八品上等），测试锁定。
- 规格未给的补史料值：御史中丞正五品上、中书侍郎正四品上（唐前期）、
  队正正九品下、果毅都尉从五品下（上府）、折冲都尉正四品上（上府）、县尉从九品下（中下县）。
- 迁转序列是"权势/清要"序列而非品阶单调序列——监察御史正八品上而权重于县丞，
  这是唐制本色，四轨分立的意义所在，不做人工"品阶递增"矫正。
- 武周改制的机构改名（中书省→凤阁、门下省→鸾台、同凤阁鸾台平章事等）记录在
  术语表 note 中，按年代显示别名待后续阶段实装。

## D10 考课九等映射采用《唐六典》通行文本
一最四善为上上；一最三善、无最四善为上中；一最二善、无最三善为上下；一最一善、
无最二善为中上；一最、无最一善为中中；善最弗闻为中下；下三等以劣迹论
（爱憎任情→下上，背公向私→下中，贪浊有状→下下）。传世文献各处引文略有出入，
以此为准并在测试中逐行锁定。

## D11 渲染管线：暂用内置管线（Built-in RP）
阶段 0–1 只有 UI。阶段 9（美术）再评估 URP 2D（灯光/宵禁夜景需要）；届时迁移成本
限于渲染设置与材质，UI 与域模型不受影响。

## D12 脚本后端：暂用 Mono
阶段 0 先 Mono 出包（快、无跨平台工具链要求）。Steam 上架前评估切 IL2CPP
（需补 link.xml 保 Newtonsoft 反射）。

## D13 主角默认姓名为占位
沈知白（明镜）、柳七（白身）、陈铁衣（戍卒）、苏青漪（女官）、康悉达（胡商）。
建角界面可改名；内容阶段可整体更换，只动 `ProtagonistCatalog` 与文案。

## D15 好感合成的口径（阶段 3）
总好感 = 基准（NPC 档案）+ 账本累计（互动逐笔，来源与日期入档）+ 动态项（实时算，不入档）。
动态项两类：名誉观感（NPC 身份主导轨 ≥55 加分 / ≤25 减分——官员看官声、百姓看民望、
游侠看江湖，同一份名声方向可相反）与衣着礼数（服色差两档以上失礼）。
悬停浮签与互动屏共用同一渲染器取数，全游戏只有一个好感口径。
互动规则全部纯函数（随机源注入），数值锁在 SocialTests。

## D16 精模资产管线（阶段 2 视觉件）
Blender 脚本（tools/blender/tang_hall.py）既出 Cycles 预览渲染又导 Unity FBX；
比例常数与 TangArchitectureSpec 同源。Unity 侧优先加载精模，缺资产回退程序化并
Debug.LogError 明示。门扇等可动件在 Blender 里把物体原点放在铰线上、以命名节点导出，
Unity 按名字驱动旋转。

## D17 俸禄与宅邸的数值口径（阶段 6）
俸禄三构成（月俸钱/禄米/职田租入）数值为游戏化简表，量级参照《唐会要》禄秩门，
从品九折、米价 200 文/石为游戏常数；精确校数与物价活化排阶段 10。
宅邸四档，乌头门按"五品以上"（含从五品下）设礼制门槛——购置判定先问身份再问钱，
出处以《营缮令》复原条文为准，待复核（诚实台账同术语表策略）。

## D18 战斗内核先行（阶段 7）
判定内核与表现层分离：帧数据（前摇/判定/后摇）、耐力、i-frame、格挡破防、硬直
全部在 Core 以逻辑帧实现并测试锁定；Unity 层只喂输入画状态。
好处：手感数值可以在无引擎环境快速迭代，切磋（阶段 3 的掷骰版）将换用同一内核。

## D14 一处规格外的自我约束
规格第十五节的方法论（"一个诊断，胜过第八个假设"）落为工程习惯：
启动时 `[Smoke]` 日志直接打印运行中的关键值（词条数、术语数、字体是否加载、
存档路径），出问题先看运行时实值，不猜。

## D19 shader 一律放 Resources，运行时 Resources.Load 直取（第十七轮出包教训）
运行时 `Shader.Find` 的 shader（无论内置还是自写）若无资产牵引、不在
GraphicsSettings 的 Always Included 表里，出包即被裁剪——编辑器自带全部内置
shader，问题只在真机 exe 暴露（像素人洋红、水墨后处理直通）。
故：自写 shader 全放 `Assets/Resources/Shaders/`（Resources 随包必含），
代码 `Resources.Load<Shader>` 直取、`Shader.Find` 只作兜底；不依赖内置
Unlit/Transparent，改用自写 `Lingyan/UnlitTransparent`。

## D20 FBX 与 .shader 的 .meta 不入库（有意为之）
`gen_meta.py` 生成确定性 .meta 覆盖脚本/文本/目录；FBX 与 shader 的导入器设置
（ModelImporter/ShaderImporter）字段多且随版本演化，手写易在导入时被 Unity
重写产生噪声 diff。二者均经 `Resources.Load` 按路径引用、无跨资产 GUID 引用，
GUID 每机重生成无碍。若未来出现 GUID 引用（如场景/预制体牵引），再补入库。
