using System;
using System.Collections.Generic;
using Lingyan.Core.Localization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Lingyan.Core.Settings
{
    /// <summary>
    /// 玩家设置（与存档分离，损坏时允许回落默认——设置不承载游戏进度）。
    /// 无障碍基线：中文基准字号不小于 22 px；色盲双编码常开（符号 + 颜色）。
    /// </summary>
    public sealed class GameSettings
    {
        /// <summary>基准字号（px），只增不减的下限。</summary>
        public const int BaseFontPx = 22;

        public const int MinScalePercent = 100;
        public const int MaxScalePercent = 160;

        public const int VolumeStep = 10;

        /// <summary>
        /// 分辨率档位（宽 × 高）。(0, 0) 表示"随桌面"（原生分辨率，不强制切换）。
        /// </summary>
        public static readonly IReadOnlyList<(int Width, int Height)> ResolutionPresets =
            new List<(int, int)>
            {
                (0, 0),
                (1280, 720),
                (1600, 900),
                (1920, 1080),
                (2560, 1440),
                (3840, 2160),
            };

        [JsonProperty("locale")]
        public string LocaleCode { get; set; } = "zh-Hans";

        [JsonProperty("fontScalePercent")]
        public int FontScalePercent { get; set; } = 100;

        /// <summary>主音量（0–100，档距 10）。</summary>
        [JsonProperty("masterVolumePercent")]
        public int MasterVolumePercent { get; set; } = 80;

        [JsonProperty("fullscreen")]
        public bool Fullscreen { get; set; } = true;

        /// <summary>分辨率宽（0 = 随桌面）。只允许取 ResolutionPresets 中的档位。</summary>
        [JsonProperty("resolutionWidth")]
        public int ResolutionWidth { get; set; }

        [JsonProperty("resolutionHeight")]
        public int ResolutionHeight { get; set; }

        [JsonIgnore]
        public Locale Locale
        {
            get { return Locales.FromCode(LocaleCode); }
            set { LocaleCode = Locales.Code(value); }
        }

        /// <summary>实际基准字号：22 px × 缩放，向上取整，永不低于 22。</summary>
        [JsonIgnore]
        public int EffectiveBaseFontPx
        {
            get
            {
                int px = (int)Math.Ceiling(BaseFontPx * FontScalePercent / 100.0);
                return px < BaseFontPx ? BaseFontPx : px;
            }
        }

        [JsonIgnore]
        public float MasterVolume01
        {
            get { return MasterVolumePercent / 100f; }
        }

        /// <summary>当前分辨率在档位表中的下标；脏值回落 0（随桌面）。</summary>
        [JsonIgnore]
        public int ResolutionIndex
        {
            get
            {
                for (int i = 0; i < ResolutionPresets.Count; i++)
                {
                    if (ResolutionPresets[i].Width == ResolutionWidth
                        && ResolutionPresets[i].Height == ResolutionHeight)
                    {
                        return i;
                    }
                }
                return 0;
            }
        }

        public void ClampScale()
        {
            if (FontScalePercent < MinScalePercent) { FontScalePercent = MinScalePercent; }
            if (FontScalePercent > MaxScalePercent) { FontScalePercent = MaxScalePercent; }
        }

        /// <summary>把所有可越界字段拉回合法档位（音量取整到 10 的倍数并夹到 0–100）。</summary>
        public void ClampAll()
        {
            ClampScale();
            int volume = (int)Math.Round(MasterVolumePercent / (double)VolumeStep) * VolumeStep;
            if (volume < 0) { volume = 0; }
            if (volume > 100) { volume = 100; }
            MasterVolumePercent = volume;
            var preset = ResolutionPresets[ResolutionIndex];
            ResolutionWidth = preset.Width;
            ResolutionHeight = preset.Height;
        }

        /// <summary>切到下一档分辨率（到底绕回）。</summary>
        public void CycleResolution()
        {
            var next = ResolutionPresets[(ResolutionIndex + 1) % ResolutionPresets.Count];
            ResolutionWidth = next.Width;
            ResolutionHeight = next.Height;
        }

        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        /// <summary>宽容解析：设置文件坏了就回默认值，不阻塞进游戏。</summary>
        public static GameSettings ParseOrDefault(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) { return new GameSettings(); }
            try
            {
                JObject root = JObject.Parse(json);
                var settings = new GameSettings();
                string locale = root.Value<string>("locale");
                if (locale == "en" || locale == "zh-Hans") { settings.LocaleCode = locale; }
                JToken scale = root["fontScalePercent"];
                if (scale != null && scale.Type == JTokenType.Integer)
                {
                    settings.FontScalePercent = scale.Value<int>();
                }
                JToken volume = root["masterVolumePercent"];
                if (volume != null && volume.Type == JTokenType.Integer)
                {
                    settings.MasterVolumePercent = volume.Value<int>();
                }
                JToken fullscreen = root["fullscreen"];
                if (fullscreen != null && fullscreen.Type == JTokenType.Boolean)
                {
                    settings.Fullscreen = fullscreen.Value<bool>();
                }
                JToken width = root["resolutionWidth"];
                JToken height = root["resolutionHeight"];
                if (width != null && width.Type == JTokenType.Integer
                    && height != null && height.Type == JTokenType.Integer)
                {
                    settings.ResolutionWidth = width.Value<int>();
                    settings.ResolutionHeight = height.Value<int>();
                }
                settings.ClampAll();
                return settings;
            }
            catch (Exception)
            {
                return new GameSettings();
            }
        }
    }
}
