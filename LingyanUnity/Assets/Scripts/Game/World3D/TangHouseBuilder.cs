using System.Collections.Generic;
using Lingyan.Core.Architecture;
using UnityEngine;

namespace Lingyan.Game.World3D
{
    /// <summary>
    /// 程序化唐式悬山屋：台基、柱列、额枋、铺作层（含下昂）、平缓坡屋面、
    /// 正脊鸱尾、白墙、板门、直棂窗。所有比例取自 TangArchitectureSpec（可测红线）。
    /// 占位形体：阶段 9 以正式模型替换，但比例契约不变。
    /// </summary>
    public static class TangHouseBuilder
    {
        public sealed class Config
        {
            /// <summary>面阔（x，米）。</summary>
            public float Width = 9f;

            /// <summary>进深（z，米）。</summary>
            public float Depth = 6f;

            /// <summary>柱高（台基顶至柱头，米）。</summary>
            public float ColumnHeight = 3.6f;

            /// <summary>是否开前门与前窗（临街小铺可全敞，此处民宅默认开）。</summary>
            public bool FrontOpenings = true;
        }

        public static GameObject Build(Transform parent, string name, Vector3 position, Config cfg)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.localPosition = position;

            float w = cfg.Width;
            float d = cfg.Depth;
            float ch = cfg.ColumnHeight;

            // 台基
            const float plinthHeight = 0.45f;
            MeshKit.Box(root.transform, "Plinth",
                new Vector3(0, plinthHeight / 2f, 0),
                new Vector3(w + 1.4f, plinthHeight, d + 1.4f), TangColors.Stone);
            float baseTop = plinthHeight;

            // 檐柱：三间四柱 × 前后两排（唐柱粗壮，径约柱高 1/8），下置柱础
            float columnDiameter = ch * 0.14f;
            var columnXs = new[] { -w / 2f, -w / 6f, w / 6f, w / 2f };
            foreach (float x in columnXs)
            {
                foreach (float zSign in new[] { -1f, 1f })
                {
                    MeshKit.Cylinder(root.transform, "Column",
                        new Vector3(x, baseTop + ch / 2f, zSign * d / 2f),
                        columnDiameter / 2f, ch, TangColors.Timber);
                    MeshKit.Box(root.transform, "ColumnBase",
                        new Vector3(x, baseTop + 0.09f, zSign * d / 2f),
                        new Vector3(columnDiameter * 1.5f, 0.18f, columnDiameter * 1.5f),
                        TangColors.Stone);
                }
            }

            // 额枋（柱头横木）
            float lintelY = baseTop + ch - 0.12f;
            MeshKit.Box(root.transform, "LintelFront",
                new Vector3(0, lintelY, -d / 2f), new Vector3(w + 0.4f, 0.24f, 0.28f),
                TangColors.Timber);
            MeshKit.Box(root.transform, "LintelBack",
                new Vector3(0, lintelY, d / 2f), new Vector3(w + 0.4f, 0.24f, 0.28f),
                TangColors.Timber);
            foreach (float xSign in new[] { -1f, 1f })
            {
                MeshKit.Box(root.transform, "LintelSide",
                    new Vector3(xSign * w / 2f, lintelY, 0), new Vector3(0.28f, 0.24f, d),
                    TangColors.Timber);
            }

            // 铺作层（斗拱带）：高 = 柱高之半（红线"大"）
            float bracketBand = (float)TangArchitectureSpec.BracketBandHeight(ch);
            float bracketBase = baseTop + ch;
            float eaveOverhang = (float)TangArchitectureSpec.EaveOverhang(ch);
            BuildBracketRows(root.transform, columnXs, w, d, bracketBase, bracketBand, eaveOverhang);

            // 屋面：举高比进深 1:6（红线"平"），出檐深远（红线"远"）
            float eaveY = bracketBase + bracketBand;
            BuildRoof(root.transform, w, d, ch, eaveY, eaveOverhang);

            // 墙体与门窗
            BuildWalls(root.transform, w, d, ch, baseTop, cfg.FrontOpenings);

            return root;
        }

