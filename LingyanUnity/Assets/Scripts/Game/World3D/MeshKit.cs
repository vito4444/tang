using System.Collections.Generic;
using UnityEngine;

namespace Lingyan.Game.World3D
{
    /// <summary>
    /// 程序化网格与原语的小工具。原则：能用原语（Cube/Cylinder/Sphere）就用，
    /// 只有唐构特有形体（坡屋面、山墙三角、鸱尾挤出）手写网格。
    /// </summary>
    public static class MeshKit
    {
        // ---- 原语 ----

        public static GameObject Box(
            Transform parent, string name, Vector3 center, Vector3 size, Color color)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = center;
            go.transform.localScale = size;
            Paint(go, color);
            return go;
        }

        public static GameObject Cylinder(
            Transform parent, string name, Vector3 center, float radius, float height, Color color)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = center;
            // Unity 原语圆柱本高 2（±1），scale.y 取半高
            go.transform.localScale = new Vector3(radius * 2f, height / 2f, radius * 2f);
            Paint(go, color);
            return go;
        }

        public static GameObject Sphere(
            Transform parent, string name, Vector3 center, float diameter, Color color)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = center;
            go.transform.localScale = new Vector3(diameter, diameter, diameter);
            Paint(go, color);
            return go;
        }

        public static void Paint(GameObject go, Color color)
        {
            var renderer = go.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = TangColors.Mat(color);
            }
        }

        // ---- 手写网格 ----

        public sealed class Builder
        {
            private readonly List<Vector3> _vertices = new List<Vector3>();
            private readonly List<int> _triangles = new List<int>();

            public void AddTriangle(Vector3 a, Vector3 b, Vector3 c)
            {
                int i = _vertices.Count;
                _vertices.Add(a);
                _vertices.Add(b);
                _vertices.Add(c);
                _triangles.Add(i);
                _triangles.Add(i + 1);
                _triangles.Add(i + 2);
            }

            /// <summary>四边形按 a-b-c-d 环序（法线朝环序右手侧）。</summary>
            public void AddQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
            {
                AddTriangle(a, b, c);
                AddTriangle(a, c, d);
            }

            /// <summary>
            /// XY 平面闭合多边形沿 Z 挤出（厚度 thickness，居中）。
            /// 多边形点序须逆时针（正面朝 +Z）。侧壁自动缝合，正反面扇形三角化。
            /// </summary>
            public void ExtrudePolygon(IReadOnlyList<(double x, double y)> polygon, float thickness)
            {
                int n = polygon.Count;
                float halfT = thickness / 2f;

                var front = new Vector3[n];
                var back = new Vector3[n];
                for (int i = 0; i < n; i++)
                {
                    front[i] = new Vector3((float)polygon[i].x, (float)polygon[i].y, halfT);
                    back[i] = new Vector3((float)polygon[i].x, (float)polygon[i].y, -halfT);
                }

                // 正面（+Z，点序逆时针 → 面向 +Z 时针序需反转）与背面
                for (int i = 1; i < n - 1; i++)
                {
                    AddTriangle(front[0], front[i + 1], front[i]);
                    AddTriangle(back[0], back[i], back[i + 1]);
                }

                // 侧壁
                for (int i = 0; i < n; i++)
                {
                    int j = (i + 1) % n;
                    AddQuad(front[i], front[j], back[j], back[i]);
                }
            }

            public GameObject Build(Transform parent, string name, Color color)
            {
                var go = new GameObject(name);
                go.transform.SetParent(parent, false);
                var mesh = new Mesh();
                mesh.name = name;
                mesh.vertices = _vertices.ToArray();
                mesh.triangles = _triangles.ToArray();
                mesh.RecalculateNormals();
                mesh.RecalculateBounds();
                var filter = go.AddComponent<MeshFilter>();
                filter.sharedMesh = mesh;
                var renderer = go.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = TangColors.Mat(color);
                return go;
            }
        }
    }
}
