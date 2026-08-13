using System.Collections.Generic;
using Lingyan.Core.Reputation;
using Lingyan.Core.World;
using UnityEngine;

namespace Lingyan.Game.World3D
{
    /// <summary>已装配的坊场景：随时辰刷新天光、坊门开闭与 NPC 位置。</summary>
    public sealed class WardScene
    {
        public GameObject Root { get; }
        public DayLightRig LightRig { get; }

        private readonly Dictionary<string, Vector3> _anchors;
        private readonly Dictionary<string, GameObject> _npcMarkers;
        private readonly List<GameObject> _gateLeaves;

        internal WardScene(
            GameObject root, DayLightRig lightRig,
            Dictionary<string, Vector3> anchors,
            Dictionary<string, GameObject> npcMarkers,
            List<GameObject> gateLeaves)
        {
            Root = root;
            LightRig = lightRig;
            _anchors = anchors;
            _npcMarkers = npcMarkers;
            _gateLeaves = gateLeaves;
        }

        /// <summary>时辰驱动：天光、坊门（暮鼓闭、晓鼓开）、NPC 按作息表落位。</summary>
        public void ApplyHour(int hourIndex, Camera camera)
        {
            LightRig.Apply(hourIndex, camera);

            bool gatesOpen = WardDef.GatesOpenAt(hourIndex);
            foreach (GameObject leaf in _gateLeaves)
            {
                // 开门时门扇转贴门墩内侧，闭门时合拢堵住门洞
                float sign = leaf.transform.localPosition.x < 0 ? 1f : -1f;
                leaf.transform.localRotation =
                    Quaternion.Euler(0f, gatesOpen ? sign * 80f : 0f, 0f);
            }

            foreach (NpcScheduleDef npc in SampleWard.Npcs)
            {
                ScheduleEntry entry = npc.At(hourIndex);
                if (_npcMarkers.TryGetValue(npc.NpcId, out GameObject marker)
                    && _anchors.TryGetValue(entry.PlaceId, out Vector3 anchor))
                {
                    marker.transform.localPosition = anchor;
                }
            }
        }
    }

    /// <summary>
    /// 槐里坊三维装配（阶段 2 视觉件，程序化占位形体）：
    /// 夯土坊墙、南门楼（门扇随时辰启闭）、三座唐屋、井亭、槐树、NPC 立标。
    /// 建筑红线由 TangHouseBuilder / TangArchitectureSpec 保证。
    /// </summary>
    public static class WardSceneBuilder
    {
        public static WardScene Build(Transform parent)
        {
            var root = new GameObject("WardScene");
            root.transform.SetParent(parent, false);
            Transform t = root.transform;

            // 地面
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.SetParent(t, false);
            ground.transform.localScale = new Vector3(9f, 1f, 8f); // Plane 原语 10×10 → 90×80
            MeshKit.Paint(ground, TangColors.Ground);

            BuildWalls(t);
            List<GameObject> gateLeaves = BuildSouthGate(t);

            // 三座屋：桓宅（大）、康家小院、武侯铺
            TangHouseBuilder.Build(t, "House_Huan", new Vector3(-12f, 0f, 9f),
                new TangHouseBuilder.Config { Width = 9f, Depth = 6f, ColumnHeight = 3.6f });
            TangHouseBuilder.Build(t, "House_Kang", new Vector3(13f, 0f, 11f),
                new TangHouseBuilder.Config { Width = 6f, Depth = 4.5f, ColumnHeight = 3.0f });
            TangHouseBuilder.Build(t, "WuhouPost", new Vector3(8f, 0f, -15f),
                new TangHouseBuilder.Config { Width = 4.5f, Depth = 3.5f, ColumnHeight = 2.8f });

            BuildWellPavilion(t, new Vector3(-3f, 0f, -3f));

            // 槐树
            var treeSpots = new[]
            {
                new Vector3(-6f, 0f, 2f), new Vector3(4f, 0f, 4f), new Vector3(-16f, 0f, -8f),
                new Vector3(17f, 0f, 0f), new Vector3(-4f, 0f, -14f), new Vector3(12f, 0f, -6f)
            };
            for (int i = 0; i < treeSpots.Length; i++)
            {
                BuildPagodaTree(t, "Tree" + i, treeSpots[i]);
            }

            // NPC 场景锚点（与作息表 placeId 一一对应）
            var anchors = new Dictionary<string, Vector3>
            {
                ["home_kang"] = new Vector3(13f, 0f, 7.5f),
                ["home_huan"] = new Vector3(-12f, 0f, 5f),
                ["well"] = new Vector3(-4.6f, 0f, -3f),
                ["south_gate"] = new Vector3(1.6f, 0f, -20f),
                ["wuhou_post"] = new Vector3(8f, 0f, -12.6f),
                ["ward_lanes"] = new Vector3(0f, 0f, 0f),
                ["west_market"] = new Vector3(-6f, 0f, -30f) // 坊外（南墙之外）
            };

            var markers = new Dictionary<string, GameObject>();
            foreach (NpcScheduleDef npc in SampleWard.Npcs)
            {
                markers[npc.NpcId] = BuildNpcMarker(t, npc);
            }

            var lightRig = new DayLightRig(t);
            return new WardScene(root, lightRig, anchors, markers, gateLeaves);
        }

