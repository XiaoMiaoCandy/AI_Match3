using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Match3
{
    /// <summary>
    /// 棋盘：负责棋子生成、输入交换、匹配检测、消除、下落、补充、死局洗牌。
    /// 纯玩法逻辑，不直接操作 UI/广告，通过事件对外通知。
    /// </summary>
    public class Board : MonoBehaviour
    {
        public const float CellSize = 1f;

        GameConfig _cfg;
        Tile[,] _grid;
        int _w, _h, _types;
        Tile _selected;
        Camera _cam;
        WaitForSeconds _deadlockDelay;

        public bool IsBusy { get; private set; }
        public bool InputEnabled { get; set; } = true;

        /// <summary>一次交换操作开始（用于扣步数）。</summary>
        public event Action OnSwapMade;
        /// <summary>一次连锁结算完成，参数：本次连锁消除数、连锁层数(1起)。</summary>
        public event Action<int, int> OnMatched;
        /// <summary>棋盘上所有动画结束（交换无效回弹或一整轮结算+洗牌结束）。</summary>
        public event Action OnBoardSettled;
        /// <summary>洗牌发生（可播放提示）。</summary>
        public event Action OnShuffled;

        #region 初始化与坐标

        public void Init(GameConfig cfg)
        {
            _cfg = cfg;
            _w = cfg.boardWidth;
            _h = cfg.boardHeight;
            _types = Mathf.Clamp(cfg.tileTypes, 3, PlaceholderArt.TileColors.Length);
            _grid = new Tile[_w, _h];
            _cam = Camera.main;
            _deadlockDelay = new WaitForSeconds(0.25f);
            BuildInitial();
        }

        /// <summary>棋盘中心点。</summary>
        public Vector3 BoardCenter => new Vector3(-(_w - 1) * CellSize * 0.5f, -(_h - 1) * CellSize * 0.5f, 0f);

        public Vector3 CellToWorld(int col, int row)
        {
            return new Vector3((col - (_w - 1) * 0.5f) * CellSize,
                               (row - (_h - 1) * 0.5f) * CellSize, 0f);
        }

        public static bool IsAdjacent(int r1, int c1, int r2, int c2)
        {
            return Mathf.Abs(r1 - r2) + Mathf.Abs(c1 - c2) == 1;
        }

        #endregion

        #region 初始生成

        void BuildInitial()
        {
            for (int x = 0; x < _w; x++)
            {
                for (int y = 0; y < _h; y++)
                {
                    int type;
                    int guard = 0;
                    do
                    {
                        type = Random.Range(0, _types);
                        guard++;
                    }
                    while (guard < 50 && WouldStartMatch(x, y, type));
                    SpawnTile(x, y, type, false);
                }
            }
        }

        bool WouldStartMatch(int x, int y, int type)
        {
            if (x >= 2 && _grid[x - 1, y] != null && _grid[x - 2, y] != null
                && _grid[x - 1, y].Type == type && _grid[x - 2, y].Type == type)
                return true;
            if (y >= 2 && _grid[x, y - 1] != null && _grid[x, y - 2] != null
                && _grid[x, y - 1].Type == type && _grid[x, y - 2].Type == type)
                return true;
            return false;
        }

        Tile SpawnTile(int x, int y, int type, bool fromTop)
        {
            var go = new GameObject($"Tile_{x}_{y}");
            go.transform.SetParent(transform, false);
            go.transform.position = CellToWorld(x, fromTop ? y + _h : y);
            var tile = go.AddComponent<Tile>();
            tile.SetType(type, CellSize);
            tile.Row = y;
            tile.Col = x;
            _grid[x, y] = tile;
            return tile;
        }

        #endregion

        #region 匹配检测

        bool IsSame(int x, int y, int type)
        {
            return x >= 0 && x < _w && y >= 0 && y < _h && _grid[x, y] != null && _grid[x, y].Type == type;
        }

        /// <summary>找出所有属于三连及以上的格子（去重）。</summary>
        List<Vector2Int> FindMatches()
        {
            var set = new HashSet<Vector2Int>();
            for (int y = 0; y < _h; y++)
            {
                for (int x = 0; x < _w; x++)
                {
                    var t = _grid[x, y];
                    if (t == null) continue;
                    int type = t.Type;
                    int x0 = x;
                    while (IsSame(x0 - 1, y, type)) x0--;
                    int x1 = x;
                    while (IsSame(x1 + 1, y, type)) x1++;
                    if (x1 - x0 + 1 >= 3)
                        for (int i = x0; i <= x1; i++) set.Add(new Vector2Int(i, y));

                    int y0 = y;
                    while (IsSame(x, y0 - 1, type)) y0--;
                    int y1 = y;
                    while (IsSame(x, y1 + 1, type)) y1++;
                    if (y1 - y0 + 1 >= 3)
                        for (int j = y0; j <= y1; j++) set.Add(new Vector2Int(x, j));
                }
            }
            return new List<Vector2Int>(set);
        }

        #endregion

        #region 输入

        float _mouseIgnoredUntil; // 触摸发生后短暂忽略鼠标按下：模拟器里一次点击可能同时产生触摸+鼠标两个事件，
                                  // 若分属两帧，会"选中后立刻又点自己→取消"，表现为永远选不中

        void Update()
        {
            if (!InputEnabled || IsBusy) return;

            // 同时兼容鼠标与触摸（设备模拟器/真机走触摸路径）
            Vector3 screenPos;
            if (Input.touchCount > 0)
            {
                var t = Input.GetTouch(0);
                if (t.phase != TouchPhase.Began) return;
                screenPos = t.position;
                _mouseIgnoredUntil = Time.realtimeSinceStartup + 0.3f;
            }
            else if (Input.GetMouseButtonDown(0) && Time.realtimeSinceStartup >= _mouseIgnoredUntil)
            {
                screenPos = Input.mousePosition;
            }
            else
            {
                return;
            }

            HandleClick(screenPos);
        }

        void HandleClick(Vector3 screenPos)
        {
            if (_cam == null) _cam = Camera.main;
            if (_cam == null) return;
            // ScreenToWorldPoint 的 z 是"到相机的距离"，必须显式传相机到棋盘平面(z=0)的距离；
            // 传 0 会取到相机自身位置，导致所有点击都落在棋盘中心
            float dist = Mathf.Abs(_cam.transform.position.z);
            Vector3 world = _cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, dist));
            var hit = Physics2D.OverlapPoint(world);
            Debug.Log($"[Board] click screen={screenPos} Screen={Screen.width}x{Screen.height} world={world} hit={(hit != null ? hit.name : "null")} inputEnabled={InputEnabled} busy={IsBusy}");
            if (hit == null)
            {
                // 诊断：打印离点击点最近的棋子及其碰撞体状态
                Tile nearest = null;
                float best = float.MaxValue;
                foreach (Transform t in transform)
                {
                    var tile2 = t.GetComponent<Tile>();
                    if (tile2 == null) continue;
                    float d = ((Vector2)t.position - (Vector2)world).sqrMagnitude;
                    if (d < best) { best = d; nearest = tile2; }
                }
                if (nearest != null)
                {
                    var c = nearest.GetComponent<Collider2D>();
                    //Debug.Log($"[Board] nearest={nearest.name} tilePos={nearest.transform.position} colliderEnabled={c.enabled} colliderSize={c.bounds.size} distToClick={Mathf.Sqrt(best):F3}");
                }
            }
            if (hit == null)
            {
                ClearSelection();
                return;
            }
            var tile = hit.GetComponent<Tile>();
            if (tile == null) return;

            if (_selected == null)
            {
                Select(tile);
            }
            else if (_selected == tile)
            {
                ClearSelection();
            }
            else if (IsAdjacent(_selected.Row, _selected.Col, tile.Row, tile.Col))
            {
                var a = _selected;
                ClearSelection();
                StartCoroutine(DoSwap(a, tile));
            }
            else
            {
                Select(tile);
            }
        }

        void Select(Tile tile)
        {
            ClearSelection();
            _selected = tile;
            tile.transform.localScale *= 1.15f;
        }

        void ClearSelection()
        {
            if (_selected != null) _selected.transform.localScale = new Vector3(CellSize * 0.86f, CellSize * 0.86f, 1f);
            _selected = null;
        }

        #endregion

        #region 交换与结算

        IEnumerator DoSwap(Tile a, Tile b)
        {
            IsBusy = true;
            OnSwapMade?.Invoke();

            SwapGrid(a, b);
            yield return StartCoroutine(MoveBoth(a, b, _cfg.swapTime));

            var matches = FindMatches();
            if (matches.Count > 0)
            {
                yield return StartCoroutine(ResolveCascades());
                yield return _deadlockDelay;
                if (!HasPossibleMove())
                {
                    yield return StartCoroutine(ShuffleBoard());
                }
                OnBoardSettled?.Invoke();
            }
            else
            {
                // 无效交换：弹回，不计分（步数是否退由 GameManager 决定）
                SwapGrid(a, b);
                yield return StartCoroutine(MoveBoth(a, b, _cfg.swapTime));
                OnBoardSettled?.Invoke();
            }
            IsBusy = false;
        }

        IEnumerator MoveBoth(Tile a, Tile b, float time)
        {
            var pa = a.transform.position;
            var pb = b.transform.position;
            var ca = StartCoroutine(a.MoveTo(pb, time));
            var cb = StartCoroutine(b.MoveTo(pa, time));
            yield return ca;
            yield return cb;
        }

        void SwapGrid(Tile a, Tile b)
        {
            int ax = a.Col, ay = a.Row, bx = b.Col, by = b.Row;
            _grid[ax, ay] = b;
            _grid[bx, by] = a;
            a.Col = bx; a.Row = by;
            b.Col = ax; b.Row = ay;
        }

        IEnumerator ResolveCascades()
        {
            int chain = 0;
            while (true)
            {
                var matches = FindMatches();
                if (matches.Count == 0) break;
                chain++;

                // 消除
                var tiles = new List<Tile>(matches.Count);
                var coroutines = new List<Coroutine>(matches.Count);
                foreach (var p in matches)
                {
                    var tile = _grid[p.x, p.y];
                    if (tile != null)
                    {
                        _grid[p.x, p.y] = null;
                        tiles.Add(tile);
                        coroutines.Add(StartCoroutine(tile.ClearAnim(_cfg.clearTime)));
                    }
                }
                OnMatched?.Invoke(matches.Count, chain);
                foreach (var c in coroutines) yield return c;
                foreach (var t in tiles) Destroy(t.gameObject);

                // 下落 + 补充（同一轮并行，按各自高度计速）
                yield return StartCoroutine(ApplyGravityAndRefill());
                yield return new WaitForSeconds(0.06f);
            }
        }

        IEnumerator ApplyGravityAndRefill()
        {
            var moves = new List<IEnumerator>();
            for (int x = 0; x < _w; x++)
            {
                int writeY = 0;
                for (int y = 0; y < _h; y++)
                {
                    if (_grid[x, y] != null)
                    {
                        if (writeY != y)
                        {
                            var tile = _grid[x, y];
                            _grid[x, writeY] = tile;
                            _grid[x, y] = null;
                            tile.Row = writeY;
                            int dist = y - writeY;
                            moves.Add(tile.MoveTo(CellToWorld(x, writeY), Mathf.Max(_cfg.refillMinTime, dist * _cfg.fallTimePerCell)));
                        }
                        writeY++;
                    }
                }
                // 顶部补充新棋子
                int missing = _h - writeY;
                for (int k = 0; k < missing; k++)
                {
                    int y = writeY + k;
                    var tile = SpawnTile(x, y, Random.Range(0, _types), true);
                    int dist = _h; // 从棋盘上方落入
                    moves.Add(tile.MoveTo(CellToWorld(x, y), Mathf.Max(_cfg.refillMinTime, dist * _cfg.fallTimePerCell)));
                }
            }
            foreach (var m in moves) yield return StartCoroutine(m);
        }

        #endregion

        #region 死局检测与洗牌

        bool HasPossibleMove()
        {
            for (int y = 0; y < _h; y++)
            {
                for (int x = 0; x < _w; x++)
                {
                    if (_grid[x, y] == null) continue;
                    if (x + 1 < _w && _grid[x + 1, y] != null && SwapAndCheck(x, y, x + 1, y)) return true;
                    if (y + 1 < _h && _grid[x, y + 1] != null && SwapAndCheck(x, y, x, y + 1)) return true;
                }
            }
            return false;
        }

        bool SwapAndCheck(int x1, int y1, int x2, int y2)
        {
            var a = _grid[x1, y1];
            var b = _grid[x2, y2];
            _grid[x1, y1] = b;
            _grid[x2, y2] = a;
            bool match = FindMatches().Count > 0;
            _grid[x1, y1] = a;
            _grid[x2, y2] = b;
            return match;
        }

        /// <summary>外部触发洗牌（如看广告打乱），带忙碌保护。</summary>
        public IEnumerator ShuffleWithBusy()
        {
            if (IsBusy) yield break;
            IsBusy = true;
            ClearSelection();
            yield return StartCoroutine(ShuffleBoard());
            IsBusy = false;
        }

        IEnumerator ShuffleBoard()
        {
            var types = new List<int>(_w * _h);
            foreach (var t in _grid) types.Add(t.Type);

            bool ok = false;
            for (int attempt = 0; attempt < 100 && !ok; attempt++)
            {
                ShuffleList(types);
                int i = 0;
                for (int y = 0; y < _h; y++)
                    for (int x = 0; x < _w; x++)
                        _grid[x, y].SetType(types[i++], CellSize);

                ok = FindMatches().Count == 0 && HasPossibleMove();
            }

            // 洗牌后确保各棋子坐标/名称正确，并归位
            var coroutines = new List<Coroutine>();
            for (int y = 0; y < _h; y++)
                for (int x = 0; x < _w; x++)
                {
                    var t = _grid[x, y];
                    t.Row = y; t.Col = x;
                    t.name = $"Tile_{x}_{y}";
                    coroutines.Add(StartCoroutine(t.MoveTo(CellToWorld(x, y), 0.25f)));
                }
            foreach (var c in coroutines) yield return c;
            OnShuffled?.Invoke();
        }

        static void ShuffleList(List<int> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        #endregion

        #region 重新开始

        public IEnumerator Rebuild()
        {
            IsBusy = true;
            ClearSelection();

            // 清空旧棋子
            for (int x = 0; x < _w; x++)
            {
                for (int y = 0; y < _h; y++)
                {
                    if (_grid != null && _grid[x, y] != null) Destroy(_grid[x, y].gameObject);
                }
            }
            yield return null;

            _grid = new Tile[_w, _h];
            BuildInitial();
            IsBusy = false;
        }

        #endregion
    }
}
