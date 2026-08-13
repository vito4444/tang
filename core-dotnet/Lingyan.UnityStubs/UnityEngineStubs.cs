// 仅供 CI 编译检查的 UnityEngine API 桩：签名对齐真实 API，全部空实现。
// 不随游戏发行，不在 Unity 工程内。真实行为以 Unity 6000.0 为准。
using System;

namespace UnityEngine
{
    public class Object
    {
        public string name { get; set; }
        public static void Destroy(Object obj) { }
        public static void DontDestroyOnLoad(Object target) { }
        public static T FindFirstObjectByType<T>() where T : Object { return null; }
        public static T[] FindObjectsByType<T>(FindObjectsSortMode sortMode) where T : Object
        {
            return Array.Empty<T>();
        }
        public static T Instantiate<T>(T original, Transform parent) where T : Object
        {
            return original;
        }
        public static T Instantiate<T>(T original) where T : Object
        {
            return original;
        }
    }

    public enum FindObjectsSortMode { None = 0, InstanceID = 1 }

    public sealed class GameObject : Object
    {
        public GameObject(string name) { this.name = name; }
        public string tag { get; set; }
        public Transform transform { get { return null; } }
        public T AddComponent<T>() where T : Component { return null; }
        public T GetComponent<T>() { return default; }
        public T GetComponentInChildren<T>() { return default; }
        public void SetActive(bool value) { }
        public bool activeSelf { get { return true; } }
        public static GameObject CreatePrimitive(PrimitiveType type) { return null; }
        public static GameObject Find(string name) { return null; }
    }

    public class Component : Object
    {
        public GameObject gameObject { get { return null; } }
        public Transform transform { get { return null; } }
    }

    public class Behaviour : Component
    {
        public bool enabled { get; set; }
    }

    public class MonoBehaviour : Behaviour { }

    public class Transform : Component
    {
        public void SetParent(Transform parent, bool worldPositionStays) { }
        public void SetParent(Transform parent) { }
        public int childCount { get { return 0; } }
        public Transform GetChild(int index) { return null; }
        public Transform Find(string n) { return null; }
        public Transform parent { get { return null; } }
        public Quaternion localRotation { get; set; }
        public Quaternion rotation { get; set; }
        public Vector3 position { get; set; }
        public Vector3 localPosition { get; set; }
        public Vector3 localScale { get; set; }
        public Vector3 forward { get { return default; } }
        public void LookAt(Vector3 worldPosition) { }
    }

    public struct Quaternion
    {
        public static Quaternion identity { get { return default; } }
        public static Quaternion Euler(float x, float y, float z) { return default; }
        public static Vector3 operator *(Quaternion rotation, Vector3 point) { return point; }
        public static Quaternion operator *(Quaternion a, Quaternion b) { return a; }
    }

    public class RectTransform : Transform
    {
        public Vector2 anchorMin { get; set; }
        public Vector2 anchorMax { get; set; }
        public Vector2 offsetMin { get; set; }
        public Vector2 offsetMax { get; set; }
        public Vector2 sizeDelta { get; set; }
        public Vector2 anchoredPosition { get; set; }
        public Vector2 pivot { get; set; }
    }

    public struct Vector2
    {
        public float x;
        public float y;
        public Vector2(float x, float y) { this.x = x; this.y = y; }
        public static Vector2 zero { get { return new Vector2(0, 0); } }
        public static Vector2 one { get { return new Vector2(1, 1); } }
    }

    public struct Vector3
    {
        public float x;
        public float y;
        public float z;
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public static Vector3 zero { get { return new Vector3(0, 0, 0); } }
        public static Vector3 one { get { return new Vector3(1, 1, 1); } }
        public static Vector3 up { get { return new Vector3(0, 1, 0); } }
        public static Vector3 forward { get { return new Vector3(0, 0, 1); } }
        public static Vector3 operator +(Vector3 a, Vector3 b)
        {
            return new Vector3(a.x + b.x, a.y + b.y, a.z + b.z);
        }
        public static Vector3 operator -(Vector3 a, Vector3 b)
        {
            return new Vector3(a.x - b.x, a.y - b.y, a.z - b.z);
        }
        public static Vector3 operator *(Vector3 a, float d)
        {
            return new Vector3(a.x * d, a.y * d, a.z * d);
        }
        public Vector3 normalized { get { return this; } }
    }

