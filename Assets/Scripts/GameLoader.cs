using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

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
