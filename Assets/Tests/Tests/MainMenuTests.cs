using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using System.Reflection;
using Buscaminas.Gameplay;

public class MainMenuTests
{
    private GameObject _menuObject;
    private MainMenu _menu;

    private Canvas _buttonsCanvas;
    private Canvas _instructionsCanvas;
    private Canvas _optionsCanvas;

    private Button _playButton;
    private Button _instructionsButton;
    private Button _optionsButton;
    private Button _exitButton;
    private Button _instructionsBackButton;
    private Button _optionsBackButton;

    private Slider _volumeSlider;
    private Toggle _timerToggle;
    private Toggle _firstClickSafetyToggle;

    [SetUp]
    public void SetUp()
    {
        PlayerPrefs.DeleteKey("Volume");
        PlayerPrefs.DeleteKey("ShowTimer");
        PlayerPrefs.DeleteKey("FirstClickSafety");

        _menuObject = new GameObject("MainMenu");
        _menuObject.SetActive(false);

        _buttonsCanvas = CreateCanvas("ButtonsCanvas");
        _instructionsCanvas = CreateCanvas("InstructionsCanvas");
        _optionsCanvas = CreateCanvas("OptionsCanvas");

        _playButton = CreateButton("PlayButton");
        _instructionsButton = CreateButton("InstructionsButton");
        _optionsButton = CreateButton("OptionsButton");
        _exitButton = CreateButton("ExitButton");
        _instructionsBackButton = CreateButton("InstructionsBackButton");
        _optionsBackButton = CreateButton("OptionsBackButton");

        _volumeSlider = CreateSlider("VolumeSlider");
        _timerToggle = CreateToggle("TimerToggle");
        _firstClickSafetyToggle = CreateToggle("FirstClickSafetyToggle");

        _menu = _menuObject.AddComponent<MainMenu>();

        SetField("buttonsCanvas", _buttonsCanvas);
        SetField("instructionsCanvas", _instructionsCanvas);
        SetField("optionsCanvas", _optionsCanvas);
        SetField("playButton", _playButton);
        SetField("instructionsButton", _instructionsButton);
        SetField("optionsButton", _optionsButton);
        SetField("exitButton", _exitButton);
        SetField("instructionsBackButton", _instructionsBackButton);
        SetField("optionsBackButton", _optionsBackButton);
        SetField("volumeSlider", _volumeSlider);
        SetField("timerToggle", _timerToggle);
        SetField("firstClickSafetyToggle", _firstClickSafetyToggle);

        CallPrivate("Awake");
        CallPrivate("OnEnable");
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_menuObject);
        Object.DestroyImmediate(_buttonsCanvas.gameObject);
        Object.DestroyImmediate(_instructionsCanvas.gameObject);
        Object.DestroyImmediate(_optionsCanvas.gameObject);
        Object.DestroyImmediate(_playButton.gameObject);
        Object.DestroyImmediate(_instructionsButton.gameObject);
        Object.DestroyImmediate(_optionsButton.gameObject);
        Object.DestroyImmediate(_exitButton.gameObject);
        Object.DestroyImmediate(_instructionsBackButton.gameObject);
        Object.DestroyImmediate(_optionsBackButton.gameObject);
        Object.DestroyImmediate(_volumeSlider.gameObject);
        Object.DestroyImmediate(_timerToggle.gameObject);
        Object.DestroyImmediate(_firstClickSafetyToggle.gameObject);

