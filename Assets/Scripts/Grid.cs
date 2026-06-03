using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using JetBrains.Annotations;

public class Grid : MonoBehaviour
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

    private bool gameOver = false;
    private bool firstClick = true;
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
    public int flags;
    


    private Vector3 gridCenter;
    private Vector2 lastScreenSize;

    private bool isRegenerated = false;
    

    void Start()
    {
        flags = mineCount;
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

        float totalGridWidth = (width - 1) * spacing;
        float totalGridHeight = (height - 1) * spacing;
        
        float startX = gridCenter.x - (totalGridWidth / 2f);
        float startY = gridCenter.y - (totalGridHeight / 2f);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (cells[x, y] != null)
                {
                    Vector3 position = new Vector3(startX + (x * spacing), startY + (y * spacing), 0);
                    cells[x, y].transform.position = position;
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

    void GenerateGrid()
    {

        cells = new Cell[width, height];

        // Calculate offset to center the grid within gridCenter
        float totalGridWidth = (width - 1) * spacing;
        float totalGridHeight = (height - 1) * spacing;
        
        float startX = gridCenter.x - (totalGridWidth / 2f);
        float startY = gridCenter.y - (totalGridHeight / 2f);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3 position = new Vector3(startX + (x * spacing), startY + (y * spacing), 0);
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

    public void RegenerateGrid()
    {
        gameOver = false;
        isRegenerated = true;
        flags = mineCount;
        OnRestart?.Invoke();
        UpdateFlagUI();
        StartTimer();
        if (this.gameObject.transform.GetChild(0))
        {
            int childs = transform.childCount;
            for (int i = childs - 1; i >= 0; i--)
            {
                GameObject.Destroy(transform.GetChild(i).gameObject);
            }
        }
        cells = new Cell[width, height];

        // Calculate offset to center the grid within gridCenter
        float totalGridWidth = (width - 1) * spacing;
        float totalGridHeight = (height - 1) * spacing;

        float startX = gridCenter.x - (totalGridWidth / 2f);
        float startY = gridCenter.y - (totalGridHeight / 2f);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3 position = new Vector3(startX + (x * spacing), startY + (y * spacing), 0);
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

    void PlaceMines(int avoidX, int avoidY)
    {
        List<Vector2Int> candidates = new List<Vector2Int>();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                bool inAvoidArea = (x >= avoidX - 1 && x <= avoidX + 1 && y >= avoidY - 1 && y <= avoidY + 1);
                if (!inAvoidArea)
                {
                    candidates.Add(new Vector2Int(x, y));
                }
            }
        }

        // Fisher-Yates shuffle
        System.Random rng = new System.Random();
        for (int i = candidates.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            Vector2Int temp = candidates[i];
            candidates[i] = candidates[j];
            candidates[j] = temp;
        }

        int minesToPlace = Mathf.Min(mineCount, candidates.Count);
        for (int i = 0; i < minesToPlace; i++)
        {
            cells[candidates[i].x, candidates[i].y].isMine = true;
        }
    }

    void CalculateNeighbors()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (cells[x, y].isMine) continue;

                int mines = 0;
                for (int xi = -1; xi <= 1; xi++)
                {
                    for (int yi = -1; yi <= 1; yi++)
                    {
                        if (xi == 0 && yi == 0) continue;

                        int nx = x + xi;
                        int ny = y + yi;

                        if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                        {
                            if (cells[nx, ny].isMine) mines++;
                        }
                    }
                }
                cells[x, y].adjacentMines = mines;
            }
        }
    }

    public void RevealCell(int x, int y)
    {
        if (gameOver || cells[x, y].isRevealed || cells[x, y].isFlagged) return;

        if (firstClick || isRegenerated)
        {
            firstClick = false;
            isRegenerated = false;
            PlaceMines(x, y);
            CalculateNeighbors();
        }

        if (cells[x, y].isMine)
        {
            cells[x, y].isRevealed = true;
            cells[x, y].UpdateVisuals();
            GameOver(false);
            return;
        }

        StartCoroutine(RevealRoutine(x, y));
    }

    private IEnumerator RevealRoutine(int startX, int startY)
    {
        Queue<Vector2Int> nodes = new Queue<Vector2Int>();
        nodes.Enqueue(new Vector2Int(startX, startY));

        while (nodes.Count > 0)
        {
            int levelSize = nodes.Count;
            for (int i = 0; i < levelSize; i++)
            {
                Vector2Int current = nodes.Dequeue();
                int x = current.x;
                int y = current.y;

                if (cells[x, y].isRevealed || cells[x, y].isFlagged) continue;

                cells[x, y].isRevealed = true;
                cells[x, y].UpdateVisuals();

                if (cells[x, y].adjacentMines == 0)
                {
                    AddNeighborsToQueue(x, y, nodes);
                }
            }
            yield return new WaitForSeconds(revealDelay);
            CheckWinCondition();
        }
    }

    void AddNeighborsToQueue(int x, int y, Queue<Vector2Int> queue)
    {
        for (int xi = -1; xi <= 1; xi++)
        {
            for (int yi = -1; yi <= 1; yi++)
            {
                if (xi == 0 && yi == 0) continue;

                int nx = x + xi;
                int ny = y + yi;

                if (nx >= 0 && nx < width && ny >= 0 && ny < height && !cells[nx, ny].isRevealed)
                {
                    queue.Enqueue(new Vector2Int(nx, ny));
                }
            }
        }
    }

    public void ToggleFlag(int x, int y)
    {
        if (gameOver || cells[x, y].isRevealed) return;

        if (cells[x, y].isFlagged)
        {
            cells[x, y].isFlagged = false;
            flags++;
            cells[x, y].UpdateVisuals();
        }
        else if (flags > 0)
        {
            cells[x, y].isFlagged = true;
            flags--;
            cells[x, y].UpdateVisuals();
        }

        UpdateFlagUI();
    }

    void CheckWinCondition()
    {
        int revealedCount = 0;
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (cells[x, y].isRevealed && !cells[x, y].isMine) revealedCount++;
            }
        }

        if (revealedCount == (width * height) - mineCount)
        {
            GameOver(true);
        }
    }

    void GameOver(bool win)
    {
        gameOver = true;
        isTimerActive = false;
        if (win)
        {
            OnWin?.Invoke();
        }
        else
        {
            OnLose?.Invoke();
            RevealAllMines();
        }
    }

    void RevealAllMines()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (cells[x, y].isMine)
                {
                    cells[x, y].isRevealed = true;
                    cells[x, y].UpdateVisuals();
                }
            }
        }
    }
}