        private static void BuildBracketRows(
            Transform parent, float[] columnXs, float w, float d,
            float bracketBase, float bandHeight, float eaveOverhang)
        {
            // 柱头铺作 + 补间铺作（柱间各一攒），前后檐各一排
            var puXs = new List<float>(columnXs);
            for (int i = 0; i < columnXs.Length - 1; i++)
            {
                puXs.Add((columnXs[i] + columnXs[i + 1]) / 2f);
            }

            foreach (float zSign in new[] { -1f, 1f })
            {
                foreach (float x in puXs)
                {
                    BuildBracketSet(parent, new Vector3(x, bracketBase, zSign * d / 2f),
                        zSign, bandHeight, eaveOverhang);
                }
            }
            // 山面各两攒（简化）
            foreach (float xSign in new[] { -1f, 1f })
            {
                foreach (float z in new[] { -d / 6f, d / 6f })
                {
                    MeshKit.Box(parent, "BracketSideBlock",
                        new Vector3(xSign * w / 2f, bracketBase + bandHeight * 0.3f, z),
                        new Vector3(0.34f, bandHeight * 0.6f, 0.42f), TangColors.TimberDark);
                }
            }

            // 拱眼壁：铺作之间以白灰填充（唐构做法），顺带挡住檐下透空
            foreach (float zSign in new[] { -1f, 1f })
            {
                MeshKit.Box(parent, "GongyanWall",
                    new Vector3(0, bracketBase + bandHeight * 0.5f, zSign * (d / 2f - 0.10f)),
                    new Vector3(w + 0.2f, bandHeight, 0.16f), TangColors.Wall);
            }
            // 檐檩（铺作顶通长承檐）
            foreach (float zSign in new[] { -1f, 1f })
            {
                MeshKit.Box(parent, "EavePurlin",
                    new Vector3(0, bracketBase + bandHeight - 0.10f,
                        zSign * (d / 2f + eaveOverhang * 0.35f)),
                    new Vector3(w + 1.2f, 0.20f, 0.20f), TangColors.Timber);
            }
        }

        /// <summary>
        /// 一攒铺作（简化形体）：栌斗 + 出跳华拱 + 令拱 + 下昂。
        /// 红线：必须有下昂——长斜杆向外下方冲出，那道斜线是轮廓的主体；
        /// 全是直角方块叠出来的是牛腿，不是唐斗拱。
        /// </summary>
        private static void BuildBracketSet(
            Transform parent, Vector3 basePos, float zSign, float bandHeight, float eaveOverhang)
        {
            // 栌斗（大坐斗）
            MeshKit.Box(parent, "LuDou",
                basePos + new Vector3(0, bandHeight * 0.14f, 0),
                new Vector3(0.44f, bandHeight * 0.28f, 0.44f), TangColors.Timber);

            // 华拱出跳（沿进深向外伸）
            MeshKit.Box(parent, "HuaGong",
                basePos + new Vector3(0, bandHeight * 0.42f, zSign * eaveOverhang * 0.22f),
                new Vector3(0.26f, bandHeight * 0.18f, eaveOverhang * 0.62f), TangColors.Timber);

            // 令拱（面阔向横木，承檐檩）
            MeshKit.Box(parent, "LingGong",
                basePos + new Vector3(0, bandHeight * 0.68f, zSign * eaveOverhang * 0.30f),
                new Vector3(1.0f, bandHeight * 0.16f, 0.22f), TangColors.Timber);

            // 下昂：自攒心斜向外下，昂身即那道斜线；昂尖压在檐口之下，不穿瓦面
            float angLength = eaveOverhang * 1.0f;
            const float angPitchDeg = 26f;
            GameObject ang = MeshKit.Box(parent, "XiaAng",
                Vector3.zero, new Vector3(0.16f, 0.11f, angLength), TangColors.TimberDark);
            ang.transform.localPosition = basePos + new Vector3(
                0,
                bandHeight * 0.40f - Mathf.Sin(angPitchDeg * Mathf.Deg2Rad) * angLength * 0.30f,
                zSign * angLength * 0.40f);
            ang.transform.localRotation = Quaternion.Euler(zSign * angPitchDeg, 0, 0);
        }

