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
            FileLogger.CaptureMainThreadPath();

            if (IsRunning)
            {
                FileLogger.Write("Перевірка вже виконується.");
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
                    FileLogger.Write("Помилка паралельної перевірки.", t.Exception);

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
                message = "Рівень не задано.";
                return false;
            }

            if (!string.IsNullOrEmpty(snap.ValidationError))
            {
                message = snap.ValidationError;
                return false;
            }

            int w = snap.Width;
            int h = snap.Height;
            const int maxJumpHeight = 3;
            const int maxJumps = 2;

            bool HasSupport(int x, int y)
            {
                if (y <= 0) return true;
                return snap.Platform[x, y - 1];
            }

            bool IsBlocked(int x, int y)
            {
                return x < 0 || y < 0 || x >= w || y >= h || snap.Hazard[x, y];
            }

            if (IsBlocked(snap.Start.x, snap.Start.y) || IsBlocked(snap.Finish.x, snap.Finish.y))
            {
                message = "Старт або фініш заблоковані перешкодою.";
                return false;
            }

            var visited = new bool[w, h, maxJumps + 1, maxJumpHeight + 1];
            var q = new Queue<(int x, int y, int jumpsUsed, int jumpRem)>();

            visited[snap.Start.x, snap.Start.y, 0, 0] = true;
            q.Enqueue((snap.Start.x, snap.Start.y, 0, 0));

            while (q.Count > 0)
            {
                var cur = q.Dequeue();
                if (cur.x == snap.Finish.x && cur.y == snap.Finish.y)
                {
                    message = "Шлях знайдено з урахуванням гравітації, стрибків і перешкод.";
                    return true;
                }

                bool grounded = HasSupport(cur.x, cur.y);
                int jumpsUsed = grounded ? 0 : cur.jumpsUsed;
                int jumpRem = grounded ? 0 : cur.jumpRem;

                if (grounded && jumpsUsed < maxJumps)
                {
                    TryEnqueue(cur.x, cur.y + 1, 1, maxJumpHeight - 1);
                }
                else if (!grounded && jumpRem > 0)
                {
                    TryEnqueue(cur.x, cur.y + 1, jumpsUsed, jumpRem - 1);
                }
                else if (!grounded && jumpsUsed < maxJumps)
                {
                    TryEnqueue(cur.x, cur.y + 1, jumpsUsed + 1, maxJumpHeight - 1);
                }

                if (!grounded && jumpRem == 0)
                {
                    TryEnqueue(cur.x, cur.y - 1, jumpsUsed, 0);
                }

                for (int dx = -1; dx <= 1; dx += 2)
                {
                    int nx = cur.x + dx;
                    if (IsBlocked(nx, cur.y)) continue;

                    bool nextGrounded = HasSupport(nx, cur.y);
                    int nextJumps = nextGrounded ? 0 : jumpsUsed;
                    int nextRem = nextGrounded ? 0 : (grounded ? 0 : Mathf.Max(0, jumpRem - 1));
                    TryEnqueue(nx, cur.y, nextJumps, nextRem);
                }

                void TryEnqueue(int nx, int ny, int nj, int nrem)
                {
                    if (IsBlocked(nx, ny)) return;
                    nj = Mathf.Clamp(nj, 0, maxJumps);
                    nrem = Mathf.Clamp(nrem, 0, maxJumpHeight);
                    if (!visited[nx, ny, nj, nrem])
                    {
                        visited[nx, ny, nj, nrem] = true;
                        q.Enqueue((nx, ny, nj, nrem));
                    }
                }
            }

            message = "Шлях від старту до фінішу не знайдено з урахуванням гравітації та стрибків.";
            return false;
        }

        private sealed class LevelSnapshot
        {
            public string Name { get; private set; }
            public int Width { get; private set; }
            public int Height { get; private set; }
            public GridPoint Start { get; private set; }
            public GridPoint Finish { get; private set; }
            public bool[,] Hazard { get; private set; }
            public bool[,] Platform { get; private set; }
            public string ValidationError { get; private set; }

            public static LevelSnapshot FromLevel(Level level)
            {
                if (level == null)
                    return new LevelSnapshot { Name = "(null)", ValidationError = "Рівень не задано." };

                var errors = LevelValidationService.Validate(level);
                if (errors.Count > 0)
                {
                    return new LevelSnapshot
                    {
                        Name = level.name,
                        ValidationError = string.Join("; ", errors)
                    };
                }

                var hazard = new bool[level.gridWidth, level.gridHeight];
                var platform = new bool[level.gridWidth, level.gridHeight];

                if (level.objects != null)
                {
                    foreach (var o in level.objects)
                    {
                        if (o == null) continue;
                        if (!LevelValidationService.Inside(level, o.position.x, o.position.y)) continue;
                        if (o.type == LevelObjectType.Hazard)
                            hazard[o.position.x, o.position.y] = true;
                        else if (o.type == LevelObjectType.Platform)
                            platform[o.position.x, o.position.y] = true;
                    }
                }

                return new LevelSnapshot
                {
                    Name = level.name,
                    Width = level.gridWidth,
                    Height = level.gridHeight,
                    Start = new GridPoint(level.start.x, level.start.y),
                    Finish = new GridPoint(level.finish.x, level.finish.y),
                    Hazard = hazard,
                    Platform = platform
                };
            }
        }
    }
}