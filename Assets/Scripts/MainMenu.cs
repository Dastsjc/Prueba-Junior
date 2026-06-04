using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Buscaminas.Gameplay
{
    /// <summary>
    /// Controls the main menu with three canvases: buttons, instructions, and options.
    /// Persists settings via <see cref="PlayerPrefs"/>.
    /// </summary>
    public class MainMenu : MonoBehaviour
    {
        [Header("Canvases")]
        [SerializeField] private Canvas buttonsCanvas;
        [SerializeField] private Canvas instructionsCanvas;
        [SerializeField] private Canvas optionsCanvas;

        [Header("Buttons Canvas")]
        [SerializeField] private Button playButton;
        [SerializeField] private Button instructionsButton;
        [SerializeField] private Button optionsButton;
        [SerializeField] private Button exitButton;

        [Header("Instructions Canvas")]
        [SerializeField] private Button instructionsBackButton;

        [Header("Options Canvas")]
        [SerializeField] private Button optionsBackButton;
        [SerializeField] private Slider volumeSlider;
        [SerializeField] private Toggle timerToggle;
        [SerializeField] private Toggle firstClickSafetyToggle;

        private const string KeyVolume = "Volume";
        private const string KeyTimer = "ShowTimer";
        private const string KeyFirstClickSafety = "FirstClickSafety";

        void Awake()
        {
            LoadSettings();
            ShowButtons();
        }

        void OnEnable()
        {
            playButton.onClick.AddListener(OnPlayClicked);
            instructionsButton.onClick.AddListener(OnInstructionsClicked);
            optionsButton.onClick.AddListener(OnOptionsClicked);
            exitButton.onClick.AddListener(OnExitClicked);
            instructionsBackButton.onClick.AddListener(ShowButtons);
            optionsBackButton.onClick.AddListener(OnOptionsBackClicked);
            volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        }

        void OnDisable()
        {
            playButton.onClick.RemoveListener(OnPlayClicked);
            instructionsButton.onClick.RemoveListener(OnInstructionsClicked);
            optionsButton.onClick.RemoveListener(OnOptionsClicked);
            exitButton.onClick.RemoveListener(OnExitClicked);
            instructionsBackButton.onClick.RemoveListener(ShowButtons);
            optionsBackButton.onClick.RemoveListener(OnOptionsBackClicked);
            volumeSlider.onValueChanged.RemoveListener(OnVolumeChanged);
        }

        void ShowButtons()
        {
            buttonsCanvas.gameObject.SetActive(true);
            instructionsCanvas.gameObject.SetActive(false);
            optionsCanvas.gameObject.SetActive(false);
        }

        void OnPlayClicked()
        {
            if (MusicManager.Instance != null)
                MusicManager.Instance.PlayGameMusic();
            SceneManager.LoadScene("Nivel 1");
        }

        void OnInstructionsClicked()
        {
            buttonsCanvas.gameObject.SetActive(false);
            instructionsCanvas.gameObject.SetActive(true);
        }

        void OnOptionsClicked()
        {
            buttonsCanvas.gameObject.SetActive(false);
            optionsCanvas.gameObject.SetActive(true);
        }

        void OnExitClicked()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        void OnOptionsBackClicked()
        {
            SaveSettings();
            ShowButtons();
        }

        void OnVolumeChanged(float value)
        {
            AudioListener.volume = value;
        }

        void SaveSettings()
        {
            PlayerPrefs.SetFloat(KeyVolume, volumeSlider.value);
            PlayerPrefs.SetInt(KeyTimer, timerToggle.isOn ? 1 : 0);
            PlayerPrefs.SetInt(KeyFirstClickSafety, firstClickSafetyToggle.isOn ? 1 : 0);
            PlayerPrefs.Save();
        }

        void LoadSettings()
        {
            float volume = PlayerPrefs.GetFloat(KeyVolume, 1f);
            bool showTimer = PlayerPrefs.GetInt(KeyTimer, 1) == 1;
            bool firstClickSafety = PlayerPrefs.GetInt(KeyFirstClickSafety, 1) == 1;

            volumeSlider.value = volume;
            AudioListener.volume = volume;
            timerToggle.isOn = showTimer;
            firstClickSafetyToggle.isOn = firstClickSafety;
        }
    }
}