        private static void BuildWalls(Transform t)
        {
            const float wallHeight = 2.9f;
            const float wallThickness = 0.9f;
            const float halfX = 26f;
            const float halfZ = 21f;

            // 北、东、西整墙；南墙留 6 米门洞
            MeshKit.Box(t, "WallN", new Vector3(0, wallHeight / 2f, halfZ),
                new Vector3(halfX * 2f + wallThickness, wallHeight, wallThickness),
                TangColors.RammedEarth);
            foreach (float xSign in new[] { -1f, 1f })
            {
                MeshKit.Box(t, "WallSide", new Vector3(xSign * halfX, wallHeight / 2f, 0),
                    new Vector3(wallThickness, wallHeight, halfZ * 2f), TangColors.RammedEarth);
            }
            float southSegWidth = halfX - 3f;
            foreach (float xSign in new[] { -1f, 1f })
            {
                MeshKit.Box(t, "WallS",
                    new Vector3(xSign * (3f + southSegWidth / 2f), wallHeight / 2f, -halfZ),
                    new Vector3(southSegWidth + wallThickness, wallHeight, wallThickness),
                    TangColors.RammedEarth);
            }

            // 墙帽（夯土墙顶的瓦檐线）
            foreach (var (center, size) in new (Vector3, Vector3)[]
            {
                (new Vector3(0, wallHeight + 0.10f, halfZ),
                    new Vector3(halfX * 2f + 1.3f, 0.2f, wallThickness + 0.5f)),
                (new Vector3(-halfX, wallHeight + 0.10f, 0),
                    new Vector3(wallThickness + 0.5f, 0.2f, halfZ * 2f)),
                (new Vector3(halfX, wallHeight + 0.10f, 0),
                    new Vector3(wallThickness + 0.5f, 0.2f, halfZ * 2f)),
                (new Vector3(-(3f + southSegWidth / 2f), wallHeight + 0.10f, -halfZ),
                    new Vector3(southSegWidth + 1.3f, 0.2f, wallThickness + 0.5f)),
                (new Vector3(3f + southSegWidth / 2f, wallHeight + 0.10f, -halfZ),
                    new Vector3(southSegWidth + 1.3f, 0.2f, wallThickness + 0.5f))
            })
            {
                MeshKit.Box(t, "WallCap", center, size, TangColors.Tile);
            }
        }

        private static List<GameObject> BuildSouthGate(Transform t)
        {
            const float halfZ = 21f;
            var gate = new GameObject("SouthGate");
            gate.transform.SetParent(t, false);
            gate.transform.localPosition = new Vector3(0f, 0f, -halfZ);
            Transform g = gate.transform;

            // 门墩
            foreach (float xSign in new[] { -1f, 1f })
            {
                MeshKit.Box(g, "GatePier", new Vector3(xSign * 4.2f, 1.8f, 0f),
                    new Vector3(2.6f, 3.6f, 1.9f), TangColors.RammedEarth);
            }
            // 过梁与小门屋
            MeshKit.Box(g, "GateLintel", new Vector3(0f, 3.9f, 0f),
                new Vector3(11.6f, 0.7f, 2.1f), TangColors.Timber);
            float pitch = (float)Lingyan.Core.Architecture.TangArchitectureSpec
                .RoofPitchRadians(2.6) * Mathf.Rad2Deg;
            foreach (float zSign in new[] { -1f, 1f })
            {
                GameObject slope = MeshKit.Box(g, "GateRoof", Vector3.zero,
                    new Vector3(12.6f, 0.12f, 1.9f), TangColors.Tile);
                slope.transform.localPosition = new Vector3(0f, 4.75f, zSign * 0.85f);
                slope.transform.localRotation = Quaternion.Euler(zSign * pitch, 0f, 0f);
            }
            MeshKit.Box(g, "GateRidge", new Vector3(0f, 5.10f, 0f),
                new Vector3(12.6f, 0.22f, 0.3f), TangColors.Ridge);
            foreach (float xSign in new[] { -1f, 1f })
            {
                var builder = new MeshKit.Builder();
                builder.ExtrudePolygon(
                    Lingyan.Core.Architecture.ChiweiProfile.Points(0.75), 0.16f);
                GameObject chiwei = builder.Build(g, "GateChiwei", TangColors.Ridge);
                chiwei.transform.localPosition = new Vector3(xSign * 5.9f, 5.18f, 0f);
                chiwei.transform.localScale = new Vector3(xSign, 1f, 1f);
            }

            // 门扇两页（绕各自铰边转动，ApplyHour 控制开闭）
            var leaves = new List<GameObject>();
            foreach (float xSign in new[] { -1f, 1f })
            {
                var hinge = new GameObject(xSign < 0 ? "GateLeafL" : "GateLeafR");
                hinge.transform.SetParent(g, false);
                hinge.transform.localPosition = new Vector3(xSign * 2.9f, 0f, 0f);
                MeshKit.Box(hinge.transform, "Leaf",
                    new Vector3(-xSign * 1.45f, 1.7f, 0f),
                    new Vector3(2.9f, 3.4f, 0.16f), TangColors.Door);
                leaves.Add(hinge);
            }
            return leaves;
        }

