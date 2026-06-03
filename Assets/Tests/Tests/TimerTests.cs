using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using TMPro;
using Buscaminas.Gameplay;

public class TimerTests
{
    private GameObject gridGameObject;
    private GridManager grid;

    [SetUp]
    public void SetUp()
    {
        gridGameObject = new GameObject("Grid");
        grid = gridGameObject.AddComponent<GridManager>();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(gridGameObject);
    }

    [Test]
    public void Timer_At10Seconds_ShowsExactly10()
    {
        // Arrange
        GameObject textGo = new GameObject("TimerText");
        textGo.AddComponent<CanvasRenderer>();
        TextMeshProUGUI timerText = textGo.AddComponent<TextMeshProUGUI>();
        grid.timerText = timerText;

        // Set elapsedTime to 10 via reflection
        FieldInfo elapsedTimeField = typeof(GridManager).GetField("elapsedTime", BindingFlags.Instance | BindingFlags.NonPublic);
        elapsedTimeField.SetValue(grid, 10f);

        // Act
        // Invoke UpdateTimerUI via reflection
        MethodInfo updateTimerUIMethod = typeof(GridManager).GetMethod("UpdateTimerUI", BindingFlags.Instance | BindingFlags.NonPublic);
        updateTimerUIMethod.Invoke(grid, null);

        // Assert
        Assert.AreEqual("10", timerText.text);

        // Cleanup
        Object.DestroyImmediate(textGo);
    }
}