    public struct Color
    {
        public float r;
        public float g;
        public float b;
        public float a;
        public Color(float r, float g, float b) { this.r = r; this.g = g; this.b = b; a = 1f; }
        public Color(float r, float g, float b, float a)
        {
            this.r = r; this.g = g; this.b = b; this.a = a;
        }
        public static Color white { get { return new Color(1, 1, 1, 1); } }
        public static Color black { get { return new Color(0, 0, 0, 1); } }
        public static Color Lerp(Color a, Color b, float t) { return a; }
        public static Color operator *(Color a, float d)
        {
            return new Color(a.r * d, a.g * d, a.b * d, a.a);
        }
    }

    public static class Mathf
    {
        public const float Deg2Rad = (float)(Math.PI / 180.0);
        public const float Rad2Deg = (float)(180.0 / Math.PI);
        public const float PI = (float)Math.PI;
        public static int RoundToInt(float f) { return (int)Math.Round(f); }
        public static float Max(float a, float b) { return Math.Max(a, b); }
        public static int Max(int a, int b) { return Math.Max(a, b); }
        public static float Min(float a, float b) { return Math.Min(a, b); }
        public static float Clamp(float v, float min, float max)
        {
            return v < min ? min : v > max ? max : v;
        }
        public static float Clamp01(float v) { return v < 0 ? 0 : v > 1 ? 1 : v; }
        public static float Abs(float v) { return Math.Abs(v); }
        public static float Sqrt(float v) { return (float)Math.Sqrt(v); }
        public static float Sin(float v) { return (float)Math.Sin(v); }
        public static float Cos(float v) { return (float)Math.Cos(v); }
        public static float Atan2(float y, float x) { return (float)Math.Atan2(y, x); }
        public static float Lerp(float a, float b, float t) { return a + (b - a) * Clamp01(t); }
        public static float PerlinNoise(float x, float y) { return 0.5f; }
    }

    public struct Rect
    {
        public Rect(float x, float y, float width, float height) { }
    }

    public static class Debug
    {
        public static void Log(object message) { }
        public static void LogWarning(object message) { }
        public static void LogError(object message) { }
    }

    public static class Application
    {
        public static string persistentDataPath { get { return ""; } }
        public static string dataPath { get { return ""; } }
        public static string version { get { return ""; } }
        public static void Quit() { }
    }

    public static class Resources
    {
        public static T Load<T>(string path) where T : Object { return null; }
    }

    public class TextAsset : Object
    {
        public string text { get { return null; } }
    }

    public class Font : Object { }

    public enum TextureFormat { RGB24 = 3, RGBA32 = 4 }

    public enum TextureWrapMode { Repeat = 0, Clamp = 1 }

    public enum FilterMode { Point = 0, Bilinear = 1, Trilinear = 2 }

    public class Texture : Object
    {
        public FilterMode filterMode { get; set; }
    }

    public class Texture2D : Texture
    {
        public Texture2D(int width, int height, TextureFormat format, bool mipChain) { }
        public Texture2D(int width, int height) { }
        public TextureWrapMode wrapMode { get; set; }
        public void SetPixel(int x, int y, Color color) { }
        public void SetPixels(Color[] colors) { }
        public void Apply() { }
        public void ReadPixels(Rect source, int destX, int destY) { }
        public byte[] EncodeToPNG() { return Array.Empty<byte>(); }
    }

    public class Sprite : Object
    {
        public static Sprite Create(Texture2D texture, Rect rect, Vector2 pivot) { return null; }
    }

    public enum CameraClearFlags { Skybox = 1, SolidColor = 2, Depth = 3, Nothing = 4 }

    public class Camera : Behaviour
    {
        public static Camera main { get { return null; } }
        public CameraClearFlags clearFlags { get; set; }
        public Color backgroundColor { get; set; }
        public int cullingMask { get; set; }
        public bool orthographic { get; set; }
        public float fieldOfView { get; set; }
        public float nearClipPlane { get; set; }
        public float farClipPlane { get; set; }
        public RenderTexture targetTexture { get; set; }
        public void Render() { }
        public Ray ScreenPointToRay(Vector3 pos) { return default; }
    }

    public enum RenderMode { ScreenSpaceOverlay = 0, ScreenSpaceCamera = 1, WorldSpace = 2 }

