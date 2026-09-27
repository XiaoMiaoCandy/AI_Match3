using System;
using UnityEngine;

namespace Match3
{
    public enum GameState
    {
        Playing,
        Won,
        Lost
    }

    /// <summary>
    /// 游戏流程：关卡初始化、步数、分数、胜负判定，并在合适时机插入广告。
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [SerializeField] GameConfig config;

        public GameConfig Config => config;
        public GameState State { get; private set; }
        public int Score { get; private set; }
        public int MovesLeft { get; private set; }
        public int Target => config.targetScore;

        Board _board;
        bool _validSwapConsumed; // 本次交换是否已真正消耗步数
        int _gamesPlayed;

        // ---------- UI 事件 ----------
        public event Action OnGameStart;
        public event Action OnScoreChanged;
        public event Action OnMovesChanged;
        public event Action OnGameWin;
        public event Action OnGameLose;
        public event Action<int> OnShuffled;
        public event Action<string> OnToast;

        void Awake()
        {
            Instance = this;
            if (config == null)
                Debug.LogError("[GameManager] 未引用 GameConfig：请创建 GameConfig 资产（Project 窗口右键 Create → Match3 → Game Config），并拖到本物体 Inspector 的 Config 字段上。");
        }

        void Start()
        {
            if (config == null) return;

            _board = FindObjectOfType<Board>();
            if (_board == null)
            {
                Debug.LogError("[GameManager] 场景中找不到 Board（应由 GameBootstrap 自动补建）。");
                return;
            }
            _board.Init(config);
            _board.InputEnabled = false; // 开始界面点击后才开放输入
            _board.OnSwapMade += HandleSwapMade;
            _board.OnMatched += HandleMatched;
            _board.OnBoardSettled += HandleBoardSettled;
            _board.OnShuffled += () => OnShuffled?.Invoke(Score);

            AdManager.Initialize();
            // 不在此处自动开局：等待开始界面（StartScreenController）点击后调用 EnterGame
        }

        void OnDestroy()
        {
            if (_board != null)
            {
                _board.OnSwapMade -= HandleSwapMade;
                _board.OnMatched -= HandleMatched;
                _board.OnBoardSettled -= HandleBoardSettled;
            }
        }

        /// <summary>由开始界面调用：点击任意位置后正式进入游戏。</summary>
        public void EnterGame()
        {
            StartGame();
        }

        public void StartGame()
        {
            StartCoroutine(RestartRoutine());
        }

        System.Collections.IEnumerator RestartRoutine()
        {
            Score = 0;
            MovesLeft = config.startMoves;
            State = GameState.Playing;
            _validSwapConsumed = false;
            _board.InputEnabled = false;
            yield return _board.Rebuild();
            _board.InputEnabled = true;
            OnGameStart?.Invoke();
            OnScoreChanged?.Invoke();
            OnMovesChanged?.Invoke();
        }

        #region 玩法事件

        void HandleSwapMade()
        {
            // 预扣一步；若交换无效，结算后退还
            _validSwapConsumed = false;
            MovesLeft = Mathf.Max(0, MovesLeft - 1);
            OnMovesChanged?.Invoke();
        }

        void HandleMatched(int count, int chain)
        {
            _validSwapConsumed = true;
            // 连锁层数越高加成越多：基础分 * 连锁倍数
            int gain = count * config.scorePerTile * chain;
            Score += gain;
            OnScoreChanged?.Invoke();
        }

        void HandleBoardSettled()
        {
            if (State != GameState.Playing) return;

            if (!_validSwapConsumed)
            {
                // 无效交换退还步数
                MovesLeft++;
                OnMovesChanged?.Invoke();
                return;
            }

            if (Score >= config.targetScore)
            {
                State = GameState.Won;
                _board.InputEnabled = false;
                _gamesPlayed++;
                OnGameWin?.Invoke();
            }
            else if (MovesLeft <= 0)
            {
                State = GameState.Lost;
                _board.InputEnabled = false;
                OnGameLose?.Invoke();
            }
        }

        #endregion

        #region 广告入口（由 UI 按钮 / 结算调用）

        /// <summary>看广告加步数（游戏内按钮 / 结算页续命通用）。</summary>
        public void ShowRewardedForExtraMoves()
        {
            _board.InputEnabled = false; // 看广告期间锁定棋盘输入
            AdManager.ShowRewarded("extra_moves", success =>
            {
                if (!success)
                {
                    OnToast?.Invoke("广告未播放完成，未获得奖励");
                    if (State == GameState.Playing) _board.InputEnabled = true;
                    return;
                }
                MovesLeft += config.rewardedExtraMoves;
                State = GameState.Playing;
                _board.InputEnabled = true;
                OnMovesChanged?.Invoke();
            });
        }

        /// <summary>游戏内“看广告打乱棋盘”按钮。</summary>
        public void ShowRewardedForShuffle()
        {
            _board.InputEnabled = false;
            AdManager.ShowRewarded("shuffle_board", success =>
            {
                if (!success)
                {
                    OnToast?.Invoke("广告未播放完成，未获得奖励");
                    if (State == GameState.Playing) _board.InputEnabled = true;
                    return;
                }
                StartCoroutine(ShuffleRoutine());
            });
        }

        System.Collections.IEnumerator ShuffleRoutine()
        {
            yield return _board.ShuffleWithBusy();
            if (State == GameState.Playing) _board.InputEnabled = true;
            OnToast?.Invoke("棋盘已打乱");
        }

        /// <summary>胜利后开始下一关（先插屏）。</summary>
        public void NextLevel()
        {
            AdManager.ShowInterstitial("between_levels", () => StartGame());
        }

        #endregion
    }
}
