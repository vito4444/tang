using Lingyan.Core.Architecture;
using UnityEngine;

namespace Lingyan.Game.World3D
{
    /// <summary>
    /// 天光：一盏平行光按时辰摆位（角度/色/强度由 Core 的 SunMoon 计算，
    /// 契约"光源永在地平线以上"在 Core 层有测试）。附带环境光、雾与天色。
    /// </summary>
    public sealed class DayLightRig
    {
        private readonly Light _light;
        private readonly Transform _transform;

        public DayLightRig(Transform parent)
        {
            var go = new GameObject("SkyLight");
            go.transform.SetParent(parent, false);
            _transform = go.transform;
            _light = go.AddComponent<Light>();
            _light.type = LightType.Directional;
            _light.shadows = LightShadows.Soft;
            _light.shadowStrength = 0.75f;
        }

        /// <summary>按时辰摆光，并同步相机天色与雾。</summary>
        public void Apply(int hourIndex, Camera camera)
        {
            SkyLightState state = SunMoon.For(hourIndex);

            // 仰角 E、方位 A（0=北 90=东）：光自该方向射向场景。
            // Euler(E, A+180, 0) 使 forward.y = -sin(E) < 0（照向地面）。
            _transform.rotation = Quaternion.Euler(
                (float)state.ElevationDeg, (float)state.AzimuthDeg + 180f, 0f);
            _light.color = new Color((float)state.R, (float)state.G, (float)state.B);
            // 白天提一成半：烘焙 AO 已把暗部压进贴图，直射不加受光面就闷（第二十轮实测）
            _light.intensity = (float)state.Intensity * (state.IsMoon ? 1.0f : 1.15f);
            _light.shadowStrength = state.IsMoon ? 0.75f : 0.62f; // 影里要能辨构件

            Color lightColor = _light.color;
            float ambientBoost = state.IsMoon ? 0.55f : 1.12f; // 背光宅第二十轮实测死黑，抬环境
            Color ambient = new Color(
                lightColor.r * (float)state.Ambient * ambientBoost + (state.IsMoon ? 0.02f : 0.05f),
                lightColor.g * (float)state.Ambient * ambientBoost + (state.IsMoon ? 0.02f : 0.05f),
                lightColor.b * (float)state.Ambient * (ambientBoost + 0.05f) + (state.IsMoon ? 0.04f : 0.07f));

            // 三色环境光（写实化）：天光偏冷、地光偏土色反照，立体感来自色温差
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(
                ambient.r * 0.95f, ambient.g * 1.0f, ambient.b * 1.18f);
            RenderSettings.ambientEquatorColor = ambient;
            RenderSettings.ambientGroundColor = new Color(
                ambient.r * 0.82f, ambient.g * 0.70f, ambient.b * 0.52f);

            Color sky = state.IsMoon
                ? new Color(0.055f, 0.075f, 0.125f)
                : Color.Lerp(
                    new Color(0.78f, 0.66f, 0.52f),   // 晨昏暖霞
                    new Color(0.63f, 0.72f, 0.80f),   // 正午淡蓝
                    (float)((state.ElevationDeg - SunMoon.HorizonMarginDeg)
                        / (SunMoon.NoonElevationDeg - SunMoon.HorizonMarginDeg)));

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogColor = sky;
            RenderSettings.fogDensity = state.IsMoon ? 0.010f : 0.006f;

            if (camera != null)
            {
                camera.backgroundColor = sky;
            }
        }
    }
}
