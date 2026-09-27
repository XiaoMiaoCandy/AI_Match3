using UnityEngine;

namespace Match3
{
    /// <summary>
    /// 运行时引导。配置不再由脚本自动生成：
    /// 场景中必须已有用户手动创建的空物体 + GameManager 组件，并在 Inspector 上拖入 GameConfig 资产；
    /// 本类只负责把相机/棋盘/UI/开始界面等其余部分自动补建齐全。
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public class GameBootstrap : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoBoot()
        {
            if (Object.FindObjectOfType<GameManager>() == null)
            {
                Debug.LogError("[GameBootstrap] 场景中没有 GameManager：请创建一个空物体并挂载 GameManager 组件，" +
                               "再把 GameConfig 资产拖到其 Inspector 的 Config 字段上。" +
                               "（GameConfig 资产：Project 窗口右键 Create → Match3 → Game Config，或菜单 Tools/Match3/生成 GameConfig 配置资产）");
                return;
            }
            var go = new GameObject("GameBootstrap");
            go.AddComponent<GameBootstrap>();
        }

        void Awake()
        {
            var gm = Object.FindObjectOfType<GameManager>();
            if (gm == null || gm.Config == null) return; // AutoBoot / GameManager 已给出错误提示
            if (Object.FindObjectOfType<Board>() != null) return; // 场景已完整（如 Tools/Match3 菜单搭建并保存过）

            SceneSetup.BuildAround(gm);
        }
    }
}
