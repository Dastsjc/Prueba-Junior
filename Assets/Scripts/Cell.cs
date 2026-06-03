using UnityEngine;

namespace Buscaminas.Gameplay
{
    /// <summary>
    /// View component for a single Minesweeper cell. Renders the correct sprite
    /// based on its state and delegates input to <see cref="GridManager"/>.
    /// </summary>
    public class Cell : MonoBehaviour
    {
        /// <summary>Grid x-coordinate of this cell.</summary>
        public int X { get; private set; }

        /// <summary>Grid y-coordinate of this cell.</summary>
        public int Y { get; private set; }

        /// <summary>Whether this cell contains a mine.</summary>
        public bool IsMine { get; internal set; }

        /// <summary>Whether this cell has been revealed.</summary>
        public bool IsRevealed { get; internal set; }

        /// <summary>Whether this cell is flagged by the player.</summary>
        public bool IsFlagged { get; internal set; }

        /// <summary>Number of mines in adjacent cells.</summary>
        public int AdjacentMines { get; internal set; }

        private SpriteRenderer spriteRenderer;
        private GridManager grid;

        [Header("Sprites")]
        [SerializeField] private Sprite[] numberSprites;
        [SerializeField] private Sprite emptyRevealedSprite;
        [SerializeField] private Sprite flagSprite;
        [SerializeField] private Sprite mineSprite;
        [SerializeField] private Sprite unrevealedSprite;

        /// <summary>
        /// Initializes the cell with its grid position and parent manager.
        /// Called by <see cref="GridManager.BuildCells"/>.
        /// </summary>
        public void Setup(int x, int y, GridManager grid)
        {
            X = x;
            Y = y;
            this.grid = grid;
            spriteRenderer = GetComponent<SpriteRenderer>();
            UpdateVisuals();
        }

        /// <summary>
        /// Updates the sprite renderer to reflect the current cell state
        /// (unrevealed, flagged, revealed with number, revealed mine, or empty).
        /// </summary>
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
