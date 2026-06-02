using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class GridTests
{
    private GameObject gridGameObject;
    private GameObject cameraGameObject;
    private Grid grid;
    private Cell cellPrefab;

    [SetUp]
    public void SetUp()
    {
        cameraGameObject = new GameObject("Main Camera");
        cameraGameObject.tag = "MainCamera";
        cameraGameObject.AddComponent<Camera>();

        gridGameObject = new GameObject("Grid");
        grid = gridGameObject.AddComponent<Grid>();
        
        // Create a dummy cell prefab for testing
        GameObject cellGo = new GameObject("CellPrefab");
        cellGo.AddComponent<SpriteRenderer>();
        cellPrefab = cellGo.AddComponent<Cell>();
        grid.cellPrefab = cellPrefab;

        // Assign some dummy sprites to avoid null references in UpdateVisuals
        grid.unrevealedSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.zero);
        grid.emptyRevealedSprite = Sprite.Create(Texture2D.blackTexture, new Rect(0, 0, 1, 1), Vector2.zero);
        grid.mineSprite = Sprite.Create(Texture2D.redTexture, new Rect(0, 0, 1, 1), Vector2.zero);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(gridGameObject);
        Object.DestroyImmediate(cameraGameObject);
        Object.DestroyImmediate(cellPrefab.gameObject);
    }

    [Test]
    public void GridDimensions_10x5_ResultsInExactMatrixDimensions()
    {
        // Arrange
        grid.width = 10;
        grid.height = 5;

        // Act
        MethodInfo calcMethod = typeof(Grid).GetMethod("CalculateScaleAndSpacing", BindingFlags.Instance | BindingFlags.NonPublic);
        calcMethod.Invoke(grid, null);

        MethodInfo generateGridMethod = typeof(Grid).GetMethod("GenerateGrid", BindingFlags.Instance | BindingFlags.NonPublic);
        generateGridMethod.Invoke(grid, null);

        // Assert
        FieldInfo cellsField = typeof(Grid).GetField("cells", BindingFlags.Instance | BindingFlags.NonPublic);
        Cell[,] cells = (Cell[,])cellsField.GetValue(grid);

        Assert.AreEqual(10, cells.GetLength(0), "Grid width should be 10");
        Assert.AreEqual(5, cells.GetLength(1), "Grid height should be 5");
    }

    [Test]
    public void CellWithZeroNeighbors_IsSetToBlank()
    {
        // Arrange
        grid.width = 3;
        grid.height = 3;
        grid.mineCount = 0; // No mines

        MethodInfo calcMethod = typeof(Grid).GetMethod("CalculateScaleAndSpacing", BindingFlags.Instance | BindingFlags.NonPublic);
        calcMethod.Invoke(grid, null);

        MethodInfo generateGridMethod = typeof(Grid).GetMethod("GenerateGrid", BindingFlags.Instance | BindingFlags.NonPublic);
        generateGridMethod.Invoke(grid, null);

        FieldInfo cellsField = typeof(Grid).GetField("cells", BindingFlags.Instance | BindingFlags.NonPublic);
        Cell[,] cells = (Cell[,])cellsField.GetValue(grid);

        // Manually trigger neighbor calculation (should be 0 since no mines)
        MethodInfo calculateNeighborsMethod = typeof(Grid).GetMethod("CalculateNeighbors", BindingFlags.Instance | BindingFlags.NonPublic);
        calculateNeighborsMethod.Invoke(grid, null);

        Cell centerCell = cells[1, 1];
        
        // Act
        centerCell.isRevealed = true;
        centerCell.UpdateVisuals();

        // Assert
        Assert.AreEqual(0, centerCell.adjacentMines, "Center cell should have 0 adjacent mines");
        
        SpriteRenderer sr = centerCell.GetComponent<SpriteRenderer>();
        Assert.AreEqual(grid.emptyRevealedSprite, sr.sprite, "Cell with zero neighbors should use the emptyRevealedSprite (blank)");
    }

    [Test]
    public void FlagCount_MatchesBombCount_AndCellStatesAreCorrect()
    {
        // Arrange
        grid.width = 5;
        grid.height = 5;
        grid.mineCount = 5;

        // Manually initialize flags as it normally happens in Start()
        grid.flags = grid.mineCount;

        MethodInfo calcMethod = typeof(Grid).GetMethod("CalculateScaleAndSpacing", BindingFlags.Instance | BindingFlags.NonPublic);
        calcMethod.Invoke(grid, null);

        MethodInfo generateGridMethod = typeof(Grid).GetMethod("GenerateGrid", BindingFlags.Instance | BindingFlags.NonPublic);
        generateGridMethod.Invoke(grid, null);

        FieldInfo cellsField = typeof(Grid).GetField("cells", BindingFlags.Instance | BindingFlags.NonPublic);
        Cell[,] cells = (Cell[,])cellsField.GetValue(grid);

        // Assert initial state
        Assert.AreEqual(5, grid.flags, "Initial flag count should match mine count");

        // Act & Assert: Flag a cell
        grid.ToggleFlag(0, 0);
        Assert.AreEqual(4, grid.flags, "Flag count should decrease after flagging a cell");
        Assert.IsTrue(cells[0, 0].isFlagged, "Cell (0,0) should be flagged");

        // Act & Assert: Unflag the same cell
        grid.ToggleFlag(0, 0);
        Assert.AreEqual(5, grid.flags, "Flag count should increase after unflagging a cell");
        Assert.IsFalse(cells[0, 0].isFlagged, "Cell (0,0) should be unflagged");

        // Act: Flag all allowed cells (5 cells)
        for (int i = 0; i < 5; i++)
        {
            grid.ToggleFlag(i, 0);
        }

        // Assert
        Assert.AreEqual(0, grid.flags, "Flag count should be 0 after flagging 5 cells");
        for (int i = 0; i < 5; i++)
        {
            Assert.IsTrue(cells[i, 0].isFlagged, $"Cell ({i},0) should be flagged");
        }

        // Act: Try to flag one more cell
        grid.ToggleFlag(0, 1);

        // Assert
        Assert.AreEqual(0, grid.flags, "Flag count should remain 0 after trying to flag more than mineCount");
        Assert.IsFalse(cells[0, 1].isFlagged, "Cell (0,1) should not be flagged when no flags are left");
    }
}
