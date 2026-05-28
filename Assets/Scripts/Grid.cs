using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Grid : MonoBehaviour
{
    public int width = 10;
    public int height = 10;
    public int mineCount = 15; // Increased density for a more "classic" feel
    public float revealDelay = 0.05f;

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

    void Start()
    {
        if (unrevealedSprite != null)
        {
            spacing = unrevealedSprite.bounds.size.x;
        }

        GenerateGrid();
    }

    void GenerateGrid()
    {
        cells = new Cell[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3 position = new Vector3(x * spacing, y * spacing, 0);
                Cell cell = Instantiate(cellPrefab, position, Quaternion.identity, transform);
                
                cell.numberSprites = numberSprites;
                cell.emptyRevealedSprite = emptyRevealedSprite;
                cell.flagSprite = flagSprite;
                cell.mineSprite = mineSprite;
                cell.unrevealedSprite = unrevealedSprite;

                cell.Setup(x, y, this);
                cells[x, y] = cell;
            }
        }

        // Center the camera
        float centerX = (width - 1) * spacing / 2f;
        float centerY = (height - 1) * spacing / 2f;
        Camera.main.transform.position = new Vector3(centerX, centerY, -10);
        
        // Adjust camera size to fit the grid
        Camera.main.orthographicSize = Mathf.Max(width, height) * spacing / 2f + 1f;
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
