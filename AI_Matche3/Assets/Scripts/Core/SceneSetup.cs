using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Match3
{
    /// <summary>
    /// 纯代码搭建游戏场景：相机、棋盘、管理器、HUD、结算弹窗、开始界面。
    /// Build：编辑器菜单/旧流程使用，连同 GameManager 一起创建；
    /// BuildAround：场景中已由用户手动挂载 GameManager（空物体 + Inspector 拖入 GameConfig）时，
    /// 运行时由 GameBootstrap 调用，只补建其余部分。
    /// </summary>
    public static class SceneSetup
    {
        public static void Build(GameConfig config)
        {
            EnsureCamera(config);

            // ---------- 游戏根物体 ----------
            var gameRoot = new GameObject("Game");
            var gm = gameRoot.AddComponent<GameManager>();
            if (config != null) SetPrivateConfig(gm, config);

            BuildWorld(gameRoot, gm);
        }

        /// <summary>围绕场景中已有的 GameManager 补建相机/棋盘/UI/开始界面。</summary>
        public static void BuildAround(GameManager gm)
        {
            EnsureCamera(gm.Config);
            BuildWorld(gm.gameObject, gm);
        }

        static void EnsureCamera(GameConfig config)
        {
            // 先清掉场景里已有的 MainCamera 相机：双相机会让 Camera.main 选错对象，
            // 点击坐标换算错乱（表现为怎么点都没反应），且产生 2 个 AudioListener 警告
            foreach (var oldCam in Object.FindObjectsOfType<Camera>())
            {
                if (!oldCam.CompareTag("MainCamera")) continue;
                if (Application.isPlaying) Object.Destroy(oldCam.gameObject);
                else Object.DestroyImmediate(oldCam.gameObject);
            }

            var camGo = new GameObject("MainCamera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.16f, 0.18f, 0.26f);
            cam.nearClipPlane = -10f;
            cam.farClipPlane = 10f;
            if (Object.FindObjectOfType<AudioListener>() == null)
                camGo.AddComponent<AudioListener>();
            int bw = config != null ? config.boardWidth : 8;
            int bh = config != null ? config.boardHeight : 8;
            var fitter = camGo.AddComponent<CameraFitter>();
            fitter.SetBoardSize(bw, bh);
            cam.orthographicSize = Mathf.Max(bh * 0.5f + 1.1f, bw * 0.89f + 1.1f);
        }

        static void BuildWorld(GameObject gameRoot, GameManager gm)
        {
            var cfg = gm.Config;
            int bw = cfg != null ? cfg.boardWidth : 8;
            int bh = cfg != null ? cfg.boardHeight : 8;

            // ---------- 棋盘 ----------
            var boardGo = new GameObject("Board");
            boardGo.transform.position = Vector3.zero;
            var board = boardGo.AddComponent<Board>();

            // 棋盘背景占位板
            var bgGo = new GameObject("BoardBackground", typeof(SpriteRenderer));
            bgGo.transform.SetParent(boardGo.transform, false);
            bgGo.transform.localPosition = Vector3.zero;
            var bgSr = bgGo.GetComponent<SpriteRenderer>();
            bgSr.sprite = PlaceholderArt.WhiteSquare;
            bgSr.color = new Color(0.24f, 0.27f, 0.37f);
            bgSr.sortingOrder = -10;
            bgGo.transform.localScale = new Vector3(bw + 0.25f, bh + 0.25f, 1f);

            // ---------- UI ----------
            // uGUI 按钮必须有 EventSystem 才能响应点击
            if (Object.FindObjectOfType<EventSystem>() == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            var canvas = UIFactory.CreateCanvas("GameCanvas", 10);
            var hud = BuildHud(canvas, gameRoot.AddComponent<HudController>());

            // ---------- 底部广告功能按钮（看广告加步数 / 看广告打乱棋盘）----------
            BuildBottomBar(canvas, gm);

            // ---------- 开始界面（最后创建，盖在 HUD 之上）----------
            BuildStartScreen(canvas, gm);
        }

        static void SetPrivateConfig(GameManager gm, GameConfig config)
        {
            // config 是 [SerializeField] private，用反射注入，运行时/编辑器通用
            var field = typeof(GameManager).GetField("config",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            field?.SetValue(gm, config);
        }

        #region 开始界面

        static void BuildStartScreen(Canvas canvas, GameManager gm)
        {
            // 全屏底板：既是背景也是"点击任意位置"的点击接收区
            var screen = UIFactory.CreatePanel(canvas.gameObject.transform, "StartScreen",
                Vector2.zero, Vector2.one, Vector2.zero, new Color(0.07f, 0.08f, 0.13f, 1f));
            var catcher = screen.AddComponent<Button>();
            catcher.transition = Selectable.Transition.None; // 全屏点击不需要按钮高亮变色

            // 序列帧显示图（居中偏上，保持图片原始宽高比）
            var frameGo = new GameObject("Frame", typeof(RectTransform), typeof(Image));
            var frt = (RectTransform)frameGo.transform;
            frt.SetParent(screen.transform, false);
            frt.anchorMin = frt.anchorMax = new Vector2(0.5f, 0.62f);
            frt.sizeDelta = new Vector2(560f, 560f);
            var frameImg = frameGo.GetComponent<Image>();
            frameImg.raycastTarget = false; // 点击穿透到全屏接收区

            // 提示文字
            var promptGo = UIFactory.CreatePanel(screen.transform, "Prompt",
                new Vector2(0.5f, 0.18f), new Vector2(0.5f, 0.18f),
                new Vector2(900f, 120f), new Color(0, 0, 0, 0));
            var prompt = UIFactory.CreateLabel(promptGo, "点击任意位置进入游戏", 44,
                TextAnchor.MiddleCenter, Color.white);

            var ctrl = screen.AddComponent<StartScreenController>();
            ctrl.frameImage = frameImg;
            ctrl.promptLabel = prompt;
            ctrl.clickCatcher = catcher;
            ctrl.Setup(gm.Config, gm);
        }

        #endregion

        #region HUD

        static HudController BuildHud(Canvas canvas, HudController hud)
        {
            // ---- 顶部信息栏 ----
            var topBar = UIFactory.CreatePanel(canvas.transform, "TopBar",
                new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.zero,
                new Color(0f, 0f, 0f, 0.35f));
            var rt = (RectTransform)topBar.transform;
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(0f, 220f);

            hud.scoreLabel = CreateHudBox(topBar, "ScoreBox", new Vector2(0.22f, 0.5f),
                new Color(0.93f, 0.3f, 0.3f, 0.9f));
            hud.targetLabel = CreateHudBox(topBar, "TargetBox", new Vector2(0.78f, 0.5f),
                new Color(0.3f, 0.6f, 0.95f, 0.9f));
            hud.movesLabel = CreateHudBox(topBar, "MovesBox", new Vector2(0.5f, 0.5f),
                new Color(0.35f, 0.75f, 0.4f, 0.95f));
            hud.movesLabel.fontSize = 46;

            // ---- 结算弹窗 ----
            var panel = UIFactory.CreatePanel(canvas.transform, "ResultPanel",
                Vector2.zero, Vector2.one, Vector2.zero, new Color(0f, 0f, 0f, 0.72f));

            var titleGo = UIFactory.CreatePanel(panel.transform, "Title",
                new Vector2(0.5f, 0.68f), new Vector2(0.5f, 0.68f),
                new Vector2(700f, 180f), new Color(0, 0, 0, 0));
            hud.resultTitle = UIFactory.CreateLabel(titleGo, "", 72, TextAnchor.MiddleCenter, Color.white);

            var detailGo = UIFactory.CreatePanel(panel.transform, "Detail",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(820f, 240f), new Color(0, 0, 0, 0));
            hud.resultDetail = UIFactory.CreateLabel(detailGo, "", 40, TextAnchor.MiddleCenter, Color.white);

            hud.btnAdRevive = CreateResultButton(panel, "看广告 +5步", new Vector2(0.5f, 0.34f),
                new Color(1f, 0.75f, 0.25f), Color.black);
            hud.btnNext = CreateResultButton(panel, "下一关", new Vector2(0.5f, 0.34f),
                new Color(0.4f, 0.85f, 0.45f), Color.black);
            hud.btnRestart = CreateResultButton(panel, "重新开始", new Vector2(0.5f, 0.2f),
                new Color(0.9f, 0.9f, 0.9f), Color.black);

            hud.resultPanel = panel;

            // ---- Toast ----
            var toastGo = UIFactory.CreatePanel(canvas.transform, "Toast",
                new Vector2(0.5f, 0.28f), new Vector2(0.5f, 0.28f),
                new Vector2(860f, 120f), new Color(0f, 0f, 0f, 0.8f));
            hud.toast = toastGo;
            hud.toastLabel = UIFactory.CreateLabel(toastGo, "", 34, TextAnchor.MiddleCenter, Color.white);

            return hud;
        }

        static Text CreateHudBox(GameObject parent, string name, Vector2 anchor, Color bg)
        {
            var box = UIFactory.CreatePanel(parent.transform, name,
                anchor, anchor, new Vector2(300f, 150f), bg);
            return UIFactory.CreateLabel(box, "", 38, TextAnchor.MiddleCenter, Color.white);
        }

        static Button CreateResultButton(GameObject parent, string text, Vector2 anchor, Color bg, Color textColor)
        {
            var btn = UIFactory.CreateButton(parent, text, 38, null);
            var brt = (RectTransform)btn.transform;
            brt.anchorMin = brt.anchorMax = anchor;
            brt.sizeDelta = new Vector2(520f, 110f);
            btn.GetComponent<Image>().color = bg;
            btn.GetComponentInChildren<Text>().color = textColor;
            return btn;
        }

        #endregion

        #region 底部广告功能按钮

        static void BuildBottomBar(Canvas canvas, GameManager gm)
        {
            var btnMoves = UIFactory.CreateButton(canvas.gameObject,
                $"看广告 +{gm.Config.rewardedExtraMoves}步", 34,
                () => gm.ShowRewardedForExtraMoves());
            var rt = (RectTransform)btnMoves.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(-225f, 195f);
            rt.sizeDelta = new Vector2(420f, 110f);
            btnMoves.GetComponent<Image>().color = new Color(1f, 0.75f, 0.25f);

            var btnShuffle = UIFactory.CreateButton(canvas.gameObject,
                "看广告 打乱棋盘", 34,
                () => gm.ShowRewardedForShuffle());
            var rt2 = (RectTransform)btnShuffle.transform;
            rt2.anchorMin = rt2.anchorMax = new Vector2(0.5f, 0f);
            rt2.anchoredPosition = new Vector2(225f, 195f);
            rt2.sizeDelta = new Vector2(420f, 110f);
            btnShuffle.GetComponent<Image>().color = new Color(0.4f, 0.85f, 0.45f);
        }

        #endregion
    }
}
