using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace Buscaminas.Gameplay
{
    /// <summary>
    /// MonoBehaviour view/controller for the Minesweeper grid. Handles cell
    /// instantiation, layout, animation, UI, and delegates game logic to <see cref="Board"/>.
    /// </summary>
    public class GridManager : MonoBehaviour
    {
        [SerializeField] private int width = 10;
        [SerializeField] private int height = 10;
        [SerializeField] private int mineCount = 15;
        [SerializeField] private float revealDelay = 0.05f;
        [Range(0.1f, 0.9f)]
        [SerializeField] private float screenUsage = 0.8f;

        [SerializeField] private Cell cellPrefab;
        [SerializeField] private GameObject explosionPrefab;
        private Cell[,] cells;

        [Header("Sprites")]
        [SerializeField] private Sprite[] numberSprites;
        [SerializeField] private Sprite emptyRevealedSprite;
        [SerializeField] private Sprite flagSprite;
        [SerializeField] private Sprite mineSprite;
        [SerializeField] private Sprite unrevealedSprite;

        private Board board;

        /// <summary>The underlying <see cref="Board"/> holding game state.</summary>
        public Board Board => board;

        /// <summary>Grid width in cells.</summary>
        public int Width => width;

        /// <summary>Grid height in cells.</summary>
        public int Height => height;

        /// <summary>Total mine count for this grid.</summary>
        public int MineCount => mineCount;

        private float spacing = 1f;
        private float cellScale = 1.0f;

        /// <summary>Fired when the player wins.</summary>
        public event Action OnWin;

        /// <summary>Fired when the player hits a mine.</summary>
        public event Action OnLose;

        /// <summary>Fired when the grid is regenerated (restart).</summary>
        public event Action OnRestart;

        [Header("UI Constraints")]
        [SerializeField] private RectTransform gameArea;
        [SerializeField] private RectTransform gameLine;

        [Header("Timer")]
        public TextMeshProUGUI timerText;
        private float elapsedTime = 0f;
        private bool isTimerActive = false;

        [Header("Flags")]
        [SerializeField] private TextMeshProUGUI flagText;

        /// <summary>Number of flags remaining.</summary>
        public int Flags => board != null ? board.Flags : mineCount;

        private Vector3 gridCenter;
        private Vector2 lastScreenSize;

        void Start()
        {
            board = new Board(width, height, mineCount);
            board.OnWin += () => OnWin?.Invoke();
            board.OnLose += () => { OnLose?.Invoke(); RevealAllMineViews(); };
            lastScreenSize = new Vector2(Screen.width, Screen.height);
            CalculateScaleAndSpacing();
            GenerateGrid();
            StartTimer();
        }

        void Update()
        {
            if (Screen.width != lastScreenSize.x || Screen.height != lastScreenSize.y)
            {
                lastScreenSize = new Vector2(Screen.width, Screen.height);
                RepositionGrid();
            }

            if (isTimerActive)
            {
                elapsedTime += Time.deltaTime;
                UpdateTimerUI();
            }
        }

        void StartTimer()
        {
            elapsedTime = 0f;
            isTimerActive = true;
            UpdateTimerUI();
        }

        void UpdateTimerUI()
        {
            if (timerText != null)
            {
                timerText.text = Mathf.FloorToInt(elapsedTime).ToString();
            }
        }

        void UpdateFlagUI()
        {
            if (flagText != null)
            {
                flagText.text = Flags.ToString();
            }
        }

        /// <summary>
        /// Recalculates cell positions and scales. Called on screen resize.
        /// </summary>
        public void RepositionGrid()
        {
            if (cells == null) return;
            CalculateScaleAndSpacing();

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (cells[x, y] != null)
                    {
                        cells[x, y].transform.position = GetCellWorldPosition(x, y);
                        cells[x, y].transform.localScale = new Vector3(cellScale, cellScale, 1f);
                    }
                }
            }
        }

        void CalculateScaleAndSpacing()
        {
            if (unrevealedSprite == null) return;

            float availableWidth, availableHeight;

            if (gameArea != null && gameLine != null)
            {
                Vector3[] cornersArea = new Vector3[4];
                Vector3[] cornersLine = new Vector3[4];
                gameArea.GetWorldCorners(cornersArea);
                gameLine.GetWorldCorners(cornersLine);

                float minX = Mathf.Max(cornersArea[0].x, cornersLine[0].x);
                float minY = Mathf.Max(cornersArea[0].y, cornersLine[0].y);
                float maxX = Mathf.Min(cornersArea[2].x, cornersLine[2].x);
                float maxY = Mathf.Min(cornersArea[2].y, cornersLine[2].y);

                availableWidth = maxX - minX;
                availableHeight = maxY - minY;
                gridCenter = new Vector3((minX + maxX) / 2f, (minY + maxY) / 2f, 0);
            }
            else
            {
                float camHeight = Camera.main.orthographicSize * 2f;
                float camWidth = camHeight * Camera.main.aspect;

                availableWidth = camWidth * screenUsage;
                availableHeight = camHeight * screenUsage;

                float xOffset = -(camWidth * (1f - screenUsage) / 2f);
                gridCenter = new Vector3(xOffset, 0, 0);
            }

            float spriteSize = unrevealedSprite.bounds.size.x;

            float scaleX = availableWidth / (width * spriteSize);
            float scaleY = availableHeight / (height * spriteSize);

            cellScale = Mathf.Min(scaleX, scaleY);
            spacing = spriteSize * cellScale;
        }

        Vector3 GetCellWorldPosition(int x, int y)
        {
            float totalGridWidth = (width - 1) * spacing;
            float totalGridHeight = (height - 1) * spacing;
            float startX = gridCenter.x - (totalGridWidth / 2f);
            float startY = gridCenter.y - (totalGridHeight / 2f);
            return new Vector3(startX + (x * spacing), startY + (y * spacing), 0);
        }

        void BuildCells()
        {
            cells = new Cell[width, height];
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Vector3 position = GetCellWorldPosition(x, y);
                    Cell cell = Instantiate(cellPrefab, position, Quaternion.identity, transform);
                    cell.transform.localScale = new Vector3(cellScale, cellScale, 1f);
                    cell.Setup(x, y, this);
                    cells[x, y] = cell;
                }
            }
        }

        void GenerateGrid()
        {
            BuildCells();
        }

        /// <summary>
        /// Destroys all existing cells and creates a fresh board and grid.
        /// Called by the restart button.
        /// </summary>
        public void RegenerateGrid()
        {
            board = new Board(width, height, mineCount);
            board.OnWin += () => OnWin?.Invoke();
            board.OnLose += () => { OnLose?.Invoke(); RevealAllMineViews(); };
            OnRestart?.Invoke();
            UpdateFlagUI();
            StartTimer();

            for (int i = transform.childCount - 1; i >= 0; i--)
                Destroy(transform.GetChild(i).gameObject);

            BuildCells();
        }

        /// <summary>
        /// Reveals the cell at (x, y). On a mine hit, reveals all mines and stops the timer.
        /// Otherwise animates the BFS reveal.
        /// </summary>
        public void RevealCell(int x, int y)
        {
            if (board.IsGameOver || board.IsRevealed(x, y) || board.IsFlagged(x, y)) return;

            var levels = board.Reveal(x, y);
            if (levels == null)
            {
                if (board.IsGameOver)
                {
                    cells[x, y].IsRevealed = true;
                    cells[x, y].UpdateVisuals();
                    isTimerActive = false;

                    if (explosionPrefab != null)
                        Instantiate(explosionPrefab, cells[x, y].transform.position, Quaternion.identity);

                    RevealAllMineViews();
                }
                return;
            }

            StartCoroutine(RevealRoutine(levels));
        }

        private IEnumerator RevealRoutine(List<List<Vector2Int>> levels)
        {
            foreach (var level in levels)
            {
                foreach (var pos in level)
                {
                    cells[pos.x, pos.y].IsRevealed = true;
                    cells[pos.x, pos.y].AdjacentMines = board.AdjacentMines(pos.x, pos.y);
                    cells[pos.x, pos.y].IsMine = board.IsMine(pos.x, pos.y);
                    cells[pos.x, pos.y].UpdateVisuals();
                }
                yield return new WaitForSeconds(revealDelay);
            }
        }

        /// <summary>
        /// Toggles the flag on the cell at (x, y). Updates the UI counter.
        /// </summary>
        public void ToggleFlag(int x, int y)
        {
            if (board.IsGameOver || board.IsRevealed(x, y)) return;

            board.ToggleFlag(x, y);
            cells[x, y].IsFlagged = board.IsFlagged(x, y);
            cells[x, y].UpdateVisuals();
            UpdateFlagUI();
        }

        void RevealAllMineViews()
        {
            if (cells == null) return;
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (board.IsMine(x, y))
                    {
                        cells[x, y].IsMine = true;
                        cells[x, y].IsRevealed = true;
                        cells[x, y].UpdateVisuals();
                    }
                }
            }
        }
    }
}
