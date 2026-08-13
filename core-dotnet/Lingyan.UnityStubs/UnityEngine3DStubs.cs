// 3D/渲染相关 UnityEngine API 桩，仅供编译检查。
using System;

namespace UnityEngine
{
    public class Mesh : Object
    {
        public Vector3[] vertices { get; set; }
        public int[] triangles { get; set; }
        public Vector2[] uv { get; set; }
        public void RecalculateNormals() { }
        public void RecalculateBounds() { }
        public void Clear() { }
    }

    public class MeshFilter : Component
    {
        public Mesh mesh { get; set; }
        public Mesh sharedMesh { get; set; }
    }

    public class Renderer : Component
    {
        public Material material { get; set; }
        public Material sharedMaterial { get; set; }
        public bool receiveShadows { get; set; }
    }

    public class MeshRenderer : Renderer { }

    public class Shader : Object
    {
        public static Shader Find(string name) { return null; }
    }

    public class Material : Object
    {
        public Material(Shader shader) { }
        public Color color { get; set; }
        public void SetFloat(string name, float value) { }
        public void SetColor(string name, Color value) { }
        public void EnableKeyword(string keyword) { }
    }

    public enum PrimitiveType
    {
        Sphere = 0,
        Capsule = 1,
        Cylinder = 2,
        Cube = 3,
        Plane = 4,
        Quad = 5
    }

    public enum LightType { Spot = 0, Directional = 1, Point = 2 }

    public enum LightShadows { None = 0, Hard = 1, Soft = 2 }

    public class Light : Behaviour
    {
        public LightType type { get; set; }
        public Color color { get; set; }
        public float intensity { get; set; }
        public LightShadows shadows { get; set; }
        public float shadowStrength { get; set; }
    }

    public enum FogMode { Linear = 1, Exponential = 2, ExponentialSquared = 3 }

    public static class RenderSettings
    {
        public static Color ambientLight { get; set; }
        public static bool fog { get; set; }
        public static Color fogColor { get; set; }
        public static float fogDensity { get; set; }
        public static FogMode fogMode { get; set; }
    }

    public class RenderTexture : Texture
    {
        public RenderTexture(int width, int height, int depth) { }
        public static RenderTexture active { get; set; }
        public void Release() { }
    }

    public static class Time
    {
        public static float deltaTime { get { return 0f; } }
        public static float unscaledDeltaTime { get { return 0f; } }
    }

    public enum KeyCode
    {
        None = 0, W = 119, A = 97, S = 115, D = 100, Q = 113, E = 101,
        UpArrow = 273, DownArrow = 274, RightArrow = 275, LeftArrow = 276
    }

    public static class Input
    {
        public static bool GetMouseButton(int button) { return false; }
        public static float GetAxis(string axisName) { return 0f; }
        public static bool GetKey(KeyCode key) { return false; }
        public static Vector3 mousePosition { get { return default; } }
    }

    public struct Ray
    {
        public Vector3 origin;
        public Vector3 direction;
    }

    public class Collider : Component { }

    public struct RaycastHit
    {
        public Collider collider { get { return null; } }
        public Vector3 point { get { return default; } }
    }

    public static class Physics
    {
        public static bool Raycast(Ray ray, out RaycastHit hitInfo, float maxDistance)
        {
            hitInfo = default;
            return false;
        }
    }
}
