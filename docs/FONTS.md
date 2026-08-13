# 字体登记（规格第十一节：所有字体登记在案）

| 字体 | 版本 | 文件 | 授权 | 用途 | 来源 |
|---|---|---|---|---|---|
| 霞鹜文楷 LXGW WenKai Regular | v1.522 | `LingyanUnity/Assets/Resources/Fonts/LXGWWenKai-Regular.ttf`（25,575,676 字节） | SIL OFL 1.1（商用安全） | 全部界面中英文（TMP 运行时动态字集） | github.com/lxgw/LxgwWenKai release v1.522 |

- 授权全文随字体同目录发布：`Assets/Resources/Fonts/OFL.txt`
  （含保留字体名 '霞鹜'、'LXGW' 等条款）。主菜单页脚常驻授权署名。
- **禁令**：绝不使用微软雅黑（无商用授权）。此禁令由自动化测试
  `FontBan_NoUnlicensedFontAnywhere` 扫描 `Assets/` 全部文本文件强制执行。
- 字体经 `FontService` 在运行时用 `TMP_FontAsset.CreateFontAsset` 动态生成图集，
  CJK 按需铺字，不预烘全量图集。
- 后续若为正文/UI 增补 Noto Sans SC（OFL）或标题书法字重（LXGW WenKai Medium），
  一律先登记本表再入库。
