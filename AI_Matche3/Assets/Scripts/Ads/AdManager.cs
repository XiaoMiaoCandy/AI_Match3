using System;
using UnityEngine;

namespace Match3
{
    /// <summary>
    /// 广告统一入口（门面）。游戏代码只调这里，不关心具体 SDK。
    /// 默认使用 MockAdService；接 SDK 后调用 AdManager.UseService(new XxxAdService())。
    /// </summary>
    public static class AdManager
    {
        static IAdService _service;
        static bool _initialized;

        /// <summary>两次插屏之间至少间隔的完成关卡数（防止广告太密）。</summary>
        const int InterstitialEveryNLevels = 2;
        static int _levelCounter;

        public static string ServiceName => _service?.Name ?? "None";

        public static void Initialize(IAdService service = null)
        {
            _service = service ?? new MockAdService();
            _service.Initialize();
            _service.ShowBanner();
            _initialized = true;
            Debug.Log($"[Ads] AdManager ready, service = {ServiceName}");
        }

        /// <summary>替换广告实现（接真实 SDK 时用）。</summary>
        public static void UseService(IAdService service)
        {
            if (_initialized)
            {
                _service.HideBanner();
            }
            _service = service;
            _service.Initialize();
            _service.ShowBanner();
            _initialized = true;
        }

        public static void ShowRewarded(string placement, Action<bool> callback)
        {
            _service?.ShowRewarded(placement, callback);
        }

        /// <summary>带频控的插屏：每完成 N 关展示一次。</summary>
        public static void ShowInterstitial(string placement, Action onClosed)
        {
            _levelCounter++;
            if (_service != null && _levelCounter % InterstitialEveryNLevels == 0)
            {
                _service.ShowInterstitial(placement, onClosed);
            }
            else
            {
                onClosed?.Invoke();
            }
        }

        public static void ShowBanner() => _service?.ShowBanner();
        public static void HideBanner() => _service?.HideBanner();
    }
}
