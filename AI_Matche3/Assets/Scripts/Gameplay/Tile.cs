using System.Collections;
using UnityEngine;

namespace Match3
{
    /// <summary>
    /// 单个棋子。占位形态：带颜色的方块 SpriteRenderer + BoxCollider2D。
    /// 后续换成美术图时，只需要换 SpriteRenderer 的 sprite。
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(BoxCollider2D))]
    public class Tile : MonoBehaviour
    {
        SpriteRenderer _sr;
        public int Type { get; private set; }
        public int Row { get; set; }
        public int Col { get; set; }
        public bool IsAnimating { get; private set; }

        void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
            if (_sr.sprite == null) _sr.sprite = PlaceholderArt.WhiteSquare;

            // 显式设置碰撞体尺寸：AddComponent 时 sprite 尚未赋值，碰撞体自动适配会得到 0，
            // 导致 Physics2D.OverlapPoint 永远命中不了棋子
            var col = GetComponent<BoxCollider2D>();
            col.size = new Vector2(1f, 1f);
            col.offset = Vector2.zero;
        }

        void Reset()
        {
            // 编辑器中 AddComponent 时自动给占位图，方便场景搭建可视化
            var sr = GetComponent<SpriteRenderer>();
            if (sr != null && sr.sprite == null) sr.sprite = PlaceholderArt.WhiteSquare;
        }

        public void SetType(int type, float size)
        {
            Type = type;
            if (_sr == null) _sr = GetComponent<SpriteRenderer>();
            _sr.sprite = PlaceholderArt.WhiteSquare;
            _sr.color = PlaceholderArt.TileColors[Mathf.Clamp(type, 0, PlaceholderArt.TileColors.Length - 1)];
            transform.localScale = new Vector3(size * 0.86f, size * 0.86f, 1f);
        }

        /// <summary>平滑移动到世界坐标。</summary>
        public IEnumerator MoveTo(Vector3 target, float duration)
        {
            IsAnimating = true;
            Vector3 start = transform.position;
            float t = 0f;
            if (duration <= 0f)
            {
                transform.position = target;
                IsAnimating = false;
                yield break;
            }
            while (t < 1f)
            {
                t += Time.deltaTime / duration;
                // 下落用 ease-out，手感更好
                float e = 1f - Mathf.Pow(1f - Mathf.Clamp01(t), 2f);
                transform.position = Vector3.Lerp(start, target, e);
                yield return null;
            }
            transform.position = target;
            IsAnimating = false;
        }

        /// <summary>消除动画：缩小消失。</summary>
        public IEnumerator ClearAnim(float duration)
        {
            IsAnimating = true;
            Vector3 startScale = transform.localScale;
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / Mathf.Max(0.01f, duration);
                transform.localScale = startScale * (1f - Mathf.Clamp01(t));
                yield return null;
            }
            IsAnimating = false;
        }
    }
}
