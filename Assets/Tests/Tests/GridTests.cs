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
}
