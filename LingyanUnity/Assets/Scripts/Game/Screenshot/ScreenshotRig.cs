using System.IO;
using UnityEngine;

namespace Lingyan.Game.Screenshot
{
    /// <summary>
    /// 引擎内截图设备：相机渲到 RenderTexture 再落 PNG，
    /// 分辨率与窗口无关（无头 CI 下同样 1920×1080）。
    /// UI 画布临时切到 ScreenSpaceCamera 一并入镜，截完恢复 Overlay。
    /// </summary>
    public static class ScreenshotRig
    {
        public static string OutputDir
        {
            get
            {
                return Path.GetFullPath(Path.Combine(
                    Application.dataPath, "..", "..", "artifacts", "unity-shots"));
            }
        }

        public static string Capture(GameController c, string name, int width = 1920, int height = 1080)
        {
            Camera cam = c.MainCamera;
            Canvas canvas = c.RootCanvas;

            RenderMode previousMode = canvas.renderMode;
            int previousMask = cam.cullingMask;

            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
            canvas.planeDistance = 1f;
            cam.cullingMask = ~0; // UI 层要进相机；纯 UI 屏无 3D 物体，全开无副作用

            var rt = new RenderTexture(width, height, 24);
            cam.targetTexture = rt;
            cam.Render();

            RenderTexture.active = rt;
            var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new UnityEngine.Rect(0, 0, width, height), 0, 0);
            tex.Apply();

            cam.targetTexture = null;
            RenderTexture.active = null;
            rt.Release();
            canvas.renderMode = previousMode;
            cam.cullingMask = previousMask;

            Directory.CreateDirectory(OutputDir);
            string path = Path.Combine(OutputDir, name + ".png");
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.Destroy(tex);
            Debug.Log("[Screenshot] " + path);
            return path;
        }
    }
}
