using System;
using System.Diagnostics;
using System.Threading.Tasks;
using UnityEngine;

namespace Coursework2D
{
    public class ConcurrencyBenchmark : MonoBehaviour
    {
        public int ObjectCount = 12000;
        public string LastReport { get; private set; }
        
        // ДОДАНО: подія для виводу результату в UI
        public event Action<string> ReportReady;

        private double lastSpeedup;

        public async void RunBenchmark()
        {
            try
            {
                int count = Mathf.Max(10000, ObjectCount);
                var data = new Vector2[count];
                var rnd = new System.Random(12345);
                for (int i = 0; i < count; i++) 
                    data[i] = new Vector2((float)rnd.NextDouble() * 100f, (float)rnd.NextDouble() * 100f);

                // Послідовно
                var sw = Stopwatch.StartNew();
                float seq = ComputeAttractionSequential(data);
                sw.Stop();
                double t1 = Math.Max(0.001, sw.Elapsed.TotalMilliseconds);

                // Паралельно: без lock на кожному елементі (локальні акумулятори)
                sw.Restart();
                float par = await Task.Run(() => ComputeAttractionParallel(data));
                sw.Stop();
                double tn = Math.Max(0.001, sw.Elapsed.TotalMilliseconds);

                lastSpeedup = t1 / tn;
                LastReport = $"Об'єктів: {count}; послідовно: {t1:F2} мс; паралельно: {tn:F2} мс; S={lastSpeedup:F2}; суми збігаються: {Math.Abs(seq - par) < 1e-2f}";
                
                FileLogger.Write(LastReport);
                ReportReady?.Invoke(LastReport); // Викликаємо подію
            }
            catch (Exception ex) 
            { 
                FileLogger.Write("Помилка тесту продуктивності.", ex); 
            }
        }

        private static float ComputeAttractionSequential(Vector2[] pts)
        {
            float sum = 0;
            for (int i = 0; i < pts.Length; i++)
            {
                float dx = pts[i].x, dy = pts[i].y;
                float dist2 = dx * dx + dy * dy;
                if (dist2 > 0.0001f) sum += 1f / dist2;
            }
            return sum;
        }

        private static float ComputeAttractionParallel(Vector2[] pts)
        {
            object finalLock = new object();
            float total = 0f;
            Parallel.For(0, pts.Length, () => 0f,
                (i, state, local) =>
                {
                    float dx = pts[i].x, dy = pts[i].y;
                    float dist2 = dx * dx + dy * dy;
                    if (dist2 > 0.0001f) local += 1f / dist2;
                    return local;
                },
                local => { lock (finalLock) total += local; });
            return total;
        }
    }
}