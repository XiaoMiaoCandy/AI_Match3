using System;
using System.Collections;
using UnityEngine;

namespace Match3
{
    /// <summary>
    /// 假广告：未接入 SDK 前用于跑通全部流程。
    /// - 激励视频：弹出全屏面板，1.5 秒后自动“播放完成”并发奖励；
    /// - 插屏：弹出全屏面板，1.2 秒后自动关闭并继续；
    /// - Banner：屏幕底部常驻白条。
    /// </summary>
    public class MockAdService : IAdService
    {
        public string Name => "MockAdService";

        GameObject _banner;

        public void Initialize()
        {
            Debug.Log("[Ads] MockAdService initialized（当前为模拟广告，接 SDK 后替换）");
        }

        public void ShowRewarded(string placement, Action<bool> callback)
        {
            AdMockOverlay.Show($"[模拟激励视频] {placement}\n播放中…\n（1.5秒后自动完成）",
                "跳过(不发奖励)", 1.5f,
                completed => callback(completed));
        }

        public void ShowInterstitial(string placement, Action onClosed)
        {
            AdMockOverlay.Show($"[模拟插屏广告] {placement}\n1.2秒后自动关闭",
                "关闭", 1.2f,
                _ => onClosed?.Invoke());
        }

        public void ShowBanner()
        {
            if (_banner != null) return;
            var canvas = AdMockOverlay.GetCanvas();
            _banner = UIFactory.CreatePanel(canvas.transform, "MockBanner",
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(420f, 64f), new Color(0.15f, 0.15f, 0.15f, 0.9f));
            ((RectTransform)_banner.transform).pivot = new Vector2(0.5f, 0f);
            UIFactory.CreateLabel(_banner, "[ Banner 广告位 560x90 ]", 20, TextAnchor.MiddleCenter, Color.white);
        }

        public void HideBanner()
        {
            if (_banner != null)
            {
                UnityEngine.Object.Destroy(_banner);
                _banner = null;
            }
        }
    }
}
