using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Buscaminas.Gameplay
{
    public class GameLoader : MonoBehaviour
    {
        public Animator transition;
        public float transitionTime = 1f;
        public GridManager gridManager;

        [Header("Navigation")]
        [SerializeField] private Button menuButton;
        [SerializeField] private Button exitButton;

        void OnEnable()
        {
            if (gridManager != null)
                gridManager.OnWin += HandleWin;

            if (menuButton != null)
                menuButton.onClick.AddListener(GoToMenu);

            if (exitButton != null)
                exitButton.onClick.AddListener(ExitGame);
        }

        void OnDisable()
        {
            if (gridManager != null)
                gridManager.OnWin -= HandleWin;

            if (menuButton != null)
                menuButton.onClick.RemoveListener(GoToMenu);

            if (exitButton != null)
                exitButton.onClick.RemoveListener(ExitGame);
        }

        void HandleWin()
        {
            LoadNextLevel();
        }

        public void LoadNextLevel()
        {
            StartCoroutine(LoadLevel(SceneManager.GetActiveScene().buildIndex + 1));
        }

        IEnumerator LoadLevel(int levelIndex)
        {
            yield return new WaitForSeconds(transitionTime);
            transition.SetTrigger("Start");
            SceneManager.LoadScene(levelIndex);
        }

        public void GoToMenu()
        {
            SceneManager.LoadScene("Menu");
        }

        public void ExitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
