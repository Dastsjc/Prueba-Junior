using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class GridManager : MonoBehaviour
{
    public int width = 10;
    public int height = 10;
    public int mineCount = 15;
    public float revealDelay = 0.05f;
    [Range(0.1f, 0.9f)]
    public float screenUsage = 0.8f; // How much of the screen the grid should take

    public Cell cellPrefab;
    private Cell[,] cells;

    [Header("Sprites")]
    public Sprite[] numberSprites; // 1 to 8
    public Sprite emptyRevealedSprite;
    public Sprite flagSprite;
    public Sprite mineSprite;
    public Sprite unrevealedSprite;

    private Board board;
    public Board Board => board;

    private float spacing = 1f;
    private float cellScale = 1.0f;
    
    public event Action OnWin;
    public event Action OnLose;
    public event Action OnRestart;


    [Header("UI Constraints")]
    public RectTransform gameArea;
    public RectTransform gameLine;

    [Header("Timer")]
    public TextMeshProUGUI timerText;
    private float elapsedTime = 0f;
    private bool isTimerActive = false;

    [Header("Flags")]
    public TextMeshProUGUI flagText;
    [HideInInspector]
    public int flags { get { return board != null ? board.Flags : mineCount; } }
    


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
            flagText.text = flags.ToString();
        }
    }

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

            // Find the intersection (most constrained bounds)
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
            // Fallback to camera-based scaling if UI constraints are not provided
            float camHeight = Camera.main.orthographicSize * 2f;
            float camWidth = camHeight * Camera.main.aspect;

            availableWidth = camWidth * screenUsage;
            availableHeight = camHeight * screenUsage;
            
            // Original centering logic
            float xOffset = -(camWidth * (1f - screenUsage) / 2f);
            gridCenter = new Vector3(xOffset, 0, 0);
        }

        // Sprite base size
        float spriteSize = unrevealedSprite.bounds.size.x;

        // Calculate scale needed to fit width and height
        float scaleX = availableWidth / (width * spriteSize);
        float scaleY = availableHeight / (height * spriteSize);

        // Use the smaller scale to ensure it fits in both dimensions
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
                cell.numberSprites = numberSprites;
                cell.emptyRevealedSprite = emptyRevealedSprite;
                cell.flagSprite = flagSprite;
                cell.mineSprite = mineSprite;
                cell.unrevealedSprite = unrevealedSprite;
                cell.Setup(x, y, this);
                cells[x, y] = cell;
            }
        }
    }

    void GenerateGrid()
    {
        BuildCells();
    }

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

    public void RevealCell(int x, int y)
    {
        if (board.IsGameOver || board.IsRevealed(x, y) || board.IsFlagged(x, y)) return;

        var levels = board.Reveal(x, y);
        if (levels == null)
        {
            if (board.IsGameOver)
            {
                cells[x, y].isRevealed = true;
                cells[x, y].UpdateVisuals();
                isTimerActive = false;
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
                cells[pos.x, pos.y].isRevealed = true;
                cells[pos.x, pos.y].adjacentMines = board.AdjacentMines(pos.x, pos.y);
                cells[pos.x, pos.y].isMine = board.IsMine(pos.x, pos.y);
                cells[pos.x, pos.y].UpdateVisuals();
            }
            yield return new WaitForSeconds(revealDelay);
        }
    }

    public void ToggleFlag(int x, int y)
    {
        if (board.IsGameOver || board.IsRevealed(x, y)) return;

        board.ToggleFlag(x, y);
        cells[x, y].isFlagged = board.IsFlagged(x, y);
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
                    cells[x, y].isMine = true;
                    cells[x, y].isRevealed = true;
                    cells[x, y].UpdateVisuals();
                }
            }
        }
    }
}
