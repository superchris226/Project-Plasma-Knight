using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    public GameObject container;
    private bool isPaused = false;

    void Update()
    {
        if (Keyboard.current == null) return;
        if (Keyboard.current.pKey.wasPressedThisFrame)
        {
            if (isPaused)
                ResumeGame();
            else
                PauseGame();
        }
        if (isPaused)
        {
            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                SceneManager.LoadScene("MainMenu"); 
            }
        }
    }

    public void PauseGame()
    {
        container.SetActive(true);
        Time.timeScale = 0f;
        isPaused = true;
    }

    public void ResumeGame()
    {
        container.SetActive(false);
        Time.timeScale = 1f;
        isPaused = false;
    }
}