using UnityEngine;
using UnityEngine.SceneManagement;

public class FinishLevel : MonoBehaviour
{
    [Header("UI")]
    public GameObject winUI;

    [Header("Налаштування переходу")]
    [Tooltip("Назва сцени, на яку потрібно перейти")]
    public string nextSceneName = "MainMenu";

    [Tooltip("Затримка в секундах перед переходом")]
    public float delay = 3f;

    private bool isFinished = false;
    private float timer;

    public GameplayStatisticsTracker statistics;

    private void Awake()
    {
        if (statistics == null)
            statistics = FindFirstObjectByType<GameplayStatisticsTracker>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isFinished) return;
        if (collision.gameObject.CompareTag("Player"))
        {
            isFinished = true;
            timer = delay;
            if (winUI != null)
                winUI.SetActive(true);
            if (statistics == null)
                statistics = FindFirstObjectByType<GameplayStatisticsTracker>();
            statistics?.CompleteRun();
        }
    }

    private void Update()
    {
        if (!isFinished) return;
        timer -= Time.unscaledDeltaTime;
        if (timer <= 0f)
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(nextSceneName);
        }
    }
}