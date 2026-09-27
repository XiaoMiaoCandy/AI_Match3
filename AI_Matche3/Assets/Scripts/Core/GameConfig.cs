using UnityEngine;

namespace Match3
{
    /// <summary>
    /// 游戏全局配置。可通过 Create 菜单生成资产，也可运行时使用 Default。
    /// </summary>
    [CreateAssetMenu(fileName = "GameConfig", menuName = "Match3/Game Config")]
    public class GameConfig : ScriptableObject
    {
        [Header("棋盘")]
        public int boardWidth = 8;
        public int boardHeight = 8;
        [Range(3, 8)] public int tileTypes = 6;

        [Header("关卡")]
        public int startMoves = 20;
        public int targetScore = 3000;
        public int scorePerTile = 10;
        public int rewardedExtraMoves = 5;

        [Header("动画时长(秒)")]
        public float swapTime = 0.18f;
        public float fallTimePerCell = 0.001f;
        public float clearTime = 0.2f;
        public float refillMinTime = 0.18f;

        /// <summary>运行时默认配置（无需制作资产即可运行）。</summary>
        public static GameConfig Default
        {
            get
            {
                var c = CreateInstance<GameConfig>();
                c.hideFlags = HideFlags.HideAndDontSave;
                return c;
            }
        }
    }
}
