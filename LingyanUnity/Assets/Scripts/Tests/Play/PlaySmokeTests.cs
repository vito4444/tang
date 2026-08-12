using System.Collections;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Lingyan.PlayTests
{
    /// <summary>
    /// 播放模式冒烟：Boot 场景自举出主菜单，标题可见、按钮可用。
    /// 断言契约（画面上有可读的标题字），不断言实现细节。
    /// </summary>
    public class PlaySmokeTests
    {
        [UnityTest]
        public IEnumerator Boot_BringsUpMainMenu()
        {
            SceneManager.LoadScene("Boot");
            yield return null;
            yield return null;
            yield return null;

            var controller = Object.FindFirstObjectByType<Lingyan.Game.GameController>();
            Assert.That(controller, Is.Not.Null, "GameController 未自举");

            TextMeshProUGUI[] texts = Object.FindObjectsByType<TextMeshProUGUI>(
                FindObjectsSortMode.None);
            Assert.That(texts.Length, Is.GreaterThan(3), "主菜单应有多段文字");

            bool hasTitle = texts.Any(t => t.text.Contains("凌烟") || t.text.Contains("LINGYAN"));
            Assert.That(hasTitle, Is.True, "画面上应出现标题「凌烟 / LINGYAN」");

            var buttons = Object.FindObjectsByType<UnityEngine.UI.Button>(
                FindObjectsSortMode.None);
            Assert.That(buttons.Count(b => b.interactable), Is.GreaterThanOrEqualTo(3),
                "至少应有 新的仕途/设置/退出/语言 等可点按钮");
        }
    }
}
