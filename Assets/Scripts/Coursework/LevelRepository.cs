using System;
using System.Collections.Generic;
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

        public LevelCollection Load(string path = null)
        {
            bool useDefaultPath = string.IsNullOrWhiteSpace(path);

            if (useDefaultPath)
                path = DefaultPath;

            try
            {
                string json = null;

                // Спочатку перевіряємо збережені користувачем рівні.
                if (File.Exists(path))
                {
                    json = File.ReadAllText(path);
                }
                else if (useDefaultPath)
                {
                    // Якщо збереження немає, завантажуємо початкові рівні.
                    string starterPath = Path.Combine(
                        Application.streamingAssetsPath,
                        "levels.json"
                    );

                    if (File.Exists(starterPath))
                        json = File.ReadAllText(starterPath);
                }

                LevelCollection data = null;

                if (!string.IsNullOrWhiteSpace(json))
                {
                    data = JsonConvert.DeserializeObject<LevelCollection>(json);
                }

                // Якщо файла немає або в ньому немає рівнів,
                // створюємо стартову колекцію.
                if (data == null || data.levels == null ||
                    data.levels.Count == 0)
                {
                    data = CreateStarterCollection();
                }

                // Перевіряємо коректність кожного рівня.
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

                // Зберігаємо початкові рівні в користувацький файл.
                if (useDefaultPath &&
                    (!File.Exists(path) || string.IsNullOrWhiteSpace(json)))
                {
                    Save(data, path);
                }

                return data;
            }
            catch (Exception ex)
            {
                FileLogger.Write(
                    "Помилка завантаження рівнів: " + path,
                    ex
                );

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
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(path, JsonConvert.SerializeObject(collection, Formatting.Indented));
            }
            catch (Exception ex) { FileLogger.Write("Помилка збереження рівнів: " + path, ex); throw; }
        }

        public async Task<LevelCollection> LoadAsync(string path = null) => await Task.Run(() => Load(path));

        public static LevelCollection CreateStarterCollection()
        {
            var c = new LevelCollection();
            for (int i = 1; i <= 10; i++)
            {
                var l = new Level { name = $"Рівень {i:00}", difficulty = ((i - 1) % 5) + 1,
                    gridWidth = 20, gridHeight = 12, start = new GridPoint(1, 1), finish = new GridPoint(18, 1) };
                for (int x = 0; x < 20; x++) l.objects.Add(new LevelObject(LevelObjectType.Platform, new Vector2Int(x, 0)));
                l.objects.Add(new LevelObject(LevelObjectType.Start, new Vector2Int(1, 1)));
                l.objects.Add(new LevelObject(LevelObjectType.Finish, new Vector2Int(18, 1)));
                if (i > 1) l.enemies.Add(new EnemyData { type = i % 2 == 0 ? EnemyType.Melee : EnemyType.Ranged, position = new Vector2Int(8, 1), health = 20 + i * 2 });
                if (i > 3) l.objects.Add(new LevelObject(LevelObjectType.Coin, new Vector2Int(5, 2)));
                c.levels.Add(l);
            }
            return c;
        }
    }
}