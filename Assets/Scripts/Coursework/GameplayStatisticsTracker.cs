using UnityEngine;
using Coursework2D;

public class GameplayStatisticsTracker : MonoBehaviour
{
    private float startedAt;
    private int attempts = 1;
    private int defeatedEnemies;
    private string currentLevel = "Рівень 01";
    private LevelStatisticsDocument document = new LevelStatisticsDocument();

    public LevelStatisticsDocument Document => document;

    private void Awake()
    {
        FileLogger.CaptureMainThreadPath();
    }

    public void BeginRun(string levelName, int attemptNumber = 1)
    {
        FileLogger.CaptureMainThreadPath();
        currentLevel = string.IsNullOrWhiteSpace(levelName) ? "Без назви" : levelName;
        attempts = Mathf.Max(1, attemptNumber);
        defeatedEnemies = 0;
        startedAt = Time.time;
        FileLogger.Write($"[Stats] Початок проходження: {currentLevel}");
    }

    public void RegisterEnemyDefeated() => defeatedEnemies++;

    public void CompleteRun()
    {
        FileLogger.CaptureMainThreadPath();
        document.Runs.Add(new LevelRunStatistic
        {
            LevelName = currentLevel,
            TimeSeconds = Mathf.Max(0, Time.time - startedAt),
            Attempts = attempts,
            DefeatedEnemies = defeatedEnemies,
            CompletedAt = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        });
        try
        {
            LevelStatisticsService.Export(document);
            FileLogger.Write("[Stats] Проходження збережено.");
        }
        catch (System.Exception ex)
        {
            FileLogger.Write("Не вдалося зберегти статистику проходження.", ex);
        }
    }

    public bool ExportCurrentStatistics(string path = null)
    {
        FileLogger.CaptureMainThreadPath();
        try
        {
            LevelStatisticsService.Export(document, path);
            FileLogger.Write("[Stats] Експорт XML виконано.");
            return true;
        }
        catch (System.Exception ex)
        {
            FileLogger.Write("Експорт XML не вдався.", ex);
            return false;
        }
    }

    public bool ImportStatistics(string path = null)
    {
        FileLogger.CaptureMainThreadPath();
        try
        {
            var imported = LevelStatisticsService.Import(path);
            if (imported == null)
            {
                FileLogger.Write("Імпорт XML не вдався: порожній документ.");
                return false;
            }
            document = imported;
            FileLogger.Write("[Stats] Імпорт XML виконано.");
            return true;
        }
        catch (System.Exception ex)
        {
            FileLogger.Write("Імпорт XML не вдався.", ex);
            return false;
        }
    }
}