using System.Collections.Generic;
using UnityEngine;

namespace Lingyan.Game.World3D
{
    /// <summary>
    /// 唐构用色（红线："色调简洁：土红 / 白墙 / 灰瓦"，禁明清富丽彩画）。
    /// 数值为占位设计值，阶段 9 对照佛光寺、南禅寺照片再校。
    /// 材质按色缓存复用，整场景十余个材质即可。
    /// </summary>
    public static class TangColors
    {
        /// <summary>柱、枋、斗拱：土红（丹粉刷饰）。</summary>
        public static readonly Color Timber = new Color(0.60f, 0.30f, 0.22f);

        /// <summary>斗拱暗部/下昂：深土红。</summary>
        public static readonly Color TimberDark = new Color(0.48f, 0.24f, 0.18f);

        /// <summary>白墙（白灰抹面）。</summary>
        public static readonly Color Wall = new Color(0.88f, 0.85f, 0.78f);

        /// <summary>灰瓦。</summary>
        public static readonly Color Tile = new Color(0.38f, 0.39f, 0.41f);

        /// <summary>正脊、鸱尾：深灰。</summary>
        public static readonly Color Ridge = new Color(0.27f, 0.28f, 0.30f);

        /// <summary>台基、井圈：石灰岩。</summary>
        public static readonly Color Stone = new Color(0.60f, 0.58f, 0.53f);

        /// <summary>坊墙、门墩：夯土。</summary>
        public static readonly Color RammedEarth = new Color(0.66f, 0.58f, 0.46f);

        /// <summary>地面黄土。</summary>
        public static readonly Color Ground = new Color(0.55f, 0.49f, 0.39f);

        /// <summary>板门、直棂窗棂条：木本色偏深。</summary>
        public static readonly Color Door = new Color(0.36f, 0.27f, 0.20f);

        /// <summary>槐树冠。</summary>
        public static readonly Color Foliage = new Color(0.36f, 0.46f, 0.28f);

        /// <summary>树干。</summary>
        public static readonly Color Trunk = new Color(0.40f, 0.33f, 0.26f);

        private static readonly Dictionary<Color, Material> Cache =
            new Dictionary<Color, Material>();

        /// <summary>取（并缓存）纯色 Standard 材质。</summary>
        public static Material Mat(Color color)
        {
            if (Cache.TryGetValue(color, out Material cached) && cached != null)
            {
                return cached;
            }
            var material = new Material(Shader.Find("Standard"));
            material.color = color;
            material.SetFloat("_Glossiness", 0.08f);
            Cache[color] = material;
            return material;
        }
    }
}
