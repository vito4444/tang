using UnityEngine;
using UnityEngine.SceneManagement;

namespace Lingyan.Game
{
    /// <summary>
    /// 启动入口。场景零依赖：Boot 场景为空，一切由代码构建，
    /// 场景文件因此不携带任何 GUID 引用，手工维护零风险。
    /// </summary>
    public static class GameBootstrap
    {
        // RuntimeInitializeOnLoadMethod 每次进入播放只触发一次。玩家构建里首场景即 Boot，
        // 直接自举；播放模式测试先进入测试运行器的 InitTestScene、之后才 LoadScene("Boot")，
        // 故同时订阅 sceneLoaded，Boot 何时加载何时自举。幂等由"已有控制器则跳过"保证。
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Init()
        {
            TryBoot(SceneManager.GetActiveScene());
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            TryBoot(scene);
        }

        private static void TryBoot(Scene scene)
        {
            if (scene.name != "Boot")
            {
                return;
            }
            if (Object.FindFirstObjectByType<GameController>() != null)
            {
                return;
            }
            var root = new GameObject("Lingyan.Game");
            Object.DontDestroyOnLoad(root);
            root.AddComponent<GameController>();
        }
    }
}
