using System;

namespace Match3
{
    /// <summary>
    /// 广告服务统一接口。
    /// 接入真实 SDK（穿山甲 Pangle / 优量汇 GDT / AdMob 等）时，
    /// 写一个实现该接口的类并在 AdManager.UseService() 中替换即可，业务代码不用改。
    /// </summary>
    public interface IAdService
    {
        string Name { get; }

        /// <summary>SDK 初始化（在游戏启动时调用一次）。</summary>
        void Initialize();

        /// <summary>激励视频。callback(true) 表示完整观看，应发奖励；false 表示未完成/失败。</summary>
        void ShowRewarded(string placement, Action<bool> callback);

        /// <summary>插屏广告。onClosed 在广告关闭（或无填充）后回调，用于继续流程。</summary>
        void ShowInterstitial(string placement, Action onClosed);

        /// <summary>Banner 显示/隐藏。</summary>
        void ShowBanner();
        void HideBanner();
    }
}
