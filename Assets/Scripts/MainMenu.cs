using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class GameManager : MonoBehaviour
{
    private bool isGameStarted = false;


    void Update()
    {
        if (Keyboard.current == null) return;

        if (!isGameStarted)
        {
            if (Keyboard.current.enterKey.wasPressedThisFrame)
            {
                StartGame();
            }
        }
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            QuitGame();
        }
    }

    void StartGame()
    {
        isGameStarted = true;
        GameObject menu = GameObject.Find("Player"); 
        if (menu != null)
        {
            menu.SetActive(false);
        }
        Time.timeScale = 1f;
        SceneManager.LoadScene("TestZoneLocation");
    }

    void QuitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}