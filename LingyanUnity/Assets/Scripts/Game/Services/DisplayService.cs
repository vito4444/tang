using Lingyan.Core.Settings;
using UnityEngine;

namespace Lingyan.Game.Services
{
    /// <summary>把显示设置落到 Screen（编辑器与批处理下自然无效，不需分支）。</summary>
    public static class DisplayService
    {
        public static void Apply(GameSettings settings)
        {
            if (settings.ResolutionWidth > 0 && settings.ResolutionHeight > 0)
            {
                Screen.SetResolution(
                    settings.ResolutionWidth, settings.ResolutionHeight, settings.Fullscreen);
            }
            else
            {
                // 随桌面：不强改尺寸，只应用全屏/窗口
                Screen.fullScreen = settings.Fullscreen;
            }
        }
    }
}
