using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using BlockPuzzle.Grid;

namespace BlockPuzzle.Gameplay
{
    /// <summary>
    /// Cosmetic-only line clear FX. Rows burst outward from their centre;
    /// columns burst upward/downward. Uses the existing LinesCleared event.
    /// Does not delay, mutate, or re-score a clear, and never blocks UI input.
    /// Compatible with permanent locks and breakable obstacles in Adventure.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class DirectionalLineClearFX : MonoBehaviour
    {
        [Header("Existing Scene References")]
        [SerializeField] private GridManager gridManager;
        [SerializeField] private RectTransform gridRoot;

        [Header("Effect Switch")]
        [SerializeField] private bool enableEffect = true;

        [Header("Line Colours")]
        [SerializeField] private Color horizontalColor = new Color(0.15f, 1f, 0.75f, 0.92f);
        [SerializeField] private Color verticalColor = new Color(0.30f, 0.70f, 1f, 0.92f);
        [SerializeField] private Color intersectionColor = new Color(1f, 0.96f, 0.70f, 1f);

        [Header("Timing - Unscaled Seconds")]
        [SerializeField, Range(0.15f, 0.65f)] private float beamDuration = 0.32f;
        [SerializeField, Range(0.12f, 0.65f)] private float tileBurstDuration = 0.28f;
        [SerializeField, Range(0f, 0.18f)] private float edgeWaveDelay = 0.10f;

        [Header("Mobile Quality")]
        [SerializeField, Range(0.06f, 0.30f)] private float beamThicknessRatio = 0.12f;
        [SerializeField, Range(16, 128)] private int maximumConcurrentElements = 96;

        private sealed class Element
        {
            public RectTransform Rect;
            public Image Image;
            public Vector2 Position;
            public Vector2 FullSize;
            public Vector2 Drift;
            public Color Tint;
            public float Age;
            public float Delay;
            public float Duration;
            public bool Beam;
        }

        private readonly List<Element> active = new List<Element>(96);
        private readonly Stack<Element> pool = new Stack<Element>(96);
        private RectTransform fxLayer;
        private GridCellView[,] cellViews;
        private bool subscribed;

        private void OnEnable()
        {
            Subscribe();
        }

        private void Start()
        {
            EnsureLayer();
        }

        private void Subscribe()
        {
            if (subscribed || gridManager == null) return;
            gridManager.LinesCleared += HandleLinesCleared;
            subscribed = true;
        }

        private void OnDisable()
        {
            if (subscribed && gridManager != null)
                gridManager.LinesCleared -= HandleLinesCleared;
            subscribed = false;
            for (int i = active.Count - 1; i >= 0; i--)
                RecycleAt(i);
        }

        private void Update()
        {
            if (active.Count == 0) return;
            float dt = Mathf.Max(0f, Time.unscaledDeltaTime);
            for (int i = active.Count - 1; i >= 0; i--)
            {
                Element item = active[i];
                if (item.Rect == null || item.Image == null)
                {
                    active.RemoveAt(i);
                    continue;
                }

                item.Age += dt;
                float t = (item.Age - item.Delay) / Mathf.Max(0.01f, item.Duration);
                if (t < 0f) continue;
                if (t >= 1f)
                {
                    RecycleAt(i);
                    continue;
                }

                float progress = Mathf.Clamp01(t);
                Color tint = item.Tint;
                if (item.Beam)
                {
                    // A symmetric beam expands both ways from a line's centre.
                    float easeOut = 1f - Mathf.Pow(1f - progress, 3f);
                    item.Rect.sizeDelta = item.FullSize * easeOut;
                    item.Rect.localScale = Vector3.one;
                    tint.a *= Mathf.Sin(progress * Mathf.PI);
                }
                else
                {
                    // A ghost tile bursts gently outward and fades, without
                    // touching the real GridCellView or its placement preview.
                    item.Rect.sizeDelta = item.FullSize;
                    item.Rect.anchoredPosition = item.Position +
                        item.Drift * (1f - Mathf.Pow(1f - progress, 2f));
                    float pulse = 0.82f + 0.43f * (1f - Mathf.Pow(1f - progress, 2f));
                    item.Rect.localScale = new Vector3(pulse, pulse, 1f);
                    tint.a *= Mathf.Min(1f, progress * 8f) *
                              (1f - Mathf.Pow(progress, 1.5f));
                }
                item.Image.color = tint;
            }
        }

        private void HandleLinesCleared(LineClearResult result)
        {
            if (!enableEffect || result == null || !result.HasClearedLines ||
                !EnsureViews() || !EnsureLayer()) return;

            int rows = gridManager.Rows;
            int columns = gridManager.Columns;
            var cleared = new bool[rows, columns];
            foreach (Vector2Int position in result.ClearedCells)
            {
                if (gridManager.IsInsideGrid(position.x, position.y))
                    cleared[position.x, position.y] = true;
            }

            // LineClearResult exposes row/column COUNTS, not their indices.
            // A completed row or column consists entirely of cleared cells
            // and/or permanent locks (locks stay occupied after a clear).
            // This uniquely reconstructs the completed line orientation even
            // when a row and a column are cleared in the same move.
            var completedRows = new bool[rows];
            var completedColumns = new bool[columns];
            int foundRows = 0;
            int foundColumns = 0;

            for (int row = 0; row < rows; row++)
            {
                bool complete = true;
                for (int column = 0; column < columns; column++)
                {
                    if (!cleared[row, column] && !IsPermanentLock(row, column))
                    {
                        complete = false;
                        break;
                    }
                }
                if (complete)
                {
                    completedRows[row] = true;
                    foundRows++;
                    SpawnRowBeam(row);
                }
            }

            for (int column = 0; column < columns; column++)
            {
                bool complete = true;
                for (int row = 0; row < rows; row++)
                {
                    if (!cleared[row, column] && !IsPermanentLock(row, column))
                    {
                        complete = false;
                        break;
                    }
                }
                if (complete)
                {
                    completedColumns[column] = true;
                    foundColumns++;
                    SpawnColumnBeam(column);
                }
            }

            if (foundRows != result.ClearedRowCount ||
                foundColumns != result.ClearedColumnCount)
            {
                // Gameplay remains unaffected if a future version changes
                // which cells are included in LineClearResult.ClearedCells.
                Debug.LogWarning("[DirectionalLineFX] Orientation count differs from " +
                    "clear result. Animation may be partial; scoring is unaffected.");
            }

            float centerRow = (rows - 1) * 0.5f;
            float centerColumn = (columns - 1) * 0.5f;
            foreach (Vector2Int position in result.ClearedCells)
            {
                int row = position.x;
                int col = position.y;
                if (!gridManager.IsInsideGrid(row, col)) continue;

                bool horizontal = completedRows[row];
                bool vertical = completedColumns[col];
                Color tint = horizontal && vertical
                    ? intersectionColor
                    : horizontal ? horizontalColor : verticalColor;

                float rowDelay = Mathf.Abs(col - centerColumn) /
                    Mathf.Max(1f, centerColumn) * edgeWaveDelay;
                float columnDelay = Mathf.Abs(row - centerRow) /
                    Mathf.Max(1f, centerRow) * edgeWaveDelay;
                float delay = horizontal && vertical
                    ? Mathf.Min(rowDelay, columnDelay)
                    : horizontal ? rowDelay : columnDelay;

                SpawnTile(row, col, tint, delay, horizontal, vertical);
            }
        }

        private bool IsPermanentLock(int row, int column)
        {
            GridCell cell = gridManager.GetCell(row, column);
            return cell != null && cell.SpecialKind == SpecialCellKind.Locked;
        }

        private void SpawnRowBeam(int row)
        {
            int last = gridManager.Columns - 1;
            if (!TryGetCenter(row, 0, out Vector2 first) ||
                !TryGetCenter(row, last, out Vector2 end)) return;
            float cellSize = GetCellSize(row, 0);
            Vector2 centre = (first + end) * 0.5f;
            float length = Mathf.Abs(end.x - first.x) + cellSize;
            float thickness = Mathf.Max(3f, cellSize * beamThicknessRatio);
            Spawn(centre, new Vector2(length, thickness), horizontalColor,
                0f, beamDuration, true, Vector2.zero);
        }

        private void SpawnColumnBeam(int column)
        {
            int last = gridManager.Rows - 1;
            if (!TryGetCenter(0, column, out Vector2 first) ||
                !TryGetCenter(last, column, out Vector2 end)) return;
            float cellSize = GetCellSize(0, column);
            Vector2 centre = (first + end) * 0.5f;
            float length = Mathf.Abs(end.y - first.y) + cellSize;
            float thickness = Mathf.Max(3f, cellSize * beamThicknessRatio);
            Spawn(centre, new Vector2(thickness, length), verticalColor,
                0f, beamDuration, true, Vector2.zero);
        }

        private void SpawnTile(int row, int column, Color tint, float delay,
            bool horizontal, bool vertical)
        {
            if (!TryGetCenter(row, column, out Vector2 centre)) return;
            float size = GetCellSize(row, column) * 0.90f;
            Vector2 outward = Vector2.zero;
            if (horizontal)
                outward.x = Mathf.Sign(column - (gridManager.Columns - 1) * 0.5f);
            if (vertical)
                outward.y = Mathf.Sign((gridManager.Rows - 1) * 0.5f - row);
            // Small outward motion makes the tiles look like a burst,
            // without changing the real grid or causing large overdraw.
            Vector2 drift = outward.normalized * size * 0.23f;
            Spawn(centre, new Vector2(size, size), tint,
                delay, tileBurstDuration, false, drift);
        }

        private void Spawn(Vector2 centre, Vector2 size, Color tint,
            float delay, float duration, bool beam, Vector2 drift)
        {
            if (active.Count >= maximumConcurrentElements) return;
            Element item = Rent();
            if (item == null) return;
            item.Position = centre;
            item.FullSize = size;
            item.Drift = drift;
            item.Tint = tint;
            item.Age = 0f;
            item.Delay = delay;
            item.Duration = duration;
            item.Beam = beam;
            item.Rect.anchoredPosition = centre;
            item.Rect.sizeDelta = beam ? Vector2.zero : size;
            item.Rect.localScale = Vector3.one;
            Color hidden = tint;
            hidden.a = 0f;
            item.Image.color = hidden;
            item.Image.raycastTarget = false;
            item.Rect.gameObject.SetActive(true);
            item.Rect.SetAsLastSibling();
            active.Add(item);
        }

        private Element Rent()
        {
            if (!EnsureLayer()) return null;
            if (pool.Count > 0)
                return pool.Pop();

            var go = new GameObject("LineClear_Pulse",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.SetParent(fxLayer, false);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            Image img = go.GetComponent<Image>();
            img.raycastTarget = false;
            return new Element { Rect = rect, Image = img };
        }

        private void RecycleAt(int index)
        {
            Element item = active[index];
            active.RemoveAt(index);
            if (item.Rect == null) return;
            item.Rect.gameObject.SetActive(false);
            pool.Push(item);
        }

        private bool EnsureLayer()
        {
            if (fxLayer != null) return true;
            RectTransform board = transform as RectTransform;
            if (board == null) return false;
            Transform old = board.Find("LineClearFXLayer");
            if (old != null)
                fxLayer = old as RectTransform;
            if (fxLayer == null)
            {
                var go = new GameObject("LineClearFXLayer", typeof(RectTransform));
                fxLayer = go.GetComponent<RectTransform>();
                fxLayer.SetParent(board, false);
            }
            fxLayer.anchorMin = Vector2.zero;
            fxLayer.anchorMax = Vector2.one;
            fxLayer.offsetMin = Vector2.zero;
            fxLayer.offsetMax = Vector2.zero;
            fxLayer.localScale = Vector3.one;
            fxLayer.localRotation = Quaternion.identity;
            fxLayer.SetAsLastSibling();
            return true;
        }

        private bool EnsureViews()
        {
            if (gridManager == null || gridRoot == null ||
                gridManager.GetCell(0, 0) == null) return false;
            if (cellViews != null &&
                cellViews.GetLength(0) == gridManager.Rows &&
                cellViews.GetLength(1) == gridManager.Columns)
                return true;

            cellViews = new GridCellView[gridManager.Rows, gridManager.Columns];
            foreach (GridCellView view in gridRoot.GetComponentsInChildren<GridCellView>(true))
            {
                GridCell cell = view.Cell;
                if (cell == null || !gridManager.IsInsideGrid(cell.Row, cell.Column))
                    continue;
                cellViews[cell.Row, cell.Column] = view;
            }

            for (int row = 0; row < gridManager.Rows; row++)
            for (int col = 0; col < gridManager.Columns; col++)
            {
                if (cellViews[row, col] == null)
                {
                    cellViews = null;
                    return false;
                }
            }
            return true;
        }

        private bool TryGetCenter(int row, int column, out Vector2 centre)
        {
            centre = Vector2.zero;
            if (fxLayer == null || cellViews == null ||
                !gridManager.IsInsideGrid(row, column)) return false;
            GridCellView cell = cellViews[row, column];
            if (cell == null) return false;
            RectTransform rect = cell.GetComponent<RectTransform>();
            centre = fxLayer.InverseTransformPoint(rect.TransformPoint(rect.rect.center));
            return true;
        }

        private float GetCellSize(int row, int column)
        {
            if (cellViews == null || !gridManager.IsInsideGrid(row, column) ||
                cellViews[row, column] == null) return 40f;
            RectTransform rect = cellViews[row, column].GetComponent<RectTransform>();
            return Mathf.Max(1f, Mathf.Min(rect.rect.width, rect.rect.height));
        }

        // Preview commands do not change the board, score, saves, or rewards.
        [ContextMenu("DEBUG/Preview Horizontal Clear (row 3)")]
        private void PreviewHorizontal()
        {
            if (!Application.isPlaying || !EnsureViews() || !EnsureLayer()) return;
            int row = Mathf.Min(3, gridManager.Rows - 1);
            SpawnRowBeam(row);
            for (int col = 0; col < gridManager.Columns; col++)
                SpawnTile(row, col, horizontalColor,
                    Mathf.Abs(col - (gridManager.Columns - 1) * 0.5f) /
                    Mathf.Max(1f, (gridManager.Columns - 1) * 0.5f) * edgeWaveDelay,
                    true, false);
        }

        [ContextMenu("DEBUG/Preview Vertical Clear (column 4)")]
        private void PreviewVertical()
        {
            if (!Application.isPlaying || !EnsureViews() || !EnsureLayer()) return;
            int col = Mathf.Min(4, gridManager.Columns - 1);
            SpawnColumnBeam(col);
            for (int row = 0; row < gridManager.Rows; row++)
                SpawnTile(row, col, verticalColor,
                    Mathf.Abs(row - (gridManager.Rows - 1) * 0.5f) /
                    Mathf.Max(1f, (gridManager.Rows - 1) * 0.5f) * edgeWaveDelay,
                    false, true);
        }

        [ContextMenu("DEBUG/Preview Cross Clear (row 3 + column 4)")]
        private void PreviewCross()
        {
            if (!Application.isPlaying || !EnsureViews() || !EnsureLayer()) return;
            int row = Mathf.Min(3, gridManager.Rows - 1);
            int col = Mathf.Min(4, gridManager.Columns - 1);
            SpawnRowBeam(row);
            SpawnColumnBeam(col);
            for (int c = 0; c < gridManager.Columns; c++)
            {
                Color tint = c == col ? intersectionColor : horizontalColor;
                float delay = Mathf.Abs(c - (gridManager.Columns - 1) * 0.5f) /
                    Mathf.Max(1f, (gridManager.Columns - 1) * 0.5f) * edgeWaveDelay;
                SpawnTile(row, c, tint, delay, true, c == col);
            }
            for (int r = 0; r < gridManager.Rows; r++)
            {
                if (r == row) continue; // Intersection tile is drawn only once.
                float delay = Mathf.Abs(r - (gridManager.Rows - 1) * 0.5f) /
                    Mathf.Max(1f, (gridManager.Rows - 1) * 0.5f) * edgeWaveDelay;
                SpawnTile(r, col, verticalColor, delay, false, true);
            }
        }
    }
}
