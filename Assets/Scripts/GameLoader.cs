using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Buscaminas.Gameplay
{
    /// <summary>
    /// Handles level transitions. Subscribes to <see cref="GridManager.OnWin"/>
    /// and loads the next scene after a transition animation.
    /// </summary>
    public class GameLoader : MonoBehaviour
    {
        public Animator transition;
        public float transitionTime = 1f;
        public GridManager gridManager;

        void OnEnable()
        {
            if (gridManager != null)
            {
                gridManager.OnWin += HandleWin;
            }
        }

        void OnDisable()
        {
            if (gridManager != null)
            {
                gridManager.OnWin -= HandleWin;
            }
        }

        void HandleWin()
        {
            LoadNextLevel();
        }

        /// <summary>Loads the next scene in the build order.</summary>
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
    }
}