        PlayerPrefs.DeleteKey("Volume");
        PlayerPrefs.DeleteKey("ShowTimer");
        PlayerPrefs.DeleteKey("FirstClickSafety");
    }

    [Test]
    public void Awake_ShowsButtonsCanvasAndHidesOthers()
    {
        Assert.IsTrue(_buttonsCanvas.gameObject.activeSelf, "Buttons canvas should be active");
        Assert.IsFalse(_instructionsCanvas.gameObject.activeSelf, "Instructions canvas should be inactive");
        Assert.IsFalse(_optionsCanvas.gameObject.activeSelf, "Options canvas should be inactive");
    }

    [Test]
    public void OnInstructionsClicked_HidesButtonsAndShowsInstructions()
    {
        _instructionsButton.onClick.Invoke();

        Assert.IsFalse(_buttonsCanvas.gameObject.activeSelf, "Buttons canvas should be hidden");
        Assert.IsTrue(_instructionsCanvas.gameObject.activeSelf, "Instructions canvas should be visible");
        Assert.IsFalse(_optionsCanvas.gameObject.activeSelf, "Options canvas should be hidden");
    }

    [Test]
    public void OnOptionsClicked_HidesButtonsAndShowsOptions()
    {
        _optionsButton.onClick.Invoke();

        Assert.IsFalse(_buttonsCanvas.gameObject.activeSelf, "Buttons canvas should be hidden");
        Assert.IsFalse(_instructionsCanvas.gameObject.activeSelf, "Instructions canvas should be hidden");
        Assert.IsTrue(_optionsCanvas.gameObject.activeSelf, "Options canvas should be visible");
    }

    [Test]
    public void InstructionsBackButton_ShowsButtonsCanvas()
    {
        _instructionsButton.onClick.Invoke();
        Assert.IsTrue(_instructionsCanvas.gameObject.activeSelf);

        _instructionsBackButton.onClick.Invoke();

        Assert.IsTrue(_buttonsCanvas.gameObject.activeSelf, "Buttons canvas should be visible again");
        Assert.IsFalse(_instructionsCanvas.gameObject.activeSelf, "Instructions canvas should be hidden");
        Assert.IsFalse(_optionsCanvas.gameObject.activeSelf, "Options canvas should be hidden");
    }

    [Test]
    public void OptionsBackButton_ShowsButtonsCanvasAndSavesSettings()
    {
        _optionsButton.onClick.Invoke();

        _volumeSlider.value = 0.5f;
        _timerToggle.isOn = false;
        _firstClickSafetyToggle.isOn = false;

        _optionsBackButton.onClick.Invoke();

        Assert.IsTrue(_buttonsCanvas.gameObject.activeSelf, "Buttons canvas should be visible again");
        Assert.IsFalse(_optionsCanvas.gameObject.activeSelf, "Options canvas should be hidden");

        Assert.AreEqual(0.5f, PlayerPrefs.GetFloat("Volume"), 0.01f, "Volume should be saved");
        Assert.AreEqual(0, PlayerPrefs.GetInt("ShowTimer"), "Timer setting should be saved");
        Assert.AreEqual(0, PlayerPrefs.GetInt("FirstClickSafety"), "First click safety should be saved");
    }

    [Test]
    public void VolumeSlider_ChangesAudioListenerVolume()
    {
        _volumeSlider.value = 0.3f;

        Assert.AreEqual(0.3f, AudioListener.volume, 0.01f, "AudioListener volume should match slider");
    }

    [Test]
    public void LoadSettings_RestoresSavedValues()
    {
        PlayerPrefs.SetFloat("Volume", 0.7f);
        PlayerPrefs.SetInt("ShowTimer", 0);
        PlayerPrefs.SetInt("FirstClickSafety", 0);
        PlayerPrefs.Save();

        Object.DestroyImmediate(_menuObject);
        Object.DestroyImmediate(_volumeSlider.gameObject);
        Object.DestroyImmediate(_timerToggle.gameObject);
        Object.DestroyImmediate(_firstClickSafetyToggle.gameObject);

        _menuObject = new GameObject("MainMenu");
        _menuObject.SetActive(false);

        _volumeSlider = CreateSlider("VolumeSlider2");
        _timerToggle = CreateToggle("TimerToggle2");
        _firstClickSafetyToggle = CreateToggle("FirstClickSafetyToggle2");

        _menu = _menuObject.AddComponent<MainMenu>();
        SetField("buttonsCanvas", _buttonsCanvas);
        SetField("instructionsCanvas", _instructionsCanvas);
        SetField("optionsCanvas", _optionsCanvas);
        SetField("playButton", _playButton);
        SetField("instructionsButton", _instructionsButton);
        SetField("optionsButton", _optionsButton);
        SetField("exitButton", _exitButton);
        SetField("instructionsBackButton", _instructionsBackButton);
        SetField("optionsBackButton", _optionsBackButton);
        SetField("volumeSlider", _volumeSlider);
        SetField("timerToggle", _timerToggle);
        SetField("firstClickSafetyToggle", _firstClickSafetyToggle);

        CallPrivate("Awake");
        CallPrivate("OnEnable");

        Assert.AreEqual(0.7f, _volumeSlider.value, 0.01f, "Volume slider should be restored");
        Assert.IsFalse(_timerToggle.isOn, "Timer toggle should be restored");
        Assert.IsFalse(_firstClickSafetyToggle.isOn, "First click safety toggle should be restored");
    }

    [Test]
    public void LoadSettings_UsesDefaultsWhenNoSavedData()
    {
        Assert.AreEqual(1f, _volumeSlider.value, 0.01f, "Volume should default to 1");
        Assert.IsTrue(_timerToggle.isOn, "Timer should default to on");
        Assert.IsTrue(_firstClickSafetyToggle.isOn, "First click safety should default to on");
    }

    private Canvas CreateCanvas(string name)
    {
        var go = new GameObject(name);
        return go.AddComponent<Canvas>();
    }

    private Button CreateButton(string name)
    {
        var go = new GameObject(name);
        go.AddComponent<CanvasRenderer>();
        return go.AddComponent<Button>();
    }

    private Slider CreateSlider(string name)
    {
        var go = new GameObject(name);
        go.AddComponent<CanvasRenderer>();
        go.AddComponent<RectTransform>();
        var slider = go.AddComponent<Slider>();
        slider.fillRect = null;
        slider.handleRect = null;
        return slider;
    }

    private Toggle CreateToggle(string name)
    {
        var go = new GameObject(name);
        go.AddComponent<CanvasRenderer>();
        go.AddComponent<RectTransform>();
        var toggle = go.AddComponent<Toggle>();
        toggle.graphic = null;
        return toggle;
    }

    private void SetField(string fieldName, object value)
    {
        var field = typeof(MainMenu).GetField(fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        field.SetValue(_menu, value);
    }

    private void CallPrivate(string methodName)
    {
        var method = typeof(MainMenu).GetMethod(methodName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        method.Invoke(_menu, null);
    }
}
