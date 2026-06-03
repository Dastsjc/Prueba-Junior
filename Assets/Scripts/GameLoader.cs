using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameLoader : MonoBehaviour
{
    public Animator transition;
    public float transitionTime = 1f;
    public Grid gridManager;

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

    public void LoadNextLevel()
    {
        StartCoroutine(LoadLevel(SceneManager.GetActiveScene().buildIndex + 1));
    }

    IEnumerator LoadLevel(int levelIndex)
    {
        transition.SetTrigger("Start");
        yield return new WaitForSeconds(transitionTime);
        SceneManager.LoadScene(levelIndex);
    }
}
