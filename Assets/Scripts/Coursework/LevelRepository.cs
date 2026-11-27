// ШІ: ChatGPT – Створив LevelRepository.
// Змінено: фікс кода.
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

namespace Coursework2D
{
    public class LevelRepository
    {
        public string DefaultPath => Path.Combine(Application.persistentDataPath, "levels.json");

        public string GetDefaultSavePathMainThread() => Path.Combine(Application.persistentDataPath, "levels.json");
        public string GetStarterLevelsPathMainThread() => Path.Combine(Application.streamingAssetsPath, "levels.json");

        public LevelCollection Load(string path = null)
        {
            string defaultSavePath = GetDefaultSavePathMainThread();
            string starterLevelsPath = GetStarterLevelsPathMainThread();
            return Load(path, defaultSavePath, starterLevelsPath);
        }

        public LevelCollection Load(string path, string defaultSavePath, string starterLevelsPath)
        {
            bool useDefaultPath = string.IsNullOrWhiteSpace(path);
            string targetPath = useDefaultPath ? defaultSavePath : path;

            try
            {
                string json = null;

                if (!string.IsNullOrWhiteSpace(targetPath) && File.Exists(targetPath))
                {
                    json = File.ReadAllText(targetPath);
                }
                else if (useDefaultPath && !string.IsNullOrWhiteSpace(starterLevelsPath) && File.Exists(starterLevelsPath))
                {
                    json = File.ReadAllText(starterLevelsPath);
                }

                LevelCollection data = null;
                if (!string.IsNullOrWhiteSpace(json))
                {
                    data = JsonConvert.DeserializeObject<LevelCollection>(json);
                }

                if (data == null || data.levels == null || data.levels.Count == 0)
                {
                    data = CreateStarterCollection();
                }

                for (int i = 0; i < data.levels.Count; i++)
                {
                    var level = data.levels[i];
                    var errors = LevelValidationService.Validate(
                        level,
                        data.levels.Where((_, index) => index != i)
                    );

                    if (errors.Count > 0)
                    {
                        throw new InvalidDataException(
                            $"Рівень '{level?.name}': " +
                            string.Join("; ", errors)
                        );
                    }
                }

                if (useDefaultPath && !string.IsNullOrWhiteSpace(targetPath) &&
                    (!File.Exists(targetPath) || string.IsNullOrWhiteSpace(json)))
                {
                    Save(data, targetPath);
                }

                return data;
            }
            catch (Exception ex)
            {
                FileLogger.Write("Помилка завантаження рівнів: " + targetPath, ex);
                throw;
            }
        }

        public void Save(LevelCollection collection, string path = null)
        {
            path = string.IsNullOrWhiteSpace(path) ? DefaultPath : path;
            try
            {
                if (collection == null || collection.levels == null) throw new ArgumentNullException(nameof(collection));
                for (int i = 0; i < collection.levels.Count; i++)
                {
                    var errors = LevelValidationService.Validate(collection.levels[i], collection.levels.Where((_, idx) => idx != i));
                    if (errors.Count > 0) throw new InvalidDataException($"Рівень '{collection.levels[i]?.name}': {string.Join("; ", errors)}");
                }
                string dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir))
                    Directory.CreateDirectory(dir);
                File.WriteAllText(path, JsonConvert.SerializeObject(collection, Formatting.Indented));
            }
            catch (Exception ex) { FileLogger.Write("Помилка збереження рівнів: " + path, ex); throw; }
        }

        public async Task<LevelCollection> LoadAsync(string path = null)
        {
            FileLogger.CaptureMainThreadPath();
            string defaultSavePath = GetDefaultSavePathMainThread();
            string starterLevelsPath = GetStarterLevelsPathMainThread();
            string targetPath = path;

            return await Task.Run(() => Load(targetPath, defaultSavePath, starterLevelsPath));
        }

        public static LevelCollection CreateStarterCollection()
        {
            var c = new LevelCollection();
            for (int i = 1; i <= 10; i++)
            {
                var l = new Level
                {
                    name = $"Рівень {i:00}",
                    difficulty = ((i - 1) % 5) + 1,
                    gridWidth = 20,
                    gridHeight = 12,
                    start = new GridPoint(1, 1),
                    finish = new GridPoint(18, 1)
                };

                for (int x = 0; x < 20; x++)
                {
                    if (i >= 3 && (x == 9 || x == 10))
                        continue;
                    l.objects.Add(new LevelObject(LevelObjectType.Platform, new Vector2Int(x, 0)));
                }

                if (i >= 2)
                {
                    l.objects.Add(new LevelObject(LevelObjectType.Platform, new Vector2Int(6, 2)));
                    l.objects.Add(new LevelObject(LevelObjectType.Platform, new Vector2Int(7, 2)));
                }

                if (i == 2)
                {
                    l.objects.Add(new LevelObject(LevelObjectType.Hazard, new Vector2Int(6, 1)));
                }
                else if (i >= 4)
                {
                    l.objects.Add(new LevelObject(LevelObjectType.Hazard, new Vector2Int(12, 1)));
                    l.objects.Add(new LevelObject(LevelObjectType.Platform, new Vector2Int(12, 2)));
                }

                l.objects.Add(new LevelObject(LevelObjectType.Start, new Vector2Int(1, 1)));
                l.objects.Add(new LevelObject(LevelObjectType.Finish, new Vector2Int(18, 1)));

                if (i > 1)
                    l.enemies.Add(new EnemyData { type = i % 2 == 0 ? EnemyType.Melee : EnemyType.Ranged, position = new Vector2Int(8, 1), health = 20 + i * 2 });
                if (i > 3)
                    l.objects.Add(new LevelObject(LevelObjectType.Coin, new Vector2Int(5, 2)));

                c.levels.Add(l);
            }
            return c;
        }
    }
}