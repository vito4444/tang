using UnityEngine;

namespace Lingyan.Game.World3D
{
    /// <summary>
    /// 相机水墨后处理：暗角 + 轻降饱和 + 暖纸偏色（Shaders/InkPost.shader）。
    /// shader 缺席时直通不挡画面（响亮记一条错误，不静默假装生效）。
    /// </summary>
    public sealed class InkPostEffect : MonoBehaviour
    {
        private Material _material;
        private bool _warned;

        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (_material == null)
            {
                // Resources 直取（随包必含）；Find 兜底（第十七轮：仅 Find 会被玩家包裁剪）
                Shader shader = Resources.Load<Shader>("Shaders/InkPost");
                if (shader == null) { shader = Shader.Find("Lingyan/InkPost"); }
                if (shader == null)
                {
                    if (!_warned)
                    {
                        _warned = true;
                        Debug.LogError("[Lingyan] InkPost shader 缺失，后处理直通");
                    }
                    Graphics.Blit(source, destination);
                    return;
                }
                _material = new Material(shader);
            }
            Graphics.Blit(source, destination, _material);
        }
    }
}
