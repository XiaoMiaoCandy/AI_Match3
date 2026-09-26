using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Match3
{
    /// <summary>
    /// 模拟广告的全屏遮罩面板（仅 MockAdService 使用）。
    /// 接入真实 SDK 后整个文件不会再被使用。
    /// </summary>
    public class AdMockOverlay : MonoBehaviour
    {
        static Canvas _canvas;
        static AdMockOverlay _host;

        public static Canvas GetCanvas()
        {
            if (_canvas != null) return _canvas;
            var go = new GameObject("MockAdCanvas");
            _canvas = go.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 500; // 盖过游戏 UI
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            _host = go.AddComponent<AdMockOverlay>();
            DontDestroyOnLoad(go);
            return _canvas;
        }

        /// <summary>
        /// 显示一个自动倒计时的全屏广告面板。
        /// 倒计时结束 = 完整观看（true）；点按钮 = 提前关闭（false）。
        /// </summary>
        public static void Show(string text, string buttonText, float autoSeconds, Action<bool> onDone)
        {
            GetCanvas();
            var panel = UIFactory.CreatePanel(_canvas.transform, "MockAdPanel",
                Vector2.zero, Vector2.one, Vector2.zero, new Color(0.05f, 0.05f, 0.08f, 0.98f));

            UIFactory.CreateLabel(panel, text, 40, TextAnchor.MiddleCenter, Color.white);

            bool finished = false;
            var btn = UIFactory.CreateButton(panel, buttonText, 30, () =>
            {
                if (finished) return;
                finished = true;
                Destroy(panel);
                onDone?.Invoke(false);
            });
            var brt = (RectTransform)btn.transform;
            brt.anchorMin = brt.anchorMax = new Vector2(0.5f, 0.15f);
            brt.sizeDelta = new Vector2(420f, 96f);

            _host.StartCoroutine(AutoClose(panel, autoSeconds, () =>
            {
                if (finished) return;
                finished = true;
                Destroy(panel);
                onDone?.Invoke(true);
            }));
        }

        static IEnumerator AutoClose(GameObject panel, float seconds, Action action)
        {
            yield return new WaitForSeconds(seconds);
            if (panel != null) action?.Invoke();
        }
    }
}
