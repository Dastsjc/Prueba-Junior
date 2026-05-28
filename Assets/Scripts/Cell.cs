using UnityEngine;

public class Cell : MonoBehaviour
{
    public int x;
    public int y;
    public bool isMine;
    public bool isRevealed;
    public bool isFlagged;
    public int adjacentMines;

    private SpriteRenderer spriteRenderer;
    private Grid grid;

    [Header("Sprites")]
    public Sprite[] numberSprites; // 1 to 8
    public Sprite emptyRevealedSprite;
    public Sprite flagSprite;
    public Sprite mineSprite;
    public Sprite unrevealedSprite;

    public void Setup(int x, int y, Grid grid)
    {
        this.x = x;
        this.y = y;
        this.grid = grid;
        this.spriteRenderer = GetComponent<SpriteRenderer>();
        UpdateVisuals();
    }

    public void UpdateVisuals()
    {
        if (isRevealed)
        {
            if (isMine)
            {
                spriteRenderer.sprite = mineSprite;
            }
            else if (adjacentMines > 0)
            {
                // adjacentMines 1 corresponds to numberSprites[0]
                if (adjacentMines - 1 < numberSprites.Length)
                {
                    spriteRenderer.sprite = numberSprites[adjacentMines - 1];
                }
            }
            else
            {
                spriteRenderer.sprite = emptyRevealedSprite;
            }
        }
        else if (isFlagged)
        {
            spriteRenderer.sprite = flagSprite;
        }
        else
        {
            spriteRenderer.sprite = unrevealedSprite;
        }
    }

    private void OnMouseOver()
    {
        if (Input.GetMouseButtonDown(0))
        {
            grid.RevealCell(x, y);
        }
        else if (Input.GetMouseButtonDown(1))
        {
            grid.ToggleFlag(x, y);
        }
    }
}
