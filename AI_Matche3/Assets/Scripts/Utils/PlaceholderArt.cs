using UnityEngine;

namespace Match3
{
    /// <summary>
    /// 占位美术：代码生成纯白方块 + 若干颜色，后续替换成美术素材时只需改这里/棋子的 Sprite。
    /// </summary>
    public static class PlaceholderArt
    {
        static Sprite _whiteSquare;

        public static Sprite WhiteSquare
        {
            get
            {
                if (_whiteSquare == null) _whiteSquare = CreateSquare(64, new Color32(255, 255, 255, 255));
                return _whiteSquare;
            }
        }

        static Sprite CreateSquare(int size, Color32 color)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            var px = new Color32[size * size];
            for (int i = 0; i < px.Length; i++) px[i] = color;
            tex.SetPixels32(px);
            tex.Apply();
            var sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            sprite.name = $"Placeholder_Square_{color}";
            return sprite;
        }

        /// <summary>开始界面占位序列帧：一组不同颜色的方块，未在 GameConfig 配置图片时演示动画用。</summary>
        public static Sprite[] StartScreenFrames()
        {
            var frames = new Sprite[TileColors.Length];
            for (int i = 0; i < frames.Length; i++)
                frames[i] = CreateSquare(64, TileColors[i]);
            return frames;
        }

        // 占位棋子配色（红/蓝/绿/黄/紫/橙/青/粉），最多 8 种
        public static readonly Color[] TileColors =
        {
            new Color(0.93f, 0.30f, 0.30f), // 0 红
            new Color(0.25f, 0.52f, 0.95f), // 1 蓝
            new Color(0.36f, 0.78f, 0.38f), // 2 绿
            new Color(0.98f, 0.82f, 0.27f), // 3 黄
            new Color(0.61f, 0.36f, 0.85f), // 4 紫
            new Color(0.96f, 0.58f, 0.20f), // 5 橙
            new Color(0.25f, 0.78f, 0.83f), // 6 青
            new Color(0.94f, 0.48f, 0.72f), // 7 粉
        };
    }
}
