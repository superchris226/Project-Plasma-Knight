using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using UnityEngine;

namespace Coursework2D
{
    public class ConcurrencyBenchmark : MonoBehaviour
    {
        [Tooltip("Кількість об'єктів рівня для тесту (10 000+)")]
        public int ObjectCount = 12000;

        [Tooltip("Кількість проходів аналізу по набору об'єктів")]
        public int Passes = 50;

        public string LastReport { get; private set; }
        public event Action<string> ReportReady;

        private double lastSpeedup;

        public async void RunBenchmark()
        {
            FileLogger.CaptureMainThreadPath();

            try
            {
                int count = Mathf.Max(10000, ObjectCount);
                int passes = Mathf.Max(1, Passes);

                // Формуємо набір об'єктів рівня (координати в межах сітки рівня, типи LevelObjectType)
                var levelObjects = BuildLevelObjectsDataset(count, gridWidth: 200, gridHeight: 100);

                // Прогрів
                _ = ProcessLevelObjectsSequential(levelObjects, 2);

                // 1. Однопотоковий режим (T1)
                var sw = Stopwatch.StartNew();
                int seqValidCount = ProcessLevelObjectsSequential(levelObjects, passes);
                sw.Stop();
                double t1 = Math.Max(0.001, sw.Elapsed.TotalMilliseconds);

                // 2. Багатопотоковий режим (Tn)
                sw.Restart();
                int parValidCount = await Task.Run(() => ProcessLevelObjectsParallel(levelObjects, passes));
                sw.Stop();
                double tn = Math.Max(0.001, sw.Elapsed.TotalMilliseconds);

                lastSpeedup = t1 / tn;
                LastReport =
                    $"Об'єктів рівня: {count}; проходів: {passes}; " +
                    $"T1 (1 потік): {t1:F2} мс; Tn (багатопотоково): {tn:F2} мс; " +
                    $"S = T1/Tn = {lastSpeedup:F2}; валідних перевірок: {seqValidCount}/{parValidCount}";

                FileLogger.Write(LastReport);
                ReportReady?.Invoke(LastReport);
            }
            catch (Exception ex)
            {
                FileLogger.Write("Помилка тесту продуктивності.", ex);
                ReportReady?.Invoke("Помилка тесту продуктивності: " + ex.Message);
            }
        }

        private static List<LevelObject> BuildLevelObjectsDataset(int count, int gridWidth, int gridHeight)
        {
            var list = new List<LevelObject>(count);
            var rnd = new System.Random(12345);
            var types = (LevelObjectType[])Enum.GetValues(typeof(LevelObjectType));

            for (int i = 0; i < count; i++)
            {
                var type = types[i % types.Length];
                int x = rnd.Next(-5, gridWidth + 5);
                int y = rnd.Next(-5, gridHeight + 5);
                list.Add(new LevelObject(type, new Vector2Int(x, y)));
            }

            return list;
        }

        private static int EvaluateLevelObject(LevelObject obj, int gridWidth, int gridHeight)
        {
            if (obj == null) return 0;

            bool inside = obj.position.x >= 0 && obj.position.x < gridWidth &&
                          obj.position.y >= 0 && obj.position.y < gridHeight;

            // Додаткове навантаження на об'єкт рівня (аналіз сусідніх клітинок / ваги об'єкта)
            double weight = 0;
            for (int k = 1; k <= 32; k++)
            {
                weight += Math.Sqrt((obj.position.x + k) * (obj.position.x + k) +
                                    (obj.position.y + k) * (obj.position.y + k));
            }

            return (inside && weight > 0) ? 1 : 0;
        }

        private static int ProcessLevelObjectsSequential(List<LevelObject> objects, int passes)
        {
            const int w = 200;
            const int h = 100;
            int total = 0;

            for (int p = 0; p < passes; p++)
            {
                for (int i = 0; i < objects.Count; i++)
                {
                    total += EvaluateLevelObject(objects[i], w, h);
                }
            }

            return total;
        }

        private static int ProcessLevelObjectsParallel(List<LevelObject> objects, int passes)
        {
            const int w = 200;
            const int h = 100;
            object sync = new object();
            int total = 0;

            for (int p = 0; p < passes; p++)
            {
                Parallel.For(0, objects.Count,
                    () => 0,
                    (i, state, local) => local + EvaluateLevelObject(objects[i], w, h),
                    local =>
                    {
                        lock (sync)
                        {
                            total += local;
                        }
                    });
            }

            return total;
        }
    }
}