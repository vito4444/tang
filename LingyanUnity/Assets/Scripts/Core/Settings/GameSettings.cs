using System;
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

        [JsonProperty("locale")]
        public string LocaleCode { get; set; } = "zh-Hans";

        [JsonProperty("fontScalePercent")]
        public int FontScalePercent { get; set; } = 100;

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

        public void ClampScale()
        {
            if (FontScalePercent < MinScalePercent) { FontScalePercent = MinScalePercent; }
            if (FontScalePercent > MaxScalePercent) { FontScalePercent = MaxScalePercent; }
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
                settings.ClampScale();
                return settings;
            }
            catch (Exception)
            {
                return new GameSettings();
            }
        }
    }
}
