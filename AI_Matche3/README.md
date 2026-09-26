# Match3 三消游戏框架（仿开心消消乐）

团结引擎（Tuanjie 2022.3.62t15）工程。当前为**游戏框架版本**：核心玩法可玩、UI/HUD 齐全、广告链路全部跑通；所有美术为代码生成的纯色占位方块，后续直接替换 Sprite 即可。

## 一、怎么跑起来

两种方式任选：

1. **直接运行（零配置）**：打开任意场景，点击 Play。
   `GameBootstrap` 会在进入播放模式时自动用代码搭建整个游戏。
2. **编辑器搭建（推荐用于继续开发）**：菜单 `Tools/Match3/搭建游戏场景 (Ctrl+Alt+B)`，
   会生成 `Assets/Scenes/Match3Game.unity` 并自动创建 `Assets/Resources/GameConfig.asset` 配置资产。

其他菜单：
- `Tools/Match3/生成 GameConfig 配置资产`
- `Tools/Match3/清空为空白场景`

## 二、目录结构

```
Assets/Scripts/
├── Core/
│   ├── GameConfig.cs      全局配置（棋盘尺寸、棋子种类、步数、目标分、动画时长）
│   ├── GameManager.cs     游戏流程：分数/步数/胜负/广告时机
│   ├── SceneSetup.cs      纯代码搭建场景（相机+棋盘+管理器+UI）
│   └── GameBootstrap.cs   运行时自动引导（空场景也能跑）
├── Gameplay/
│   ├── Board.cs           棋盘：交换、匹配检测、消除、下落、补充、死局洗牌
│   └── Tile.cs            单个棋子（移动/消除动画）
├── UI/
│   └── HudController.cs   顶部 HUD + 结算弹窗 + Toast
├── Ads/
│   ├── IAdService.cs      ★ 广告统一接口（接 SDK 只需实现它）
│   ├── AdManager.cs       广告门面：频控、激励/插屏/Banner 入口
│   ├── MockAdService.cs   模拟广告（默认，无 SDK 也能跑通流程）
│   └── AdMockOverlay.cs   模拟广告的全屏面板（接 SDK 后不再使用）
├── Utils/
│   ├── PlaceholderArt.cs  占位美术（纯白方块 + 8 种棋子配色）
│   ├── UIFactory.cs       uGUI 控件工厂（内置字体，不依赖 TMP）
│   └── CameraFitter.cs    相机自适应（保证棋盘完整可见）
└── Editor/
    └── Match3SceneBuilder.cs  编辑器一键搭建菜单
```

## 三、玩法说明

- 点击两个**相邻**棋子交换；不能形成三连则自动弹回并退还步数。
- 三连及以上消除；消除后上方棋子下落、顶部补充新棋子。
- **连锁**：一次交换引发的连续消除，第 N 层连锁得分 ×N。
- 达成目标分 = 胜利；步数耗尽未达标 = 失败，可看激励视频加 5 步续命。
- 棋盘无可消除步骤时自动**洗牌**（保证无现成三连且存在可行步骤）。

参数都在 `GameConfig.asset`（或 `GameConfig.Default`）里调：棋盘 8×8、6 种棋子、20 步、目标 3000 分等。

## 四、广告接入说明

游戏代码只依赖 `IAdService`，**不耦合任何广告 SDK**。当前默认 `MockAdService`：
- 激励视频：全屏面板，1.5 秒后自动“播放完成”并发奖；可点“跳过”模拟中途退出；
- 插屏：全屏面板，1.2 秒后自动关闭继续流程（带频控，默认每 2 关 1 次）；
- Banner：屏幕底部常驻条。

### 接入真实 SDK（以穿山甲 Pangle 为例）

1. 在团结引擎包管理/工程中导入对应 Android/iOS SDK（穿山甲官方 Unity 插件或 AAR）。
2. 新建 `Assets/Scripts/Ads/PangleAdService.cs`，实现 `IAdService`：

   ```csharp
   public class PangleAdService : IAdService
   {
       public string Name => "Pangle";
       public void Initialize() { /* Pangle SDK 初始化，传 AppId */ }
       public void ShowRewarded(string placement, Action<bool> cb)
       {
           // 加载并展示激励视频；OnRewardVerify -> cb(true)；关闭/失败 -> cb(false)
       }
       public void ShowInterstitial(string placement, Action onClosed) { /* ... */ }
       public void ShowBanner() { /* ... */ }
       public void HideBanner() { /* ... */ }
   }
   ```
3. 在 `GameManager.Start()` 的 `AdManager.Initialize();` 处改为：

   ```csharp
   AdManager.Initialize(new PangleAdService());
   ```

业务代码（续命、过关插屏、Banner）一行都不用改。优量汇（GDT）、AdMob、Unity Ads 同理，各自一个实现类即可，也可做“瀑布流”聚合实现。

> 广告位/频控参数：`AdManager.InterstitialEveryNLevels`；激励视频奖励数：`GameConfig.rewardedExtraMoves`。
> 正式上线前需在平台后台创建代码位 ID，并在 Android/iOS Player Settings 中配置权限与隐私合规弹窗（国内市场要求隐私政策同意后再初始化 SDK）。

## 五、后续可扩展（按优先级建议）

1. 特殊棋子：四连生成直线特效、五连生成彩色炸弹、L/T 生成爆炸棋子（在 `Board.ResolveCascades` 匹配结果中识别长度与形状）。
2. 障碍物：冰块（需消除相邻棋子）、锁链、藤蔓等开心消消乐经典元素。
3. 关卡系统：多关卡配置表（步数/目标/障碍/棋盘形状），目前是单关参数。
4. 音效与粒子：消除音效、连击特效，占位方块替换为美术素材。
5. 存档：PlayerPrefs/JSON 记录最高关与分数。
