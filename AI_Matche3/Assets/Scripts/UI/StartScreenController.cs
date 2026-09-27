using UnityEngine;
using UnityEngine.UI;

namespace Match3
{
    /// <summary>
    /// 开始界面：序列帧循环动画 + "点击任意位置进入游戏"提示 + 背景音乐。
    /// 由 SceneSetup 代码搭建并注入引用；点击任意位置后通知 GameManager 进入游戏。
    /// 序列帧图片与背景音乐均在 GameConfig 中配置，留空时使用占位资源。
    /// </summary>
    public class StartScreenController : MonoBehaviour
    {
        public Image frameImage;    // 序列帧显示图
        public Text promptLabel;    // "点击任意位置进入游戏"
        public Button clickCatcher; // 全屏点击接收

        Sprite[] _frames;
        float _fps;
        int _frameIndex;
        float _frameTimer;
        AudioSource _bgm;
        GameManager _gm;
        bool _entered;

        public void Setup(GameConfig cfg, GameManager gm)
        {
            _gm = gm;

            // 序列帧：优先用 GameConfig 里配置的图片，否则用占位色块演示
            _frames = (cfg.startScreenFrames != null && cfg.startScreenFrames.Length > 0)
                ? cfg.startScreenFrames
                : PlaceholderArt.StartScreenFrames();
            _fps = Mathf.Max(0.01f, cfg.startScreenFps);
            if (_frames.Length > 0)
            {
                frameImage.sprite = _frames[0];
                _frameIndex = 0;
            }
            frameImage.preserveAspect = true;

            // 背景音乐：优先用配置的音频，否则用占位旋律
            _bgm = gameObject.AddComponent<AudioSource>();
            _bgm.clip = cfg.backgroundMusic != null ? cfg.backgroundMusic : PlaceholderAudio.CreateLoop();
            _bgm.loop = true;
            _bgm.playOnAwake = false;
            _bgm.Play();

            clickCatcher.onClick.AddListener(EnterGame);
        }

        void Update()
        {
            // 序列帧循环推进
            if (_frames != null && _frames.Length > 1)
            {
                _frameTimer += Time.deltaTime;
                float dur = 1f / _fps;
                while (_frameTimer >= dur)
                {
                    _frameTimer -= dur;
                    _frameIndex = (_frameIndex + 1) % _frames.Length;
                    frameImage.sprite = _frames[_frameIndex];
                }
            }

            // 提示文字呼吸闪烁
            if (promptLabel != null)
            {
                var c = promptLabel.color;
                c.a = 0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * 3f);
                promptLabel.color = c;
            }
        }

        void EnterGame()
        {
            if (_entered) return;
            _entered = true;

            if (_bgm != null) _bgm.Stop();
            _gm.EnterGame();
            gameObject.SetActive(false);
        }
    }
}
