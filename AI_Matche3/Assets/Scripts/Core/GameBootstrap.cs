using UnityEngine;

namespace Match3
{
    /// <summary>
    /// 运行时引导：即使打开的是完全空的场景，进入 Play 模式也能自动搭建游戏。
    /// 若场景中已经用编辑器菜单搭好了（存在 GameManager），则不重复创建。
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public class GameBootstrap : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoBoot()
        {
            if (Object.FindObjectOfType<GameManager>() != null) return;
            var go = new GameObject("GameBootstrap");
            go.AddComponent<GameBootstrap>();
            DontDestroyOnLoad(go);
        }

        void Awake()
        {
            if (FindObjectOfType<GameManager>() != null) return;
            var cfg = LoadConfigAsset();
            SceneSetup.Build(cfg);
        }

        static GameConfig LoadConfigAsset()
        {
            // 若工程里有制作好的 GameConfig 资产则使用，否则用代码默认值
            var assets = Resources.LoadAll<GameConfig>(string.Empty);
            if (assets != null && assets.Length > 0) return assets[0];
            return GameConfig.Default;
        }
    }
}
