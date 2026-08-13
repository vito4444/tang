using System.Linq;
using Lingyan.Core.Localization;
using Lingyan.Core.Officials;
using Lingyan.Core.Saves;
using Lingyan.Game.Services;
using Lingyan.Game.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Lingyan.Game
{
    /// <summary>
    /// 全局控制器：服务装配、画布搭建、屏幕状态机。
    /// 屏幕全部代码构建；语言或字号变更时整屏重建取词。
    /// </summary>
    public sealed class GameController : MonoBehaviour
    {
        public LocalizationService L10n { get; private set; }
        public SettingsService Settings { get; private set; }
        public SaveService Saves { get; private set; }
        public FontService Fonts { get; private set; }

        /// <summary>当前进行中的存档（进入书房后非空）。</summary>
        public SaveData ActiveSave { get; private set; }

        /// <summary>主菜单顶部待展示的错误（如读档失败），展示一次后清空。</summary>
        public string PendingErrorKey { get; private set; }

        private enum ScreenId { MainMenu, Creation, Study, Settings, Ward }

        private ScreenId _screen = ScreenId.MainMenu;
        private ScreenId _settingsReturnTo = ScreenId.MainMenu;
        private RectTransform _screenRoot;

        private void Awake()
        {
            Settings = new SettingsService();
            Settings.Load();

            L10n = new LocalizationService();
            L10n.Load();
            L10n.SetLocale(Settings.Current.Locale);
            L10n.LocaleChanged += Rebuild;

            Fonts = new FontService();
            Fonts.Load();

            Saves = new SaveService();

            BuildCameraAndCanvas();
            UiKit.Configure(Fonts, Settings.Current.EffectiveBaseFontPx);
            SmokeLog();
        }

        private void Start()
        {
            Rebuild();
        }

        private void BuildCameraAndCanvas()
        {
            var camGo = new GameObject("Main Camera");
            camGo.transform.SetParent(transform);
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = InkPalette.Void;
            cam.cullingMask = 0;
            cam.orthographic = true;

            var esGo = new GameObject("EventSystem");
            esGo.transform.SetParent(transform);
            esGo.AddComponent<EventSystem>();
            esGo.AddComponent<StandaloneInputModule>();

            var canvasGo = new GameObject("Canvas");
            canvasGo.transform.SetParent(transform);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            var rootGo = new GameObject("ScreenRoot");
            _screenRoot = rootGo.AddComponent<RectTransform>();
            _screenRoot.SetParent(canvasGo.transform, false);
            _screenRoot.anchorMin = Vector2.zero;
            _screenRoot.anchorMax = Vector2.one;
            _screenRoot.offsetMin = Vector2.zero;
            _screenRoot.offsetMax = Vector2.zero;
        }

        private void SmokeLog()
        {
            Debug.Log("[Smoke] locale=" + Locales.Code(L10n.Locale)
                + " strings=" + L10n.Catalog.Count
                + " glossary=" + L10n.Glossary.Entries.Count
                + " ladderOffices=" + OfficialLadders.All.Count()
                + " sanguan=" + SanGuanTable.All.Count()
                + " font=" + (Fonts.Main != null ? "ok" : "MISSING")
                + " basePx=" + Settings.Current.EffectiveBaseFontPx
                + " hasSave=" + Saves.HasSave
                + " dataPath=" + Application.persistentDataPath);
        }

        // ---- 屏幕切换 ----

        public void GoMainMenu()
        {
            _screen = ScreenId.MainMenu;
            Rebuild();
        }

        public void GoCreation()
        {
            _screen = ScreenId.Creation;
            Rebuild();
        }

        public void GoStudy(SaveData save)
        {
            ActiveSave = save;
            _screen = ScreenId.Study;
            Rebuild();
        }

        public void GoWard(SaveData save)
        {
            ActiveSave = save;
            _screen = ScreenId.Ward;
            Rebuild();
        }

        public void GoSettings()
        {
            if (_screen != ScreenId.Settings)
            {
                _settingsReturnTo = _screen;
            }
            _screen = ScreenId.Settings;
            Rebuild();
        }

        public void BackFromSettings()
        {
            Settings.Save();
            UiKit.Configure(Fonts, Settings.Current.EffectiveBaseFontPx);
            _screen = _settingsReturnTo;
            Rebuild();
        }

        /// <summary>读档入书房；坏档时回主菜单响亮报错。</summary>
        public void ContinueCareer()
        {
            try
            {
                SaveData save = Saves.Load();
                GoStudy(save);
            }
            catch (SaveException e)
            {
                Debug.LogError("[Lingyan] 读档失败(" + e.GetType().Name + "): " + e.Message);
                PendingErrorKey = e.ReasonKey;
                GoMainMenu();
            }
        }

        public void ClearPendingError()
        {
            PendingErrorKey = null;
        }

        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void Rebuild()
        {
            if (_screenRoot == null) { return; }
            for (int i = _screenRoot.childCount - 1; i >= 0; i--)
            {
                Destroy(_screenRoot.GetChild(i).gameObject);
            }

            switch (_screen)
            {
                case ScreenId.Creation:
                    CharacterCreationScreen.Build(this, _screenRoot);
                    break;
                case ScreenId.Study:
                    StudyScreen.Build(this, _screenRoot);
                    break;
                case ScreenId.Settings:
                    SettingsScreen.Build(this, _screenRoot);
                    break;
                case ScreenId.Ward:
                    WardScreen.Build(this, _screenRoot);
                    break;
                default:
                    MainMenuScreen.Build(this, _screenRoot);
                    break;
            }
        }
    }
}
