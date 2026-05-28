using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    void Start()
    {
        CalculateScaleAndSpacing();
        GenerateGrid();
    }

    void CalculateScaleAndSpacing()
    {
        if (unrevealedSprite == null) return;

        // Get camera dimensions in world units
        float camHeight = Camera.main.orthographicSize * 2f;
        float camWidth = camHeight * Camera.main.aspect;

        // Reserve space for UI (using the screenUsage percentage)
        float availableWidth = camWidth * screenUsage;
        float availableHeight = camHeight * screenUsage;

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

        // Calculate offset to center the grid
        float gridWidth = (width - 1) * spacing;
        float gridHeight = (height - 1) * spacing;
        
        // Shift grid slightly to the left to leave room for UI on the right
        float xOffset = -gridWidth / 2f - (Camera.main.orthographicSize * Camera.main.aspect * (1f - screenUsage) / 2f);
        float yOffset = -gridHeight / 2f;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3 position = new Vector3(xOffset + (x * spacing), yOffset + (y * spacing), 0);
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
        int count = 0;
        int maxAttempts = 1000;
        int attempts = 0;

        while (count < mineCount && attempts < maxAttempts)
        {
            attempts++;
            int x = Random.Range(0, width);
            int y = Random.Range(0, height);

            // Avoid the 3x3 area around the first click to ensure a "0" start
            bool inAvoidArea = (x >= avoidX - 1 && x <= avoidX + 1 && y >= avoidY - 1 && y <= avoidY + 1);

            if (!cells[x, y].isMine && !inAvoidArea)
            {
                cells[x, y].isMine = true;
                count++;
            }
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

        if (firstClick)
        {
            firstClick = false;
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

        cells[x, y].isFlagged = !cells[x, y].isFlagged;
        cells[x, y].UpdateVisuals();
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
        if (win)
        {
            Debug.Log("You Win!");
        }
        else
        {
            Debug.Log("Game Over!");
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
