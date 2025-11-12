using UnityEngine;
using UnityEngine.SceneManagement;

public class ExitDoor : MonoBehaviour
{
    public static bool betaMessageDisplayed = false;

    public GameObject betaMessage;

    public string nextLevelName;

    private bool isPlayerNear = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNear = true;
            Debug.Log("Press R to enter next level");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNear = false;
        }
    }

    private void Update()
    {
        if (isPlayerNear && Input.GetKeyDown(KeyCode.R))
        {
            LoadNextLevel();
        }
    }

    private void LoadNextLevel()
    {
        if (!string.IsNullOrEmpty(nextLevelName))
        {
            SceneManager.LoadScene(nextLevelName);
        }
        else if (string.IsNullOrEmpty(nextLevelName))
        {
            betaMessage.SetActive(true);
            Time.timeScale = 0f;
        }
        else
        {
            Debug.LogWarning("Next level name is not set in the ExitDoor script");
        }
    }
}