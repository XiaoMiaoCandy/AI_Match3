using UnityEngine;
using UnityEngine.UI;

namespace Match3
{
    /// <summary>
    /// 游戏内 HUD 与结算弹窗。所有引用由 SceneSetup 构建时直接赋值，
    /// 不依赖序列化，保证纯代码搭建的场景能直接运行。
    /// </summary>
    public class HudController : MonoBehaviour
    {
        public Text scoreLabel;
        public Text targetLabel;
        public Text movesLabel;

        public GameObject resultPanel;
        public Text resultTitle;
        public Text resultDetail;
        public Button btnAdRevive;   // 失败页：看广告加步数
        public Button btnRestart;    // 重开本关
        public Button btnNext;       // 胜利：下一关

        public GameObject toast;
        public Text toastLabel;

        GameManager _gm;
        float _toastTimer;

        void Start()
        {
            _gm = GameManager.Instance;

            _gm.OnGameStart += HideResult;
            _gm.OnScoreChanged += RefreshScore;
            _gm.OnMovesChanged += RefreshMoves;
            _gm.OnGameWin += ShowWin;
            _gm.OnGameLose += ShowLose;
            _gm.OnToast += ShowToast;

            btnAdRevive.onClick.AddListener(OnAdRevive);
            btnRestart.onClick.AddListener(() => _gm.StartGame());
            btnNext.onClick.AddListener(() => _gm.NextLevel());

            resultPanel.SetActive(false);
            toast.SetActive(false);
            RefreshScore();
            RefreshMoves();
        }

        void OnDestroy()
        {
            if (_gm == null) return;
            _gm.OnGameStart -= HideResult;
            _gm.OnScoreChanged -= RefreshScore;
            _gm.OnMovesChanged -= RefreshMoves;
            _gm.OnGameWin -= ShowWin;
            _gm.OnGameLose -= ShowLose;
            _gm.OnToast -= ShowToast;
        }

        void Update()
        {
            if (_toastTimer > 0f)
            {
                _toastTimer -= Time.deltaTime;
                if (_toastTimer <= 0f && toast != null) toast.SetActive(false);
            }
        }

        void RefreshScore()
        {
            if (_gm == null) return;
            scoreLabel.text = $"分数\n{_gm.Score}";
            targetLabel.text = $"目标\n{_gm.Target}";
        }

        void RefreshMoves()
        {
            if (_gm == null) return;
            movesLabel.text = $"步数\n{_gm.MovesLeft}";
            movesLabel.color = _gm.MovesLeft <= 5 ? new Color(1f, 0.45f, 0.4f) : Color.white;
        }

        void ShowWin()
        {
            resultTitle.text = "过关！";
            resultTitle.color = new Color(0.4f, 0.85f, 0.4f);
            resultDetail.text = $"得分 {_gm.Score} / 目标 {_gm.Target}";
            btnAdRevive.gameObject.SetActive(false);
            btnRestart.gameObject.SetActive(true);
            btnNext.gameObject.SetActive(true);
            resultPanel.SetActive(true);
        }

        void ShowLose()
        {
            resultTitle.text = "挑战失败";
            resultTitle.color = new Color(1f, 0.5f, 0.45f);
            resultDetail.text = $"得分 {_gm.Score} / 目标 {_gm.Target}\n看广告可加 {_gm.Config.rewardedExtraMoves} 步继续";
            btnAdRevive.gameObject.SetActive(true);
            btnRestart.gameObject.SetActive(true);
            btnNext.gameObject.SetActive(false);
            resultPanel.SetActive(true);
        }

        void OnAdRevive()
        {
            resultPanel.SetActive(false);
            _gm.ShowRewardedForExtraMoves();
        }

        void ShowToast(string msg)
        {
            toastLabel.text = msg;
            toast.SetActive(true);
            _toastTimer = 2f;
        }

        /// <summary>重开/续命继续时由外部关闭结算面板（GameManager 状态变化时调用）。</summary>
        public void HideResult()
        {
            if (resultPanel != null) resultPanel.SetActive(false);
        }
    }
}
