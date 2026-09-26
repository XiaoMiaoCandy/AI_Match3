#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace Match3
{
    /// <summary>
    /// 编辑器一键搭建/清空场景。搭建后保存场景，即可替代运行时自动引导。
    /// </summary>
    public static class Match3SceneBuilder
    {
        const string ScenePath = "Assets/Scenes/Match3Game.unity";

        [MenuItem("Tools/Match3/搭建游戏场景 #&b")]
        public static void BuildScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cfg = LoadOrCreateConfig();
            SceneSetup.Build(cfg);

            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"[Match3] 游戏场景已搭建并保存：{ScenePath}");
        }

        [MenuItem("Tools/Match3/清空为空白场景")]
        public static void ResetScene()
        {
            if (!EditorUtility.DisplayDialog("清空场景",
                "确定把当前场景清空为空白场景吗？（不会删除磁盘文件）", "确定", "取消"))
                return;
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        [MenuItem("Tools/Match3/生成 GameConfig 配置资产")]
        public static void CreateConfigAsset()
        {
            LoadOrCreateConfig();
        }

        static GameConfig LoadOrCreateConfig()
        {
            const string dir = "Assets/Resources";
            const string path = dir + "/GameConfig.asset";
            var existing = AssetDatabase.LoadAssetAtPath<GameConfig>(path);
            if (existing != null) return existing;

            if (!AssetDatabase.IsValidFolder(dir))
                AssetDatabase.CreateFolder("Assets", "Resources");

            var cfg = ScriptableObject.CreateInstance<GameConfig>();
            AssetDatabase.CreateAsset(cfg, path);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Match3] 已生成配置资产：{path}");
            return cfg;
        }
    }
}
#endif
