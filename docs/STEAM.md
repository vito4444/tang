# Steam 上架材料与流程清单（阶段 11）

注册、付费与页面提交须由项目所有者在 [Steamworks](https://partner.steamgames.com/)
操作，本文件把能预先备好的全部备好：商店文案（双语）、规格参数、素材尺寸清单、
流程时间线。文案与游戏内术语同源（Hucker 锁定英译），改动请连同 `strings.json`
一起校。

## 时间线（硬性等待，规格原文要求先记）

1. Steam Direct 注册费 **$100**（每款游戏一次，收入超 $1,000 可退）。
2. 付费后有 **30 天硬性等待期**方可发布首个商店页。
3. Coming Soon 页面须公开 **至少 2 周**才能发售。
4. 结论：**建议立即注册**——等待期与开发并行，不占开发时间。

## 商店文案 · 简体中文

**一句话（Short Description，≤ 300 字符）**

> 唐朝背景单机 RPG。你的武器不是刀剑，是身份——官品、名声、别人怎么看你。
> 断案、投书、议亲、赴市、防秋；九品三十阶，一步一个脚印爬向凌烟阁。

**详细描述（About This Game）**

> **身份即命运。**
>
> 垂拱四年，武后临朝。你是县衙小吏、布衣、戍卒、宫人，或西市胡商——
> 五条入仕之路，各有各的门槛与天花板：小吏靠断案铨选，布衣应举或投军，
> 戍卒以军功叙迁，宫人待内廷之诏，胡商有钱也买不动市籍之限。
>
> - **四轨官制**：职事官、散官、勋官、爵四条轨道并行，九品三十阶，
>   服色随品阶改换（三品紫、五品绯、六七品绿、八九品青）。
> - **考课迁转**：四善二十七最，岁末赴考定九等；连课殿等或通缉滔天则贬谪
>   岭南——贬谪不是终局，岁课优异可遇赦量移。
> - **断案**：线索板、组合推论、证人门槛；严刑立结但可能屈打成冤，
>   详审费时却民心所向。冤案会永远钉在你的循吏传门槛上——除非诣铜匦申冤昭雪。
> - **坊制长安**：坊墙坊门、暮鼓晨钟、宵禁犯夜笞二十；市集午时击鼓开市、
>   日入前散（《唐六典》市制）。
> - **人情世故**：好感账本笔笔可回溯；送礼要投其所好，雇佣保跑腿打听，
>   议亲过六礼——门第、聘财、婚约，缺一不许。
> - **多结局**：紫袍金鱼、青云直上、循吏传、江湖有名、岭南瘴雨、薄宦萧然、
>   布衣终老——挂冠致仕之日，生涯回顾卷轴为你收束此生。
> - **考据向**：官职英译锁定 Hucker《中国古代官名辞典》；建筑鸱尾而非鸱吻、
>   斗拱带下昂、举折平缓出檐深远；游戏内典籍随见闻解锁词条。
>
> 战斗存在，但只是解决问题的手段之一——切磋点到即止，胜负都涨阅历。

## Store Copy · English

**Short Description (≤ 300 chars)**

> A single-player RPG of Tang-dynasty China. Your weapon is not the sword but
> standing: office, rank, reputation. Judge cases, petition the throne, wed by
> the Six Rites, ride the autumn muster—climb all thirty grades toward the
> Lingyan Pavilion.

**About This Game**

> **Identity is destiny.**
>
> 688 CE. Empress Wu holds court. You are a county clerk, a commoner, a
> garrison soldier, a palace woman, or a Sogdian merchant of the West Market—
> five roads into officialdom, each with its own gate and its own ceiling.
>
> - **Four parallel tracks of office**: substantive posts, prestige titles,
>   merit ranks, and nobility—nine ranks in thirty grades, robe colors
>   changing with your station.
> - **Annual evaluations**: the Four Virtues and Twenty-Seven Excellences
>   grade your year on a nine-step scale. Fail twice and you ride south in
>   demotion—which is not game over: excel in exile and an amnesty may move
>   you back.
> - **Casework**: a clue board, deductions from paired evidence, witnesses
>   who must be persuaded. Forced confessions close cases fast—and forge
>   wrongful convictions that bar you from the chapter of good officials,
>   unless you petition the bronze box and right your own wrong.
> - **A living ward of Chang'an**: walls and gates, dusk drums and dawn
>   bells, a curfew that flogs; a market that opens at noon drums and
>   scatters before sunset, as the Six Statutes of the Tang prescribe.
> - **People, remembered**: an itemized ledger of regard; gifts that must
>   suit tastes; a hired hand to run your errands; marriage by the Six Rites.
> - **Many endings**: the purple robe, the swift ascent, the good official's
>   chapter, a name on the rivers and lakes, the miasma rains of Lingnan, a
>   petty post, a hempen old age.
> - **Historically grounded**: official titles locked to Hucker's Dictionary
>   of Official Titles in Imperial China; Tang roofs with owl's-tail finials
>   and true bracket sets; an in-game codex that unlocks as you encounter
>   each term.
>
> Combat exists—but it is one tool among many, and never the point.

## 分类与标签建议

- Genre: RPG / Simulation
- Tags: RPG, Historical, Simulation, Detective, Choices Matter, Story Rich,
  Multiple Endings, Singleplayer, Pixel Graphics（人物）, Ancient China
- 分级：无血腥裸露；含酒精引用（宴饮）、司法暴力文字描述（笞刑、严刑）——
  填写内容问卷时如实勾选。

## 系统需求（据当前构建实测口径）

|  | 最低 | 推荐 |
|---|---|---|
| OS | Windows 10 64-bit | Windows 10/11 64-bit |
| CPU | 双核 2.0 GHz | 四核 2.5 GHz |
| 内存 | 4 GB | 8 GB |
| 显卡 | DX11 兼容、1 GB 显存 | DX11/DX12、2 GB 显存 |
| 存储 | 500 MB（当前包 122 MB，留内容增长余量） | 1 GB |

## 素材尺寸清单（Steamworks 2026 现行规格，提交前再核）

| 素材 | 尺寸 | 备注 |
|---|---|---|
| 胶囊·小 | 462×174 | 列表页 |
| 胶囊·主 | 616×353 | 商店主图 |
| 胶囊·竖版 | 374×448 | 首页推荐位 |
| 页头 | 460×215 | 库存/愿望单 |
| 库·封面 | 600×900 | 玩家库竖图 |
| 库·横幅 | 3840×1240 | 库详情页 |
| 截图 | ≥1280×720，16:9 | `artifacts/unity-shots/` 20 张可直接选用（1920×1080） |
| 预告片 | 1080p，≤2 分钟 | 建议录：坊景昼夜循环→断案板→对决→结局卷轴 |

美术方向建议：水墨底 + 像素人主视觉，与游戏画风一致；标题字用霞鹜文楷
（OFL 商用安全，见 docs/FONTS.md）。

## 构建上传

- 产物：`tools/build_windows.sh` → `Builds/Win64/Lingyan/`（exe + Data，122 MB）。
- 上传：Steamworks 后台 SteamPipe（`steamcmd` + app_build 脚本），depot 就一个
  （Windows x64）。首次配置时把 `Builds/Win64/Lingyan/*` 整目录作 content root。
- Steam Deck：Proton 兼容性未验证——上架后跑一次 Deck Verified 流程再标。

## 提交前自检清单

- [ ] Steamworks 注册 + $100（立即做，等待期与开发并行）
- [ ] 商店页文案（本文件双语稿直接粘贴）
- [ ] 素材图 8 件（上表尺寸）
- [ ] 内容问卷（司法暴力文字/酒精引用如实勾选）
- [ ] Coming Soon 页公开 ≥ 2 周
- [ ] 构建经 SteamPipe 上传并在内测分支启动验证（真 Windows 机）
- [ ] 页面双语（中文简体 + English）本地化字段填齐