        private static void BuildWellPavilion(Transform t, Vector3 position)
        {
            var pavilion = new GameObject("WellPavilion");
            pavilion.transform.SetParent(t, false);
            pavilion.transform.localPosition = position;
            Transform p = pavilion.transform;

            MeshKit.Cylinder(p, "WellRing", new Vector3(0f, 0.35f, 0f), 0.7f, 0.7f,
                TangColors.Stone);
            foreach (var (x, z) in new[] { (-1.1f, -1.1f), (-1.1f, 1.1f), (1.1f, -1.1f), (1.1f, 1.1f) })
            {
                MeshKit.Cylinder(p, "PavColumn", new Vector3(x, 1.3f, z), 0.09f, 2.6f,
                    TangColors.Timber);
            }
            float pitch = (float)Lingyan.Core.Architecture.TangArchitectureSpec
                .RoofPitchRadians(3.0) * Mathf.Rad2Deg;
            foreach (float zSign in new[] { -1f, 1f })
            {
                GameObject slope = MeshKit.Box(p, "PavRoof", Vector3.zero,
                    new Vector3(3.4f, 0.1f, 1.9f), TangColors.Tile);
                slope.transform.localPosition = new Vector3(0f, 2.95f, zSign * 0.8f);
                slope.transform.localRotation = Quaternion.Euler(zSign * pitch, 0f, 0f);
            }
            MeshKit.Box(p, "PavRidge", new Vector3(0f, 3.28f, 0f),
                new Vector3(3.4f, 0.16f, 0.22f), TangColors.Ridge);
        }

        private static void BuildPagodaTree(Transform t, string name, Vector3 position)
        {
            var tree = new GameObject(name);
            tree.transform.SetParent(t, false);
            tree.transform.localPosition = position;
            MeshKit.Cylinder(tree.transform, "Trunk", new Vector3(0f, 1.3f, 0f), 0.18f, 2.6f,
                TangColors.Trunk);
            MeshKit.Sphere(tree.transform, "Crown1", new Vector3(0f, 3.4f, 0f), 2.9f,
                TangColors.Foliage);
            MeshKit.Sphere(tree.transform, "Crown2", new Vector3(0.9f, 2.9f, 0.5f), 2.0f,
                TangColors.Foliage);
        }

        private static GameObject BuildNpcMarker(Transform t, NpcScheduleDef npc)
        {
            Color robe;
            switch (npc.Archetype)
            {
                case NpcArchetype.Official: robe = new Color(0.24f, 0.30f, 0.34f); break; // 皂衣
                case NpcArchetype.Merchant: robe = new Color(0.55f, 0.38f, 0.22f); break; // 赭衣
                default: robe = new Color(0.78f, 0.74f, 0.66f); break;                    // 素衣
            }

            var marker = new GameObject("Npc_" + npc.NpcId);
            marker.transform.SetParent(t, false);

            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(marker.transform, false);
            body.transform.localPosition = new Vector3(0f, 0.85f, 0f);
            body.transform.localScale = new Vector3(0.62f, 0.85f, 0.62f);
            MeshKit.Paint(body, robe);

            MeshKit.Sphere(marker.transform, "Head", new Vector3(0f, 1.78f, 0f), 0.42f,
                new Color(0.85f, 0.72f, 0.60f));
            return marker;
        }
    }
}
