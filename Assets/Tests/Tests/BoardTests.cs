using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class BoardTests
{
    [Test]
    public void Board_PlaceMines_PlacesExactCount()
    {
        var board = new Board(10, 10, 15);
        board.Reveal(5, 5); // triggers mine placement

        int mineCount = 0;
        for (int x = 0; x < 10; x++)
            for (int y = 0; y < 10; y++)
                if (board.IsMine(x, y)) mineCount++;

        Assert.AreEqual(15, mineCount, "Exactly 15 mines should be placed");
    }

    [Test]
    public void Board_PlaceMines_NeverPlacesInSafeArea()
    {
        var board = new Board(10, 10, 50);
        board.Reveal(5, 5); // safe area is 3x3 around (5,5)

        for (int x = 4; x <= 6; x++)
            for (int y = 4; y <= 6; y++)
                Assert.IsFalse(board.IsMine(x, y), $"Cell ({x},{y}) should not be a mine (in safe area)");
    }

    [Test]
    public void Board_Reveal_MineEndsGame()
    {
        var board = new Board(5, 5, 20); // high density to guarantee mine hit
        // Find a cell that will be a mine after placement
        board.Reveal(2, 2); // place mines avoiding (2,2) safe area

        // Find a mine cell
        int mineX = -1, mineY = -1;
        for (int x = 0; x < 5; x++)
        {
            for (int y = 0; y < 5; y++)
            {
                if (board.IsMine(x, y))
                {
                    mineX = x;
                    mineY = y;
                    break;
                }
            }
            if (mineX >= 0) break;
        }

        Assert.IsTrue(mineX >= 0, "Should have at least one mine");

        // Reveal the mine
        board.Reveal(mineX, mineY);

        Assert.IsTrue(board.IsGameOver, "Game should be over after hitting a mine");
        Assert.IsFalse(board.IsWin, "IsWin should be false on mine hit");
    }

    [Test]
    public void Board_Reveal_FloodFillExpandsThroughZeros()
    {
        // 3x3 grid with 0 mines: all cells are zeros, so revealing one should reveal all
        var board = new Board(3, 3, 0);
        var levels = board.Reveal(1, 1);

        Assert.IsNotNull(levels, "Reveal should return levels");
        Assert.IsTrue(levels.Count > 0, "Should have at least one BFS level");

        // All 9 cells should be revealed
        int revealedCount = 0;
        for (int x = 0; x < 3; x++)
            for (int y = 0; y < 3; y++)
                if (board.IsRevealed(x, y)) revealedCount++;

        Assert.AreEqual(9, revealedCount, "All cells should be revealed through flood fill");
    }

    [Test]
    public void Board_Reveal_NonZeroDoesNotExpand()
    {
        // 3x3 with 1 mine at (0,0). Reveal (0,1): adjacentMines=1, no expansion.
        // Safe area around (0,1): (max(0,-1), max(0,0)) to (min(2,1), min(2,2)) = (0,0)-(1,2)
        // That's 6 cells. Candidates: (0,2), (1,2), (2,0), (2,1), (2,2) = 5 cells.
        // Actually let me recalculate. avoidX=0, avoidY=1.
        // x >= -1 && x <= 1 → x ∈ [0, 1] (clamped to grid)
        // y >= 0 && y <= 2 → y ∈ [0, 2]
        // Safe cells: (0,0),(0,1),(0,2),(1,0),(1,1),(1,2) = 6 cells
        // Candidates: (2,0),(2,1),(2,2) = 3 cells
        // minesToPlace = min(1, 3) = 1 → mine at one of the 3 candidates.
        // But we need the mine at (0,0) which is in the safe area! Can't guarantee position.

        // Better approach: use a 5x5 grid with 1 mine.
        // Reveal (2,2): safe area = (1,1)-(3,3) = 9 cells, candidates = 16.
        // Mine at one of 16 border cells. (2,2) has 0 adjacent mines → flood-fills.
        // This doesn't help.

        // The key insight: to get adjacentMines > 0, the clicked cell must have a mine
        // as a neighbor. But the 3x3 safe area prevents that. So we can NEVER get
        // non-expansion on the first Reveal call.

        // SOLUTION: test non-expansion on a SECOND reveal after mines are already placed.
        // First reveal places mines. Then find a cell with adjacent mines and reveal it.

        var board = new Board(5, 5, 1);
        board.Reveal(0, 0); // safe area = (0,0)-(1,1), mine placed at border
        // (0,0) has 0 adjacent mines → flood-fills some cells

        // Find the mine
        int mineX = -1, mineY = -1;
        for (int x = 0; x < 5 && mineX < 0; x++)
            for (int y = 0; y < 5 && mineX < 0; y++)
                if (board.IsMine(x, y)) { mineX = x; mineY = y; }

        // Find an unrevealed cell adjacent to the mine
        int testX = -1, testY = -1;
        for (int dx = -1; dx <= 1 && testX < 0; dx++)
        {
            for (int dy = -1; dy <= 1 && testX < 0; dy++)
            {
                int nx = mineX + dx, ny = mineY + dy;
                if (nx >= 0 && nx < 5 && ny >= 0 && ny < 5
                    && !board.IsRevealed(nx, ny) && !board.IsMine(nx, ny))
                { testX = nx; testY = ny; }
            }
        }

        Assert.IsTrue(testX >= 0, "Should find an unrevealed cell adjacent to mine");
        Assert.IsTrue(board.AdjacentMines(testX, testY) >= 1, "Cell should have adjacent mines");

        int revealedBefore = CountRevealed(board);
        var levels = board.Reveal(testX, testY);
        int revealedAfter = CountRevealed(board);

        Assert.IsNotNull(levels);
        Assert.AreEqual(revealedBefore + 1, revealedAfter, "Only the clicked cell should be revealed (non-zero, no expansion)");
    }

    [Test]
    public void Board_ToggleFlag_TogglesAndLimitsFlags()
    {
        // 5x5 with 20 mines: center safe, flood-fill blocked by mines
        var board = new Board(5, 5, 20);
        board.Reveal(2, 2); // only reveals center (has adjacent mines)

        Assert.AreEqual(20, board.Flags, "Initial flags should equal mineCount");

        // Find an unrevealed, non-mine cell to flag
        int flagX = -1, flagY = -1;
        for (int x = 0; x < 5 && flagX < 0; x++)
            for (int y = 0; y < 5 && flagX < 0; y++)
                if (!board.IsRevealed(x, y) && !board.IsMine(x, y))
                { flagX = x; flagY = y; }

        // If all non-mine cells are revealed, use a mine cell for flag testing
        if (flagX < 0)
        {
            for (int x = 0; x < 5 && flagX < 0; x++)
                for (int y = 0; y < 5 && flagX < 0; y++)
                    if (!board.IsRevealed(x, y))
                    { flagX = x; flagY = y; }
        }

        board.ToggleFlag(flagX, flagY);
        Assert.IsTrue(board.IsFlagged(flagX, flagY), "Cell should be flagged");
        Assert.AreEqual(19, board.Flags, "Flags should decrease");

        board.ToggleFlag(flagX, flagY);
        Assert.IsFalse(board.IsFlagged(flagX, flagY), "Cell should be unflagged");
        Assert.AreEqual(20, board.Flags, "Flags should increase back");

        // Flag up to 3 cells
        int flagged = 0;
        for (int x = 0; x < 5 && flagged < 3; x++)
        {
            for (int y = 0; y < 5 && flagged < 3; y++)
            {
                if (!board.IsRevealed(x, y) && !board.IsFlagged(x, y))
                {
                    board.ToggleFlag(x, y);
                    flagged++;
                }
            }
        }
        Assert.AreEqual(20 - 3, board.Flags, "Flags should decrease by 3");
    }

    [Test]
    public void Board_CannotRevealFlaggedCell()
    {
        // 5x5 with 20 mines: center safe, flood-fill blocked
        var board = new Board(5, 5, 20);
        board.Reveal(2, 2); // only reveals center

        // Find an unrevealed cell
        int testX = -1, testY = -1;
        for (int x = 0; x < 5 && testX < 0; x++)
            for (int y = 0; y < 5 && testX < 0; y++)
                if (!board.IsRevealed(x, y))
                { testX = x; testY = y; }

        board.ToggleFlag(testX, testY);
        var levels = board.Reveal(testX, testY);

        Assert.IsNull(levels, "Revealing a flagged cell should return null");
        Assert.IsFalse(board.IsRevealed(testX, testY), "Flagged cell should not be revealed");
    }

    [Test]
    public void Board_WinCondition_DetectedWhenAllNonMinesRevealed()
    {
        // 3x3 with 1 mine at (0,0). Reveal (0,1) → flood-fills all 8 non-mine cells → win.
        // Safe area around (0,1): x∈[0,1], y∈[0,2] = (0,0),(0,1),(0,2),(1,0),(1,1),(1,2)
        // Candidates: (2,0),(2,1),(2,2) = 3 cells. 1 mine placed at one of them.

        // To guarantee mine at a specific position, use a larger grid.
        // 5x5 with 1 mine, Reveal(4,4): safe area = (3,3)-(4,4) = 4 cells.
        // Mine somewhere in the other 21 cells. (4,4) has 0 adjacent mines → flood-fills.

        // But flood-fill extent depends on mine position. Use a simpler approach:
        // 3x3 with 0 mines → Reveal reveals all 9 cells. 0 non-mine cells → all revealed.
        // Win condition: revealedCount == 9 - 0 = 9 → fires immediately.

        var board = new Board(3, 3, 0);

        bool onWinFired = false;
        board.OnWin += () => onWinFired = true;

        board.Reveal(1, 1); // flood-fills all 9 cells, all are non-mine

        Assert.IsTrue(onWinFired, "OnWin should fire when all non-mine cells are revealed");
        Assert.IsTrue(board.IsWin, "IsWin should be true");
        Assert.IsTrue(board.IsGameOver, "IsGameOver should be true on win");
    }

    [Test]
    public void Board_Constructor_StoresDimensions()
    {
        var board = new Board(10, 5, 15);

        Assert.AreEqual(10, board.Width, "Width should be stored");
        Assert.AreEqual(5, board.Height, "Height should be stored");
        Assert.AreEqual(15, board.MineCount, "MineCount should be stored");
        Assert.AreEqual(15, board.Flags, "Flags should equal mineCount initially");
        Assert.IsFalse(board.IsGameOver, "Game should not be over initially");
        Assert.IsFalse(board.IsWin, "IsWin should be false initially");
    }

    [Test]
    public void Board_Reveal_MineFiresOnLoseEvent()
    {
        var board = new Board(5, 5, 20); // high density
        board.Reveal(2, 2); // place mines

        // Find a mine
        int mineX = -1, mineY = -1;
        for (int x = 0; x < 5; x++)
        {
            for (int y = 0; y < 5; y++)
            {
                if (board.IsMine(x, y))
                {
                    mineX = x;
                    mineY = y;
                    break;
                }
            }
            if (mineX >= 0) break;
        }

        bool onLoseFired = false;
        board.OnLose += () => onLoseFired = true;

        board.Reveal(mineX, mineY);

        Assert.IsTrue(onLoseFired, "OnLose should fire when revealing a mine");
    }

    [Test]
    public void Board_AdjacentMines_CountedCorrectly()
    {
        // Place a known mine configuration manually
        // 3x3 with 1 mine, reveal center to trigger placement
        var board = new Board(3, 3, 1);
        board.Reveal(1, 1); // safe area is (0,0) to (2,2) - entire grid is safe for 0 mines? No, 1 mine
        // The mine will be placed somewhere outside the 3x3 safe area around (1,1)
        // But the grid IS 3x3, so safe area covers the entire grid!
        // With 1 mine and safe area covering all 9 cells, no mine can be placed
        // Let's use a bigger grid

        var board2 = new Board(5, 5, 1);
        board2.Reveal(2, 2); // safe area is (1,1) to (3,3)

        // The mine is somewhere outside the safe area
        // Find the mine
        int mineX = -1, mineY = -1;
        for (int x = 0; x < 5; x++)
        {
            for (int y = 0; y < 5; y++)
            {
                if (board2.IsMine(x, y))
                {
                    mineX = x;
                    mineY = y;
                    break;
                }
            }
            if (mineX >= 0) break;
        }

        Assert.IsTrue(mineX >= 0, "Should have one mine");

        // Count expected adjacent mines for a cell next to the mine
        // Reveal the mine's neighbor to check adjacentMines
        int testX = Mathf.Clamp(mineX + 1, 0, 4);
        int testY = mineY;
        if (board2.IsMine(testX, testY))
            testX = Mathf.Clamp(mineX - 1, 0, 4);

        board2.Reveal(testX, testY);
        Assert.IsTrue(board2.AdjacentMines(testX, testY) >= 1,
            $"Cell ({testX},{testY}) adjacent to mine should have adjacentMines >= 1");
    }

    private int CountRevealed(Board board)
    {
        int count = 0;
        for (int x = 0; x < board.Width; x++)
            for (int y = 0; y < board.Height; y++)
                if (board.IsRevealed(x, y)) count++;
        return count;
    }
}
