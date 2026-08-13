# 存档格式与版本迁移链

规格第十一节的事故模型（原文）：格式改了而没有迁移，后果不是报错，是玩家的存档
照常打开、继续玩，但钱是 0、官阶不对、查了一半的案子线索没了，全程零提示。
本文件定义的契约就是为了让这件事**在结构上不可能发生**。

## 契约（由 `SaveMigrator` + `SaveValidator` 执行，测试锁定）

1. 存档版本比当前程序新 → `SaveVersionTooNewException`（提示更新游戏）。
2. 版本旧且缺迁移器 → `SaveMigrationMissingException`（程序缺陷，响亮报告）。
3. 迁移后缺关键字段或值非法 → `SaveCorruptException`。
4. **任何情况下不得以默认值静默补齐关键字段。** 关键字段全部标
   `Required.Always/AllowNull`，缺失即在反序列化层拦截；语义校验再查一遍取值
   （主角存在、官职 id 可解析、钱非负、名誉 0–100、日期在历表内）。
5. 未知的多余字段容忍（向前兼容：新版本写的档，旧版本尽量拒绝得体——版本号
   会先触发规则 1）。
6. 读档失败必须把 `ReasonKey`（本地化键）呈现给玩家，绝不静默回主菜单。

## v2 增量（当前版本）

阶段 3 引入两个字段（`V1ToV2` 迁移器补空结构与零值——这两个字段在 v1 时代不存在，
语义正确的默认不属于"静默补齐关键进度"）：

```json
{
  "schemaVersion": 2,
  "wantedLevel": 0,
  "npcStates": {
    "huan_fuzi": {
      "met": true,
      "lastGreetDay": "chuigong:4:3:17",
      "ledger": [ { "delta": 8, "source": "affinity.src.gift_liked", "param": "gift.wenxuan", "date": "chuigong:4:3:17:5" } ],
      "flags": [ "insulted_proud_zheng_wu" ]
    }
  }
}
```

- `wantedLevel`：通缉值（偷窃败露等累积，后续武侯缉拿玩法的依据）。
- `npcStates`：每名 NPC 的好感账本（来源键 + 参数 + 日期戳）、相识标记、
  每日寒暄限流戳、互动旗标（隐藏支线钩子）。
- 迁移测试锁定：真实 v1 档升 v2 后钱/官身/剧情旗标逐项不丢；v0 档沿链升到当前版。

## v1 结构（历史）

```json
{
  "schemaVersion": 1,
  "createdUtc": "2026-08-12T00:00:00Z",
  "protagonist": "mingjing",
  "name": "沈知白",
  "entryPath": null,
  "attributes": { "stamina": 6, "health": 6, "strength": 4, "wisdom": 12 },
  "offices": { "zhishi": null, "sanguan": null, "xunZhuan": 0, "jue": null },
  "reputation": { "guansheng": 15, "minwang": 20, "jianghu": 5 },
  "reputationLedger": [ { "track": "...", "delta": 2, "source": "键", "date": "chuigong:4:3:17:5" } ],
  "moneyWen": 6000,
  "date": { "era": "chuigong", "eraYear": 4, "month": 3, "day": 17, "hourIndex": 5 },
  "storyFlags": {},
  "counters": {}
}
```

- `offices` 是四轨并行身份（职事/散官/勋转数/爵），null 表示该轨未获得。
- `storyFlags` / `counters` 为阶段 4（对话分支持久化）预留的落点，从 v1 起就在，
  避免后续加字段又要迁移。
- 文件位置：`Application.persistentDataPath/saves/slot_1.json`；写档先落 `.tmp`
  再原子替换，旧档滚为 `.bak`。

## 迁移链

- 迁移器实现 `ISaveMigration { FromVersion, Apply(JObject) }`，注册于
  `SaveMigrator.CreateDefault()`，一次升一个版本号，链式推进到当前版。
- 迁移器**只转换确实存在的字段**；源头缺失的关键字段保持缺失，让 Required 校验
  响亮失败——迁移器自己不许造数据。
- 现存迁移：`V0ToV1`（早期开发格式：money_guan 浮点贯 → moneyWen 整数文，
  拼音属性键改名，补 offices/ledger/flags 空结构）。
- 每加一个版本，必须同时交付：迁移器 + 新旧档往返测试 + "缺字段响亮失败"测试。
  参照 `SaveMigrationTests`（含 3.42 贯 → 3420 文一文不丢、缺钱字段必须抛异常等）。

## 存档策略（后续阶段）

自动存档 + 章节存档（规格第十一节）在阶段 4–5 随剧情节点实装；本阶段先交付
手动档 + 写档备份 + 迁移链地基。
