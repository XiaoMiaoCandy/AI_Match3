using UnityEngine;

namespace Match3
{
    /// <summary>
    /// 运行时根据实际屏幕宽高调整正交相机，保证棋盘完整可见（竖屏优先）。
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraFitter : MonoBehaviour
    {
        public float margin = 1.1f;
        Camera _cam;
        int _w, _h;

        public void SetBoardSize(int w, int h)
        {
            _w = w; _h = h;
            Apply();
        }

        void Awake()
        {
            _cam = GetComponent<Camera>();
            Apply();
        }

        void Update()
        {
            if (Screen.width != _lastScreenW || Screen.height != _lastScreenH) Apply();
        }

        int _lastScreenW, _lastScreenH;

        void Apply()
        {
            if (_cam == null) _cam = GetComponent<Camera>();
            if (_w <= 0 || _h <= 0) return;
            float aspect = Screen.height > 0 ? (float)Screen.width / Screen.height : 0.5625f;
            float halfH = _h * 0.5f;
            float halfW = _w * 0.5f;
            _cam.orthographicSize = Mathf.Max(halfH + margin, halfW / aspect + margin);
            _lastScreenW = Screen.width;
            _lastScreenH = Screen.height;
        }
    }
}
