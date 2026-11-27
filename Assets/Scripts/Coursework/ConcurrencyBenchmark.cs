// ШІ: ChatGPT – Створив ConcurrencyBenchmark.
// Змінено: фікс кода.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using UnityEngine;

namespace Coursework2D
{
    public class ConcurrencyBenchmark : MonoBehaviour
    {
        [Tooltip("Кількість згенерованих рівнів (рекомендується 200..500)")]
        public int LevelCount = 300;

        [Tooltip("Мінімальна сумарна кількість об'єктів у згенерованих рівнях (10 000+)")]
        public int MinTotalObjects = 12000;

        public string LastReport { get; private set; }
        public event Action<string> ReportReady;

        private double lastSpeedup;

        public async void RunBenchmark()
        {
            FileLogger.CaptureMainThreadPath();

            try
            {
                int levelsCount = Mathf.Clamp(LevelCount, 200, 500);
                int targetObjects = Mathf.Max(10000, MinTotalObjects);

                List<Level> levels = GenerateLevelsDataset(levelsCount, targetObjects);
                int actualObjects = CountTotalObjects(levels);

                // Прогрів перед замірами
                if (levels.Count > 0)
                    _ = LevelPlayabilityChecker.IsPlayable(levels[0], out _);

                // 1. Однопотоковий прохід (T1) через звичайний foreach
                var sw = Stopwatch.StartNew();
                int seqPlayable = RunSequentialPlayabilityCheck(levels);
                sw.Stop();
                double t1 = Math.Max(0.001, sw.Elapsed.TotalMilliseconds);

                // 2. Багатопотоковий прохід (Tn) через Parallel.ForEach
                sw.Restart();
                int parPlayable = await Task.Run(() => RunParallelPlayabilityCheck(levels));
                sw.Stop();
                double tn = Math.Max(0.001, sw.Elapsed.TotalMilliseconds);

                lastSpeedup = t1 / tn;
                LastReport =
                    $"Рівнів: {levels.Count}; об'єктів: {actualObjects}; " +
                    $"T1 (foreach): {t1:F2} мс; Tn (Parallel.ForEach): {tn:F2} мс; " +
                    $"S = T1/Tn = {lastSpeedup:F2}; прохідних: {seqPlayable}/{parPlayable}";

                FileLogger.Write(LastReport);
                ReportReady?.Invoke(LastReport);
            }
            catch (Exception ex)
            {
                FileLogger.Write("Помилка тесту продуктивності.", ex);
                ReportReady?.Invoke("Помилка тесту продуктивності: " + ex.Message);
            }
        }

        private static List<Level> GenerateLevelsDataset(int levelCount, int minTotalObjects)
        {
            var levels = new List<Level>(levelCount);
            var rnd = new System.Random(12345);

            for (int i = 0; i < levelCount; i++)
            {
                int width = 40;
                int height = 20;
                var start = new GridPoint(1, 1);
                var finish = new GridPoint(width - 2, 1);

                var level = new Level
                {
                    name = $"BenchLevel_{i + 1:000}",
                    difficulty = (i % 5) + 1,
                    gridWidth = width,
                    gridHeight = height,
                    start = start,
                    finish = finish,
                    objects = new List<LevelObject>
                    {
                        new LevelObject(LevelObjectType.Start, new Vector2Int(start.x, start.y)),
                        new LevelObject(LevelObjectType.Finish, new Vector2Int(finish.x, finish.y))
                    }
                };

                levels.Add(level);
            }

            // Розподіляємо об'єкти по згенерованих рівнях так, щоб сумарно було >= minTotalObjects
            int remaining = Mathf.Max(minTotalObjects, levelCount * 20);
            int idx = 0;

            while (remaining > 0)
            {
                var lvl = levels[idx % levels.Count];
                int x = rnd.Next(0, lvl.gridWidth);
                int y = rnd.Next(0, lvl.gridHeight);

                // Не затираємо клітинки старту та фінішу випадковими перешкодами/платформами
                if ((x == lvl.start.x && y == lvl.start.y) || (x == lvl.finish.x && y == lvl.finish.y))
                {
                    idx++;
                    continue;
                }

                LevelObjectType type = (idx % 5) switch
                {
                    0 => LevelObjectType.Platform,
                    1 => LevelObjectType.Coin,
                    2 => LevelObjectType.Decoration,
                    3 => LevelObjectType.Hazard,
                    _ => LevelObjectType.Platform
                };

                lvl.objects.Add(new LevelObject(type, new Vector2Int(x, y)));
                remaining--;
                idx++;
            }

            return levels;
        }

        private static int CountTotalObjects(List<Level> levels)
        {
            int sum = 0;
            for (int i = 0; i < levels.Count; i++)
            {
                if (levels[i] != null)
                    sum += levels[i].objectCount;
            }
            return sum;
        }

        private static int RunSequentialPlayabilityCheck(List<Level> levels)
        {
            int playableCount = 0;
            foreach (var level in levels)
            {
                if (LevelPlayabilityChecker.IsPlayable(level, out _))
                    playableCount++;
            }
            return playableCount;
        }

        private static int RunParallelPlayabilityCheck(List<Level> levels)
        {
            int playableCount = 0;
            object sync = new object();

            Parallel.ForEach(levels, level =>
            {
                bool ok = LevelPlayabilityChecker.IsPlayable(level, out _);
                if (ok)
                {
                    lock (sync)
                    {
                        playableCount++;
                    }
                }
            });

            return playableCount;
        }
    }
}