        private static void BuildRoof(
            Transform parent, float w, float d, float ch, float eaveY, float eaveOverhang)
        {
            float rise = (float)TangArchitectureSpec.RoofRise(d);
            float pitch = (float)TangArchitectureSpec.RoofPitchRadians(d);

            // 悬山出际（山面挑出）
            const float gableOverhang = 0.9f;
            float roofWidth = w + gableOverhang * 2f;

            // 两坡：斜置薄板，坡角即红线坡角
            float halfSpan = d / 2f + eaveOverhang;
            float ridgeY = eaveY + Mathf.Sin(pitch) / Mathf.Cos(pitch) * halfSpan;
            float slopeLength = halfSpan / Mathf.Cos(pitch);

            foreach (float zSign in new[] { -1f, 1f })
            {
                Quaternion slopeRotation = Quaternion.Euler(zSign * pitch * Mathf.Rad2Deg, 0, 0);
                Vector3 slopeCenter = new Vector3(
                    0, (eaveY + ridgeY) / 2f + 0.05f, zSign * halfSpan / 2f);

                GameObject slope = MeshKit.Box(parent, zSign < 0 ? "RoofFront" : "RoofBack",
                    Vector3.zero, new Vector3(roofWidth, 0.15f, slopeLength), TangColors.Tile);
                slope.transform.localPosition = slopeCenter;
                slope.transform.localRotation = slopeRotation;

                // 筒瓦垄：沿坡面等距窄条，读出唐瓦屋面的纵向肌理
                Vector3 slopeNormal = slopeRotation * Vector3.up;
                int ridgeCount = (int)(roofWidth / 0.62f);
                float ridgeSpan = roofWidth - 0.5f;
                for (int i = 0; i <= ridgeCount; i++)
                {
                    float x = -ridgeSpan / 2f + ridgeSpan * i / ridgeCount;
                    GameObject tileRidge = MeshKit.Box(parent, "TileRidge",
                        Vector3.zero, new Vector3(0.10f, 0.06f, slopeLength - 0.1f),
                        TangColors.Ridge);
                    tileRidge.transform.localPosition =
                        slopeCenter + new Vector3(x, 0, 0) + slopeNormal * 0.10f;
                    tileRidge.transform.localRotation = slopeRotation;
                }

                // 檐口椽带（深色收边，读出檐厚度）
                GameObject eaveTrim = MeshKit.Box(parent, "EaveTrim",
                    Vector3.zero, new Vector3(roofWidth, 0.16f, 0.22f), TangColors.TimberDark);
                eaveTrim.transform.localPosition = new Vector3(0, eaveY + 0.02f, zSign * halfSpan);
            }

            // 正脊
            MeshKit.Box(parent, "Ridge",
                new Vector3(0, ridgeY + 0.12f, 0), new Vector3(roofWidth, 0.30f, 0.36f),
                TangColors.Ridge);

            // 鸱尾 ×2（卷尾鳍形，无兽头无张口；轮廓由 Core 生成，形态有测试）
            float chiweiHeight = ch * 0.50f;
            IReadOnlyList<(double x, double y)> profile = ChiweiProfile.Points(chiweiHeight);
            foreach (float xSign in new[] { -1f, 1f })
            {
                var builder = new MeshKit.Builder();
                builder.ExtrudePolygon(profile, 0.20f);
                GameObject chiwei = builder.Build(parent, "Chiwei", TangColors.Ridge);
                chiwei.transform.localPosition = new Vector3(
                    xSign * (roofWidth / 2f - 0.35f), ridgeY + 0.10f, 0);
                // 轮廓 +x 为背缘朝外：左端镜像
                chiwei.transform.localScale = new Vector3(xSign, 1f, 1f);
            }
        }

