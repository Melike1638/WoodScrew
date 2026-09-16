using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelFlow : MonoBehaviour
{
    public GameObject LevelCompleteText;
    public int RequiredPlanks = 1;

    private int completedPlanks = 0;
    private bool levelCompleted = false;

    void Start()
    {
        if (LevelCompleteText != null)
            LevelCompleteText.SetActive(false);
    }

    public void PlankCompleted()
    {
        if (levelCompleted)
            return;

        completedPlanks++;

        if (completedPlanks >= RequiredPlanks)
        {
            levelCompleted = true;

            if (LevelCompleteText != null)
                LevelCompleteText.SetActive(true);

            Invoke(nameof(LoadNextScene), 1.5f);
        }
    }

    void LoadNextScene()
    {
        int nextScene =
            SceneManager.GetActiveScene().buildIndex + 1;

        if (nextScene < SceneManager.sceneCountInBuildSettings)
            SceneManager.LoadScene(nextScene);
    }
}