    public class Canvas : Behaviour
    {
        public RenderMode renderMode { get; set; }
        public int sortingOrder { get; set; }
        public Camera worldCamera { get; set; }
        public float planeDistance { get; set; }
        public static void ForceUpdateCanvases() { }
    }

    public enum RuntimeInitializeLoadType
    {
        AfterSceneLoad = 0,
        BeforeSceneLoad = 1,
        AfterAssembliesLoaded = 2,
        BeforeSplashScreen = 3,
        SubsystemRegistration = 4
    }

    [AttributeUsage(AttributeTargets.Method)]
    public sealed class RuntimeInitializeOnLoadMethodAttribute : Attribute
    {
        public RuntimeInitializeOnLoadMethodAttribute() { }
        public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType loadType) { }
    }
}

namespace UnityEngine.SceneManagement
{
    public struct Scene
    {
        public string name { get { return null; } }
    }

    public enum LoadSceneMode
    {
        Single = 0,
        Additive = 1
    }

    public static class SceneManager
    {
        public static Scene GetActiveScene() { return default; }
        public static void LoadScene(string sceneName) { }
        public static event UnityEngine.Events.UnityAction<Scene, LoadSceneMode> sceneLoaded;
    }
}

namespace UnityEngine.Events
{
    public delegate void UnityAction();

    public delegate void UnityAction<T0>(T0 arg0);

    public delegate void UnityAction<T0, T1>(T0 arg0, T1 arg1);

    public class UnityEventBase { }

    public class UnityEvent : UnityEventBase
    {
        public void AddListener(UnityAction call) { }
        public void Invoke() { }
    }

    public class UnityEvent<T0> : UnityEventBase
    {
        public void AddListener(UnityAction<T0> call) { }
        public void Invoke(T0 arg0) { }
    }
}

namespace UnityEngine.EventSystems
{
    public abstract class UIBehaviour : MonoBehaviour { }

    public class EventSystem : UIBehaviour { }

    public class BaseInputModule : UIBehaviour { }

    public class PointerInputModule : BaseInputModule { }

    public class StandaloneInputModule : PointerInputModule { }

    public abstract class BaseEventData { }

    public class PointerEventData : BaseEventData
    {
        public PointerEventData(EventSystem eventSystem) { }
    }

    public interface IEventSystemHandler { }

    public interface IPointerEnterHandler : IEventSystemHandler
    {
        void OnPointerEnter(PointerEventData eventData);
    }

    public interface IPointerExitHandler : IEventSystemHandler
    {
        void OnPointerExit(PointerEventData eventData);
    }
}

namespace UnityEngine.UI
{
    using UnityEngine.Events;
    using UnityEngine.EventSystems;

    public abstract class Graphic : UIBehaviour
    {
        public Color color { get; set; }
        public bool raycastTarget { get; set; }
        public RectTransform rectTransform { get { return null; } }
        public void CrossFadeColor(Color targetColor, float duration, bool ignoreTimeScale, bool useAlpha) { }
    }

    public abstract class MaskableGraphic : Graphic { }

    public class Image : MaskableGraphic
    {
        public Sprite sprite { get; set; }
    }

    public class RawImage : MaskableGraphic
    {
        public Texture texture { get; set; }
    }

    public class RectMask2D : UIBehaviour { }

    public struct ColorBlock
    {
        public Color normalColor { get; set; }
        public Color highlightedColor { get; set; }
        public Color pressedColor { get; set; }
        public Color selectedColor { get; set; }
        public Color disabledColor { get; set; }
        public float colorMultiplier { get; set; }
        public float fadeDuration { get; set; }
    }

    public class Selectable : UIBehaviour
    {
        public bool interactable { get; set; }
        public ColorBlock colors { get; set; }
        public Graphic targetGraphic { get; set; }
    }

    public class Button : Selectable
    {
        public class ButtonClickedEvent : UnityEvent { }
        public ButtonClickedEvent onClick { get { return new ButtonClickedEvent(); } }
    }

    public class CanvasScaler : UIBehaviour
    {
        public enum ScaleMode
        {
            ConstantPixelSize = 0,
            ScaleWithScreenSize = 1,
            ConstantPhysicalSize = 2
        }
        public ScaleMode uiScaleMode { get; set; }
        public Vector2 referenceResolution { get; set; }
        public float matchWidthOrHeight { get; set; }
    }

    public class GraphicRaycaster : UIBehaviour { }
}

namespace UnityEngine.TestTools
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class UnityTestAttribute : Attribute { }
}
