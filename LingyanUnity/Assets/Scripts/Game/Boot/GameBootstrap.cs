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
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Init()
        {
            if (SceneManager.GetActiveScene().name != "Boot")
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
