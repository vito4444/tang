using System;
using System.Collections;
using System.IO;
using Lingyan.Core.Characters;
using Lingyan.Core.Saves;
using Lingyan.Game;
using Lingyan.Game.Screenshot;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Lingyan.PlayTests
{
    /// <summary>
    /// 全屏巡游截图（规格第十四节：自动化验收截图，一条命令、可比较）。
    /// 输出 artifacts/unity-shots/*.png；断言文件真实生成且非空壳
    /// （渲染真的发生了），画面质量由人与指标脚本复核。
    /// 需要图形设备：CI 里用 xvfb 跑，不加 -nographics。
    /// </summary>
    public class ScreenshotTests
    {
        private const int MinBytes = 20_000;

        [UnityTest]
        public IEnumerator CaptureAllScreens()
        {
            SceneManager.LoadScene("Boot");
            yield return null;
            yield return null;
            yield return null;

            GameController c = UnityEngine.Object.FindFirstObjectByType<GameController>();
            Assert.That(c, Is.Not.Null, "GameController 未自举");

            // 主菜单
            yield return Snap(c, "01_menu");

            // 建角
            c.GoCreation();
            yield return null;
            yield return Snap(c, "02_creation");

            // 书房（明镜新档）
            CharacterDraft draft = CharacterCreationRules.NewDraft(ProtagonistId.MingJing);
            SaveData save = SaveFactory.NewGame(draft, DateTime.UtcNow);
            c.GoStudy(save);
            yield return null;
            yield return Snap(c, "03_study");

            // 坊景 · 巳时全景（自南望北：宅院、井亭、主巷同框）
            c.GoWard(save);
            yield return null;
            yield return null;
            SetRig(c, new Vector3(0f, 1.2f, 3f), yaw: -14f, pitch: 42f, distance: 34f);
            yield return Snap(c, "04_ward_noon_wide");

            // 坊景 · 南门楼近景（看鸱尾与门扇）
            SetRig(c, new Vector3(0f, 2.5f, -19f), yaw: 197f, pitch: 22f, distance: 15f);
            yield return Snap(c, "05_ward_gate");

            // 坊景 · 桓宅近景（看铺作层、下昂、直棂窗）
            SetRig(c, new Vector3(-12f, 2.6f, 7f), yaw: 160f, pitch: 24f, distance: 15f);
            yield return Snap(c, "06_ward_house");

            // 坊景 · 子夜（月光、闭门、武侯巡街）
            save.Date.HourIndex = 0;
            c.GoWard(save);
            yield return null;
            SetRig(c, new Vector3(0f, 1.2f, 0f), yaw: -170f, pitch: 36f, distance: 27f);
            yield return Snap(c, "07_ward_midnight");

            // 设置
            c.GoSettings();
            yield return null;
            yield return Snap(c, "08_settings");
        }

        private static void SetRig(GameController c, Vector3 target, float yaw, float pitch, float distance)
        {
            c.Orbit.Target = target;
            c.Orbit.Yaw = yaw;
            c.Orbit.Pitch = pitch;
            c.Orbit.Distance = distance;
            c.Orbit.ApplyTransform();
        }

        private static IEnumerator Snap(GameController c, string name)
        {
            yield return null;
            string path = ScreenshotRig.Capture(c, name);
            var info = new FileInfo(path);
            Assert.That(info.Exists, Is.True, name + " 未落盘");
            Assert.That(info.Length, Is.GreaterThan(MinBytes),
                name + " 文件过小（" + info.Length + " B），疑似黑屏/未渲染");
        }
    }
}
