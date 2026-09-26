using UnityEngine;
using UnityEngine.UI;

namespace Match3
{
    /// <summary>
    /// uGUI 控件小工厂，统一用内置字体，避免 TMP 资源依赖。
    /// 框架阶段所有界面代码生成，后续可替换为预制体。
    /// </summary>
    public static class UIFactory
    {
        static Font _font;
        public static Font DefaultFont
        {
            get
            {
                if (_font == null)
                {
                    _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    if (_font == null) _font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                }
                return _font;
            }
        }

        /// <summary>
        /// 新建面板。anchorMin==anchorMax 时按 size 定尺寸；否则拉伸（size 被忽略）。
        /// </summary>
        public static GameObject CreatePanel(Transform parent, string name,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = anchorMin == anchorMax ? size : Vector2.zero;
            go.GetComponent<Image>().color = color;
            go.GetComponent<Image>().raycastTarget = true;
            return go;
        }

        /// <summary>铺满父级的文本。</summary>
        public static Text CreateLabel(GameObject parent, string content, int fontSize,
            TextAnchor anchor, Color color)
        {
            var go = new GameObject("Label", typeof(RectTransform), typeof(Text));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent.transform, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(12, 8);
            rt.offsetMax = new Vector2(-12, -8);

            var txt = go.GetComponent<Text>();
            txt.text = content;
            txt.font = DefaultFont;
            txt.fontSize = fontSize;
            txt.alignment = anchor;
            txt.color = color;
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;
            txt.verticalOverflow = VerticalWrapMode.Overflow;
            txt.raycastTarget = false;
            return txt;
        }

        /// <summary>按钮（自带文本子物体），RectTransform 默认居中不拉伸，调用方自行设置锚点/尺寸。</summary>
        public static Button CreateButton(GameObject parent, string content, int fontSize,
            UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject("Btn_" + content, typeof(RectTransform), typeof(Image), typeof(Button));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent.transform, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(300f, 84f);

            var img = go.GetComponent<Image>();
            img.color = new Color(0.95f, 0.95f, 0.95f, 1f);

            var btn = go.GetComponent<Button>();
            var colors = btn.colors;
            colors.highlightedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            colors.pressedColor = new Color(0.65f, 0.65f, 0.65f, 1f);
            btn.colors = colors;
            if (onClick != null) btn.onClick.AddListener(onClick);

            var labelGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            var lrt = (RectTransform)labelGo.transform;
            lrt.SetParent(go.transform, false);
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero;
            lrt.offsetMax = Vector2.zero;
            var txt = labelGo.GetComponent<Text>();
            txt.text = content;
            txt.font = DefaultFont;
            txt.fontSize = fontSize;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.black;
            txt.raycastTarget = false;
            return btn;
        }

        /// <summary>在 parent 上添加 Canvas 组（ScreenSpaceOverlay + 竖屏参考分辨率）。</summary>
        public static Canvas CreateCanvas(string name, int sortOrder)
        {
            var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortOrder;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }
    }
}