        private static void BuildWalls(
            Transform parent, float w, float d, float ch, float baseTop, bool frontOpenings)
        {
            const float wallThickness = 0.24f;
            float wallHeight = ch - 0.1f;
            float wallCenterY = baseTop + wallHeight / 2f;

            // 后墙
            MeshKit.Box(parent, "WallBack",
                new Vector3(0, wallCenterY, d / 2f - 0.2f),
                new Vector3(w - 0.2f, wallHeight, wallThickness), TangColors.Wall);

            // 山墙（矩形部分；上部三角）
            foreach (float xSign in new[] { -1f, 1f })
            {
                MeshKit.Box(parent, "WallGable",
                    new Vector3(xSign * (w / 2f - 0.15f), wallCenterY, 0),
                    new Vector3(wallThickness, wallHeight, d - 0.2f), TangColors.Wall);

                float rise = (float)TangArchitectureSpec.RoofRise(d);
                var tri = new MeshKit.Builder();
                float y0 = baseTop + wallHeight;
                tri.AddTriangle(
                    new Vector3(0, y0, -d / 2f),
                    new Vector3(0, y0 + rise, 0),
                    new Vector3(0, y0, d / 2f));
                tri.AddTriangle(
                    new Vector3(0, y0, d / 2f),
                    new Vector3(0, y0 + rise, 0),
                    new Vector3(0, y0, -d / 2f));
                GameObject gableTri = tri.Build(parent, "GableTriangle", TangColors.Wall);
                gableTri.transform.localPosition = new Vector3(xSign * (w / 2f - 0.15f), 0, 0);
            }

            if (!frontOpenings)
            {
                MeshKit.Box(parent, "WallFrontSolid",
                    new Vector3(0, wallCenterY, -d / 2f + 0.2f),
                    new Vector3(w - 0.2f, wallHeight, wallThickness), TangColors.Wall);
                return;
            }

            // 前墙：明间开板门，两次间白墙嵌直棂窗
            float zFront = -d / 2f + 0.2f;
            const float doorWidth = 1.5f;
            const float doorHeight = 2.2f;
            float sideWidth = (w - 0.2f - doorWidth) / 2f;
            float sideCenterX = doorWidth / 2f + sideWidth / 2f;

            foreach (float xSign in new[] { -1f, 1f })
            {
                MeshKit.Box(parent, "WallFront",
                    new Vector3(xSign * sideCenterX, wallCenterY, zFront),
                    new Vector3(sideWidth, wallHeight, wallThickness), TangColors.Wall);
                BuildLatticeWindow(parent,
                    new Vector3(xSign * sideCenterX, baseTop + ch * 0.52f, zFront - 0.14f));
            }

            // 门上横墙
            MeshKit.Box(parent, "WallOverDoor",
                new Vector3(0, baseTop + doorHeight + (wallHeight - doorHeight) / 2f, zFront),
                new Vector3(doorWidth + 0.1f, wallHeight - doorHeight, wallThickness),
                TangColors.Wall);

            // 板门两扇（虚掩）
            foreach (float xSign in new[] { -1f, 1f })
            {
                MeshKit.Box(parent, "DoorLeaf",
                    new Vector3(xSign * doorWidth / 4f, baseTop + doorHeight / 2f, zFront - 0.05f),
                    new Vector3(doorWidth / 2f - 0.04f, doorHeight, 0.09f), TangColors.Door);
            }
        }

        /// <summary>直棂窗：框 + 竖棂（布局由 Core 计算，条数与均匀性有测试）。</summary>
        private static void BuildLatticeWindow(Transform parent, Vector3 center)
        {
            const float windowWidth = 1.5f;
            const float windowHeight = 1.3f;
            const float frameBar = 0.10f;

            // 框
            foreach (float ySign in new[] { -1f, 1f })
            {
                MeshKit.Box(parent, "WindowFrameH",
                    center + new Vector3(0, ySign * (windowHeight / 2f - frameBar / 2f), 0),
                    new Vector3(windowWidth, frameBar, 0.09f), TangColors.Door);
            }
            foreach (float xSign in new[] { -1f, 1f })
            {
                MeshKit.Box(parent, "WindowFrameV",
                    center + new Vector3(xSign * (windowWidth / 2f - frameBar / 2f), 0, 0),
                    new Vector3(frameBar, windowHeight - frameBar * 2f, 0.09f), TangColors.Door);
            }

            // 直棂（竖条，唐窗；花格繁复属明清，禁用）
            float clearWidth = windowWidth - frameBar * 2f;
            foreach (double offset in TangArchitectureSpec.LatticeBarOffsets(clearWidth))
            {
                MeshKit.Box(parent, "LatticeBar",
                    center + new Vector3((float)offset, 0, 0),
                    new Vector3((float)TangArchitectureSpec.LatticeBarWidth,
                        windowHeight - frameBar * 2f, 0.05f), TangColors.Door);
            }
        }
    }
}
