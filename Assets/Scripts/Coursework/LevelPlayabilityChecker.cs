using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using UnityEngine;

namespace Coursework2D
{
    public sealed class LevelCheckResult
    {
        public string levelName;
        public bool playable;
        public string message;
        public long elapsedMilliseconds;
    }

    public class LevelPlayabilityChecker : MonoBehaviour
    {
        public int Completed { get; private set; }
        public int Total { get; private set; }
        public float Progress => Total == 0 ? 0f : (float)Completed / Total;
        public bool IsRunning { get; private set; }
        public event Action<LevelCheckResult> ResultReceived;
        public event Action<float> ProgressChanged;

        private readonly ConcurrentQueue<LevelCheckResult> results = new ConcurrentQueue<LevelCheckResult>();
        private readonly object stateLock = new object();
        private int completedWorker;

        public void CheckMany(IList<Level> levels)
        {
            if (IsRunning)
            {
                FileLogger.Write("Проверка уже выполняется.");
                return;
            }

            if (levels == null)
                throw new ArgumentNullException(nameof(levels));

            List<LevelSnapshot> snapshots;
            lock (stateLock)
            {
                snapshots = new List<LevelSnapshot>(levels.Count);
                foreach (var lvl in levels)
                    snapshots.Add(LevelSnapshot.FromLevel(lvl));
            }

            Total = snapshots.Count;
            Completed = 0;
            completedWorker = 0;
            IsRunning = true;

            if (Total == 0)
            {
                IsRunning = false;
                ProgressChanged?.Invoke(1f);
                return;
            }

            Task.Run(() =>
            {
                Parallel.ForEach(snapshots, snap =>
                {
                    var sw = Stopwatch.StartNew();
                    bool ok = IsPlayableSnapshot(snap, out string message);
                    sw.Stop();

                    results.Enqueue(new LevelCheckResult
                    {
                        levelName = snap?.Name ?? "(null)",
                        playable = ok,
                        message = message,
                        elapsedMilliseconds = sw.ElapsedMilliseconds
                    });

                    lock (stateLock)
                    {
                        completedWorker++;
                    }
                });
            }).ContinueWith(t =>
            {
                if (t.IsFaulted)
                    FileLogger.Write("Ошибка параллельной проверки.", t.Exception);

                results.Enqueue(new LevelCheckResult
                {
                    levelName = "__COMPLETE__",
                    playable = !t.IsFaulted,
                    message = "complete"
                });
            });
        }

        private void Update()
        {
            while (results.TryDequeue(out var result))
            {
                if (result.levelName == "__COMPLETE__")
                {
                    IsRunning = false;
                    continue;
                }

                Completed++;
                ResultReceived?.Invoke(result);
                ProgressChanged?.Invoke(Progress);
            }

            if (Total > 0 && !IsRunning && Completed >= Total)
                ProgressChanged?.Invoke(1f);
        }

        public static bool IsPlayable(Level level, out string message)
        {
            return IsPlayableSnapshot(LevelSnapshot.FromLevel(level), out message);
        }

        private static bool IsPlayableSnapshot(LevelSnapshot snap, out string message)
        {
            if (snap == null)
            {
                message = "Уровень не задан.";
                return false;
            }

            if (!string.IsNullOrEmpty(snap.ValidationError))
            {
                message = snap.ValidationError;
                return false;
            }

            int w = snap.Width;
            int h = snap.Height;

            var visited = new bool[w, h];
            var q = new Queue<GridPoint>();

            q.Enqueue(snap.Start);
            visited[snap.Start.x, snap.Start.y] = true;

            int[] dx = { 1, -1, 0, 0 };
            int[] dy = { 0, 0, 1, -1 };

            while (q.Count > 0)
            {
                var p = q.Dequeue();
                if (p.x == snap.Finish.x && p.y == snap.Finish.y)
                {
                    message = "Путь найден.";
                    return true;
                }

                for (int i = 0; i < 4; i++)
                {
                    int nx = p.x + dx[i];
                    int ny = p.y + dy[i];

                    if (nx < 0 || ny < 0 || nx >= w || ny >= h) continue;
                    if (visited[nx, ny] || snap.Blocked[nx, ny]) continue;

                    visited[nx, ny] = true;
                    q.Enqueue(new GridPoint(nx, ny));
                }
            }

            message = "Путь от старта к финишу не найден.";
            return false;
        }

        private sealed class LevelSnapshot
        {
            public string Name { get; private set; }
            public int Width { get; private set; }
            public int Height { get; private set; }
            public GridPoint Start { get; private set; }
            public GridPoint Finish { get; private set; }
            public bool[,] Blocked { get; private set; }
            public string ValidationError { get; private set; }

            public static LevelSnapshot FromLevel(Level level)
            {
                if (level == null)
                    return new LevelSnapshot { Name = "(null)", ValidationError = "Уровень не задан." };

                var errors = LevelValidationService.Validate(level);
                if (errors.Count > 0)
                {
                    return new LevelSnapshot
                    {
                        Name = level.name,
                        ValidationError = string.Join("; ", errors)
                    };
                }

                var blocked = new bool[level.gridWidth, level.gridHeight];
                if (level.objects != null)
                {
                    foreach (var o in level.objects)
                    {
                        if (o != null && o.type == LevelObjectType.Hazard)
                            blocked[o.position.x, o.position.y] = true;
                    }
                }

                return new LevelSnapshot
                {
                    Name = level.name,
                    Width = level.gridWidth,
                    Height = level.gridHeight,
                    Start = new GridPoint(level.start.x, level.start.y),
                    Finish = new GridPoint(level.finish.x, level.finish.y),
                    Blocked = blocked
                };
            }
        }
    }
}