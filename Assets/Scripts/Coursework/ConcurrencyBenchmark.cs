using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using UnityEngine;

namespace Coursework2D
{
    public class ConcurrencyBenchmark : MonoBehaviour
    {
        public int ObjectCount = 12000;
        public string LastReport { get; private set; }

        public async void RunBenchmark()
        {
            try
            {
                int count = Mathf.Max(10000, ObjectCount);
                var data = new List<Vector2Int>(count);
                var random = new System.Random(12345);
                for (int i = 0; i < count; i++) data.Add(new Vector2Int(random.Next(0, 1000), random.Next(0, 1000)));
                var sw = Stopwatch.StartNew();
                long sum1 = 0;
                for (int i = 0; i < data.Count; i++) sum1 += data[i].x + data[i].y;
                sw.Stop(); double t1 = Math.Max(0.001, sw.Elapsed.TotalMilliseconds);
                sw.Restart();
                long sumN = await Task.Run(() =>
                {
                    object sync = new object(); long total = 0;
                    Parallel.ForEach(data, point => { long local = point.x + point.y; lock (sync) total += local; });
                    return total;
                });
                sw.Stop(); double tn = Math.Max(0.001, sw.Elapsed.TotalMilliseconds);
                double speedup = t1 / tn;
                LastReport = $"Об'єктів: {count}; послідовно: {t1:F2} мс; паралельно: {tn:F2} мс; S=T1/Tn={speedup:F2}; контрольні суми збігаються: {sum1 == sumN}";
                FileLogger.Write(LastReport);
                UnityEngine.Debug.Log(LastReport); // було: Debug.Log — неоднозначність між UnityEngine.Debug і System.Diagnostics.Debug
            }
            catch (Exception ex) { FileLogger.Write("Помилка тесту продуктивності.", ex); }
        }
    }
}