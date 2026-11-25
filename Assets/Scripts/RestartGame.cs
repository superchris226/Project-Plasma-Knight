using UnityEngine;
using UnityEngine.SceneManagement;

public class RestartGame : MonoBehaviour
{
    public void LoadCurrentScene()
    {
        SceneManager.LoadScene("TestZoneLocation");
        Time.timeScale = 1f;
    }

}
