using TMPro;
using UnityEngine;

namespace Lingyan.Game.Services
{
    /// <summary>
    /// 从随包 TTF（霞鹜文楷，OFL 1.1）动态生成 TMP 字体资产。
    /// 动态铺字保证 CJK 全覆盖，不预烘全量图集。
    /// </summary>
    public sealed class FontService
    {
        public const string FontResourcePath = "Fonts/LXGWWenKai-Regular";

        public TMP_FontAsset Main { get; private set; }

        public void Load()
        {
            Font source = Resources.Load<Font>(FontResourcePath);
            if (source == null)
            {
                Debug.LogError("[Lingyan] 字体缺失: Resources/" + FontResourcePath
                    + "，回落 TMP 默认字体（中文将不可读）");
                return;
            }
            Main = TMP_FontAsset.CreateFontAsset(source);
            if (Main == null)
            {
                Debug.LogError("[Lingyan] TMP 字体资产创建失败");
            }
        }

        public void Apply(TMP_Text text)
        {
            if (Main != null)
            {
                text.font = Main;
            }
        }
    }
}
