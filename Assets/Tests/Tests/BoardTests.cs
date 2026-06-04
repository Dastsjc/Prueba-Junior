using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Buscaminas.Gameplay;

public class BoardTests
{
    [Test]
    public void Board_PlaceMines_PlacesExactCount()
    {
        var board = new Board(10, 10, 15);
        board.Reveal(5, 5);

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
        board.Reveal(5, 5);

        for (int x = 4; x <= 6; x++)
            for (int y = 4; y <= 6; y++)
                Assert.IsFalse(board.IsMine(x, y), $"Cell ({x},{y}) should not be a mine (in safe area)");
    }

    [Test]
    public void Board_Reveal_MineEndsGame()
    {
        var board = new Board(5, 5, 20);
        board.Reveal(2, 2);

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

        board.Reveal(mineX, mineY);

        Assert.IsTrue(board.IsGameOver, "Game should be over after hitting a mine");
        Assert.IsFalse(board.IsWin, "IsWin should be false on mine hit");
    }

    [Test]
    public void Board_Reveal_FloodFillExpandsThroughZeros()
    {
        var board = new Board(3, 3, 0);
        var levels = board.Reveal(1, 1);

        Assert.IsNotNull(levels, "Reveal should return levels");
        Assert.IsTrue(levels.Count > 0, "Should have at least one BFS level");

        int revealedCount = 0;
        for (int x = 0; x < 3; x++)
            for (int y = 0; y < 3; y++)
                if (board.IsRevealed(x, y)) revealedCount++;

        Assert.AreEqual(9, revealedCount, "All cells should be revealed through flood fill");
    }

    [Test]
    public void Board_Reveal_NonZeroDoesNotExpand()
    {
        var board = new Board(5, 5, 10);
        board.Reveal(0, 0);
        int testX = -1, testY = -1;
        for (int x = 0; x < 5 && testX < 0; x++)
            for (int y = 0; y < 5 && testX < 0; y++)
                if (!board.IsMine(x, y) && !board.IsRevealed(x, y) && board.AdjacentMines(x, y) >= 1)
                { testX = x; testY = y; }

        Assert.IsTrue(testX >= 0, "Should find an unrevealed cell with adjacent mines");

        int revealedBefore = CountRevealed(board);
        var levels = board.Reveal(testX, testY);
        int revealedAfter = CountRevealed(board);

        Assert.IsNotNull(levels);
        Assert.AreEqual(revealedBefore + 1, revealedAfter, "Only the clicked cell should be revealed (non-zero, no expansion)");
    }

    [Test]
    public void Board_ToggleFlag_TogglesAndLimitsFlags()
    {
        var board = new Board(5, 5, 20);
        board.Reveal(2, 2); 

        Assert.AreEqual(20, board.Flags, "Initial flags should equal mineCount");

        int flagX = -1, flagY = -1;
        for (int x = 0; x < 5 && flagX < 0; x++)
            for (int y = 0; y < 5 && flagX < 0; y++)
                if (!board.IsRevealed(x, y) && !board.IsMine(x, y))
                { flagX = x; flagY = y; }

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
        var board = new Board(5, 5, 20);
        board.Reveal(2, 2);

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
        var board = new Board(3, 3, 0);

        bool onWinFired = false;
        board.OnWin += () => onWinFired = true;

        board.Reveal(1, 1);

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
        var board = new Board(5, 5, 20);
        board.Reveal(2, 2);

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

        var board = new Board(3, 3, 1);
        board.Reveal(1, 1);

        var board2 = new Board(5, 5, 1);
        board2.Reveal(2, 2);
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
