using System;
using System.Diagnostics;
using System.Threading.Tasks;
using UnityEngine;

namespace Coursework2D
{
    public class ConcurrencyBenchmark : MonoBehaviour
    {
        [Tooltip("Кількість об'єктів")]
        public int ObjectCount = 20000;

        [Tooltip("Кількість ітерацій розрахунку на кожен об'єкт")]
        public int IterationsPerObject = 64;

        public string LastReport { get; private set; }
        public event Action<string> ReportReady;

        private double lastSpeedup;

        public async void RunBenchmark()
        {
            FileLogger.CaptureMainThreadPath();

            try
            {
                int count = Mathf.Max(10000, ObjectCount);
                int iters = Mathf.Max(16, IterationsPerObject);
                var data = new Vector2[count];
                var rnd = new System.Random(12345);
                for (int i = 0; i < count; i++)
                    data[i] = new Vector2((float)(rnd.NextDouble() * 100.0 + 0.1), (float)(rnd.NextDouble() * 100.0 + 0.1));

                _ = ComputeHeavySequential(data, 4);

                var sw = Stopwatch.StartNew();
                double seq = ComputeHeavySequential(data, iters);
                sw.Stop();
                double t1 = Math.Max(0.001, sw.Elapsed.TotalMilliseconds);

                sw.Restart();
                double par = await Task.Run(() => ComputeHeavyParallel(data, iters));
                sw.Stop();
                double tn = Math.Max(0.001, sw.Elapsed.TotalMilliseconds);

                lastSpeedup = t1 / tn;
                double relDiff = Math.Abs(seq - par) / Math.Max(1e-6, Math.Abs(seq));
                LastReport = $"Об'єктів: {count}; ітерацій: {iters}; послідовно: {t1:F2} мс; паралельно: {tn:F2} мс; S={lastSpeedup:F2}x; похибка={relDiff:E2}";

                FileLogger.Write(LastReport);
                ReportReady?.Invoke(LastReport);
            }
            catch (Exception ex)
            {
                FileLogger.Write("Помилка тесту продуктивності.", ex);
                ReportReady?.Invoke("Помилка тесту продуктивності: " + ex.Message);
            }
        }

        private static double EvaluatePoint(float x, float y, int iters)
        {
            double acc = 0.0;
            double px = x;
            double py = y;
            for (int k = 1; k <= iters; k++)
            {
                double invDist = 1.0 / (Math.Sqrt(px * px + py * py) + 0.001);
                double angle = Math.Sin(px * 0.03 * k) * Math.Cos(py * 0.03 * k);
                acc += invDist * (1.0 + 0.5 * angle);
                px = px * 0.999 + Math.Cos(angle) * 0.01;
                py = py * 0.999 + Math.Sin(angle) * 0.01;
            }
            return acc;
        }

        private static double ComputeHeavySequential(Vector2[] pts, int iters)
        {
            double sum = 0.0;
            for (int i = 0; i < pts.Length; i++)
            {
                sum += EvaluatePoint(pts[i].x, pts[i].y, iters);
            }
            return sum;
        }

        private static double ComputeHeavyParallel(Vector2[] pts, int iters)
        {
            object finalLock = new object();
            double total = 0.0;
            Parallel.For(0, pts.Length,
                () => 0.0,
                (i, state, local) => local + EvaluatePoint(pts[i].x, pts[i].y, iters),
                local => { lock (finalLock) total += local; });
            return total;
        }
    }
}