# 术语政策（官职英译锁定 Hucker 1985）

规格第二节红线：官职英译锁定 Charles O. Hucker,
*A Dictionary of Official Titles in Imperial China* (Stanford University Press, 1985)，
做成术语表，写脚本强制校验，全局统一。

## 单一来源

- 数据文件：`LingyanUnity/Assets/Resources/Data/glossary.json`（本仓库唯一术语来源）。
- 职事官（`OfficeDef`）与封爵（`JueDef`）**不存英文名**，只存 `GlossaryId`，
  英文一律经术语表解析——从构造上杜绝私译。
- 文武散官与勋官数量大（29+29+12），表内自带英文字段：名号显赫者用 Hucker 译名
  （光禄大夫、上柱国等），其余以拼音转写（Title Case）兜底，待复核后逐个替换。
  拼音转写本身是 Hucker 词典对次要名号的通行处理方式。

## 强制校验（跑在每次冒烟与 dotnet test 里）

`GlossaryValidator` 三条规则，违规即测试红：

1. **解析规则**：所有职事官、封爵定义必须能在术语表解析出英译，且中文名一致。
2. **一致性规则**：本地化目录任何 zh 文案若包含术语表中的官职中文名，对应 en 文案
   必须包含锁定英译（大小写不敏感）。命中按最长词条优先，被长词条覆盖的短词条
   不误报（如「开国郡公」内含「国公」）。
3. **查重规则**：office 类目内英译唯一，一名两译或两名一译都报错。

校验器自身有"变异验证"测试：故意塞入一条私译（County Sheriff），断言校验器必须咬住。

## 复核台账（verified 字段）

每条官职词条带 `verified` 布尔：

- `true`：已确认与 Hucker 原书逐字一致（监察御史 Investigating Censor、
  侍御史 Attendant Censor、御史中丞 Vice Censor-in-chief、大理寺卿/少卿、
  县令 District Magistrate、同中书门下平章事 Manager of Affairs with the
  Secretariat-Chancellery、进士 Presented Scholar、明经 Classicist 等）。
- `false`：按 Hucker 构词法与学界通行引用拟定，**待对照纸本原书逐字复核**
  （县尉 District Defender、折冲都尉 Assault-resisting Commandant、
  中郎将 Leader of Court Gentlemen 等）。测试保证 verified 台账可枚举，
  不许把未核词条藏起来。

复核流程：取得 Hucker (1985) 原书 → 按词条号逐字核对 → 改 `verified: true` 并在
`note` 里记词条号 → 提交。改动英译会自然触发一致性校验，散落文案里的旧译一起暴露。

## 非官职词条

考试（帖经/墨义/策问）、礼制（六礼各仪）、建筑（鸱尾/下昂/斗拱）、制度（宵禁/坊/犯夜）
等不属 Hucker 管辖，用学界通行译法，同样进术语表吃一致性校验（category 区分）。
这批词条同时是后续 Codex 百科（阶段 10）的词条底稿。

## 品阶备注

品阶英文用学界缩写惯例：正=a、从=b、上=1、下=2，如正四品上 = rank 4a1。
规格与史料在个别品阶上的取舍见 docs/DECISIONS.md D9。
