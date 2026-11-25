using UnityEngine;

namespace Coursework2D
{
    // Можно вызывать из триггера финиша/системы смерти: BeginRun и CompleteRun.
    public class GameplayStatisticsTracker : MonoBehaviour
    {
        private float startedAt;
        private int attempts = 1;
        private int defeatedEnemies;
        private string currentLevel = "Уровень 01";
        private LevelStatisticsDocument document = new LevelStatisticsDocument();

        public void BeginRun(string levelName, int attemptNumber = 1)
        {
            currentLevel = string.IsNullOrWhiteSpace(levelName) ? "Без названия" : levelName;
            attempts = Mathf.Max(1, attemptNumber); defeatedEnemies = 0; startedAt = Time.time;
        }
        public void RegisterEnemyDefeated() => defeatedEnemies++;
        public void CompleteRun()
        {
            document.Runs.Add(new LevelRunStatistic { LevelName = currentLevel, TimeSeconds = Mathf.Max(0, Time.time - startedAt),
                Attempts = attempts, DefeatedEnemies = defeatedEnemies, CompletedAt = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") });
            try { LevelStatisticsService.Export(document); } catch (System.Exception ex) { FileLogger.Write("Не удалось сохранить статистику прохождения.", ex); }
        }
        public void ExportCurrentStatistics() => LevelStatisticsService.Export(document);
    }
}
