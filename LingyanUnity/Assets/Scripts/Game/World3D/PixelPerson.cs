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
        // 上身 18 行共用，腿脚 6 行分立姿与跨步两帧（阶段 9：行走动画帧）。
        private static readonly string[] Torso =
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
        };

        private static readonly string[] LegsIdle =
        {
            "...DRRRRRRD.....",
            "...DRRRRRRD.....",
            "...RRR..RRR.....",
            "...RRR..RRR.....",
            "...SSS..SSS.....",
            "..SSSS..SSSS....",
        };

        /// <summary>跨步帧：双腿前后分开，袍摆略张。</summary>
        private static readonly string[] LegsStride =
        {
            "...DRRRRRRD.....",
            "..DRRRRRRRRD....",
            "..RRR....RRR....",
            ".RRR......RRR...",
            ".SSS......SSS...",
            "SSSS......SSSS..",
        };

        /// <summary>0 = 立姿（也是行走帧 B），1 = 跨步帧。</summary>
        public const int FrameIdle = 0;
        public const int FrameStride = 1;

        private static readonly Dictionary<(Color, int), Texture2D> Cache =
            new Dictionary<(Color, int), Texture2D>();

        private static string[] Pattern(int frame)
        {
            string[] legs = frame == FrameStride ? LegsStride : LegsIdle;
            var rows = new string[Torso.Length + legs.Length];
            Torso.CopyTo(rows, 0);
            legs.CopyTo(rows, Torso.Length);
            return rows;
        }

        internal static Texture2D Texture(Color robe, int frame)
        {
            if (Cache.TryGetValue((robe, frame), out Texture2D cached) && cached != null)
            {
                return cached;
            }
            string[] rows = Pattern(frame);
            int width = rows[0].Length;
            int height = rows.Length;
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
                string row = rows[height - 1 - y]; // 纹理自下而上
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
            Cache[(robe, frame)] = tex;
            return tex;
        }

        /// <summary>
        /// 贴片 shader：自带的 Lingyan/UnlitTransparent（在 Resources 随包必含）。
        /// 内置 Unlit/Transparent 仅运行时 Find 会被玩家包裁剪（第十七轮出包核验），
        /// 编辑器里两者皆在，兜底只为万一。
        /// </summary>
        private static Shader SpriteShader()
        {
            Shader shader = Resources.Load<Shader>("Shaders/UnlitTransparent");
            if (shader != null) { return shader; }
            Debug.LogError("[Lingyan] UnlitTransparent shader 缺失，回退内置 Unlit/Transparent");
            return Shader.Find("Unlit/Transparent");
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

            var material = new Material(SpriteShader());
            material.mainTexture = Texture(robe, FrameIdle);
            var renderer = quad.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            quad.AddComponent<BillboardSprite>();

            // 行走组件：时辰变更时从旧位走到新位，步态两帧交替
            var walker = root.AddComponent<PixelWalker>();
            walker.Bind(material, Texture(robe, FrameIdle), Texture(robe, FrameStride));

            // 脚底假影（unlit 面片不投影，给一枚椭圆影贴地）
            GameObject shadow = GameObject.CreatePrimitive(PrimitiveType.Quad);
            shadow.name = "Shadow";
            shadow.transform.SetParent(root.transform, false);
            shadow.transform.localPosition = new Vector3(0f, 0.02f, 0f);
            shadow.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            shadow.transform.localScale = new Vector3(0.9f, 0.5f, 1f);
            var shadowMaterial = new Material(SpriteShader());
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

    /// <summary>
    /// 像素人行走：MoveTo 后沿直线走向目标（约 1.7 m/s），
    /// 步态帧每 0.18s 交替（跨步/立姿），到位即收步回立姿。
    /// Snap 用于首次落位（开场不该看到全坊人齐步走）。
    /// </summary>
    public sealed class PixelWalker : MonoBehaviour
    {
        private const float Speed = 1.7f;
        private const float FrameSeconds = 0.18f;

        private Material _material;
        private Texture2D _idle;
        private Texture2D _stride;
        private Vector3 _target;
        private bool _moving;
        private float _frameClock;
        private bool _strideUp;

        public void Bind(Material material, Texture2D idle, Texture2D stride)
        {
            _material = material;
            _idle = idle;
            _stride = stride;
        }

        public void Snap(Vector3 localPosition)
        {
            transform.localPosition = localPosition;
            _target = localPosition;
            StopWalking();
        }

        public void MoveTo(Vector3 localPosition)
        {
            _target = localPosition;
            if ((transform.localPosition - _target).sqrMagnitude < 0.01f)
            {
                StopWalking();
                return;
            }
            _moving = true;
        }

        private void Update()
        {
            if (!_moving) { return; }

            Vector3 current = transform.localPosition;
            Vector3 delta = _target - current;
            float step = Speed * Time.deltaTime;
            if (delta.magnitude <= step)
            {
                Snap(_target);
                return;
            }
            transform.localPosition = current + delta.normalized * step;

            _frameClock += Time.deltaTime;
            if (_frameClock >= FrameSeconds)
            {
                _frameClock = 0f;
                _strideUp = !_strideUp;
                if (_material != null)
                {
                    _material.mainTexture = _strideUp ? _stride : _idle;
                }
            }
        }

        private void StopWalking()
        {
            _moving = false;
            _frameClock = 0f;
            _strideUp = false;
            if (_material != null)
            {
                _material.mainTexture = _idle;
            }
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
            // Unity Quad 可见面法线是 -Z：让 +Z 背对相机，-Z（画面）才朝相机。
            // 原式把 +Z 转向相机，等于永远以背面示人，整个像素人被背面剔除。
            float yaw = Mathf.Atan2(-toCam.x, -toCam.z) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        }
    }
}
