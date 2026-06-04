using UnityEngine;

namespace Buscaminas.Gameplay
{

    public class Cell : MonoBehaviour
    {
        public int X { get; private set; }

        public int Y { get; private set; }

        public bool IsMine { get; internal set; }

        public bool IsRevealed { get; internal set; }

        public bool IsFlagged { get; internal set; }

        public int AdjacentMines { get; internal set; }

        private SpriteRenderer spriteRenderer;
        private GridManager grid;

        [Header("Sprites")]
        [SerializeField] private Sprite[] numberSprites;
        [SerializeField] private Sprite emptyRevealedSprite;
        [SerializeField] private Sprite flagSprite;
        [SerializeField] private Sprite mineSprite;
        [SerializeField] private Sprite unrevealedSprite;

        public void Setup(int x, int y, GridManager grid)
        {
            X = x;
            Y = y;
            this.grid = grid;
            spriteRenderer = GetComponent<SpriteRenderer>();
            UpdateVisuals();
        }

        public void UpdateVisuals()
        {
            if (IsRevealed)
            {
                if (IsMine)
                {
                    spriteRenderer.sprite = mineSprite;
                }
                else if (AdjacentMines > 0)
                {
                    if (AdjacentMines - 1 < numberSprites.Length)
                    {
                        spriteRenderer.sprite = numberSprites[AdjacentMines - 1];
                    }
                }
                else
                {
                    spriteRenderer.sprite = emptyRevealedSprite;
                }
            }
            else if (IsFlagged)
            {
                spriteRenderer.sprite = flagSprite;
            }
            else
            {
                spriteRenderer.sprite = unrevealedSprite;
            }
        }

        private float pressTime;
        private bool isPressing;
        private bool flagToggled;
        [SerializeField] private float longPressDuration = 0.5f;

        private void Update()
        {
            if (isPressing && !flagToggled)
            {
                if (Time.time - pressTime >= longPressDuration)
                {
                    grid.ToggleFlag(X, Y);
                    flagToggled = true;
                }
            }
        }

        private void OnMouseDown()
        {
            if (Input.GetMouseButtonDown(0))
            {
                pressTime = Time.time;
                isPressing = true;
                flagToggled = false;
            }
        }

        private void OnMouseUp()
        {
            if (isPressing)
            {
                if (!flagToggled)
                {
                    grid.RevealCell(X, Y);
                }
                isPressing = false;
            }
        }

        private void OnMouseExit()
        {
            isPressing = false;
        }

        private void OnMouseOver()
        {
            if (Input.GetMouseButtonDown(1))
            {
                grid.ToggleFlag(X, Y);
            }
        }
    }
}
