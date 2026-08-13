using System.Collections.Generic;
using UnityEngine;

namespace Lingyan.Game.World3D
{
    /// <summary>
    /// 程序化像素唐人（阶段 9：人物像素风、背景写实的画面方向）。
    /// 点阵以字符画定义：幞头、圆领袍、革带、乌皮靴；袍色按身份传入。
    /// 输出竖立公告牌（billboard）+ 脚底假影，替换先前的胶囊立标。
    /// </summary>
    public static class PixelPerson
    {
        // 16×24 点阵。字符：. 透明  H 幞头  F 面  R 袍(参数色)  D 袍暗部
        // B 革带  S 靴  N 手
        private static readonly string[] Pattern =
        {
            "................",
            ".....HHHH.......",
            "....HHHHHH......",
            "....HHHHHHH.....",
            "....HFFFFH......",
            "....HFFFFH......",
            ".....FFFF.......",
            "....RRRRRR......",
            "...RRRRRRRR.....",
            "..RRRRRRRRRR....",
            "..RRDRRRRDRR....",
            ".NRRDRRRRDRRN...",
            ".NRRRRRRRRRRN...",
            "..RRBBBBBBRR....",
            "..RRRRRRRRRR....",
            "..RDRRRRRRDR....",
            "..RDRRRRRRDR....",
            "..RDRRRRRRDR....",
            "...DRRRRRRD.....",
            "...DRRRRRRD.....",
            "...RRR..RRR.....",
            "...RRR..RRR.....",
            "...SSS..SSS.....",
            "..SSSS..SSSS....",
        };

        private static readonly Dictionary<Color, Texture2D> Cache =
            new Dictionary<Color, Texture2D>();

        private static Texture2D Texture(Color robe)
        {
            if (Cache.TryGetValue(robe, out Texture2D cached) && cached != null)
            {
                return cached;
            }
            int width = Pattern[0].Length;
            int height = Pattern.Length;
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Point; // 像素风的关键：点采样

            Color robeDark = new Color(robe.r * 0.72f, robe.g * 0.72f, robe.b * 0.72f);
            Color head = new Color(0.10f, 0.09f, 0.09f);
            Color face = new Color(0.86f, 0.72f, 0.58f);
            Color belt = new Color(0.28f, 0.20f, 0.13f);
            Color boot = new Color(0.14f, 0.12f, 0.11f);
            Color clear = new Color(0f, 0f, 0f, 0f);

            for (int y = 0; y < height; y++)
            {
                string row = Pattern[height - 1 - y]; // 纹理自下而上
                for (int x = 0; x < width; x++)
                {
                    Color c;
                    switch (row[x])
                    {
                        case 'H': c = head; break;
                        case 'F': c = face; break;
                        case 'R': c = robe; break;
                        case 'D': c = robeDark; break;
                        case 'B': c = belt; break;
                        case 'S': c = boot; break;
                        case 'N': c = face; break;
                        default: c = clear; break;
                    }
                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply();
            Cache[robe] = tex;
            return tex;
        }

        /// <summary>立一个像素人：billboard 面片 + 脚底假影。高约 1.75m。</summary>
        public static GameObject Build(Transform parent, string name, Color robe)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);

            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "Sprite";
            quad.transform.SetParent(root.transform, false);
            quad.transform.localPosition = new Vector3(0f, 0.95f, 0f);
            quad.transform.localScale = new Vector3(1.17f, 1.75f, 1f);

            var material = new Material(Shader.Find("Unlit/Transparent"));
            material.mainTexture = Texture(robe);
            var renderer = quad.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            quad.AddComponent<BillboardSprite>();

            // 脚底假影（unlit 面片不投影，给一枚椭圆影贴地）
            GameObject shadow = GameObject.CreatePrimitive(PrimitiveType.Quad);
            shadow.name = "Shadow";
            shadow.transform.SetParent(root.transform, false);
            shadow.transform.localPosition = new Vector3(0f, 0.02f, 0f);
            shadow.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            shadow.transform.localScale = new Vector3(0.9f, 0.5f, 1f);
            var shadowMaterial = new Material(Shader.Find("Unlit/Transparent"));
            shadowMaterial.mainTexture = ShadowTexture();
            shadow.GetComponent<MeshRenderer>().sharedMaterial = shadowMaterial;

            return root;
        }

        private static Texture2D _shadowTex;

        private static Texture2D ShadowTexture()
        {
            if (_shadowTex != null) { return _shadowTex; }
            const int size = 32;
            _shadowTex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            _shadowTex.wrapMode = TextureWrapMode.Clamp;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - size / 2f) / (size / 2f);
                    float dy = (y - size / 2f) / (size / 2f);
                    float d = dx * dx + dy * dy;
                    float alpha = d < 1f ? 0.34f * (1f - d) : 0f;
                    _shadowTex.SetPixel(x, y, new Color(0f, 0f, 0f, alpha));
                }
            }
            _shadowTex.Apply();
            return _shadowTex;
        }
    }

    /// <summary>公告牌：绕 Y 轴面向相机（像素人贴片不倒伏）。</summary>
    public sealed class BillboardSprite : MonoBehaviour
    {
        private void LateUpdate()
        {
            Camera cam = Camera.main;
            if (cam == null) { return; }
            Vector3 toCam = cam.transform.position - transform.position;
            float yaw = Mathf.Atan2(toCam.x, toCam.z) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        }
    }
}
