using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Coursework2D
{
    // Контролер MVC: UI-кнопки можуть викликати ці публічні методи.
    public class LevelEditorController : MonoBehaviour
    {
        public LevelCollection Data { get; private set; } = new LevelCollection();
        public int SelectedIndex { get; private set; } = -1;
        private LevelRepository repository;
        public event Action<string> Message;
        public event Action DataChanged;

        private void Awake()
        {
            repository = new LevelRepository();
            try { Data = repository.Load(); }
            catch (Exception ex) { Message?.Invoke("Не вдалося завантажити levels.json: " + ex.Message); Data = LevelRepository.CreateStarterCollection(); }
            if (Data.levels.Count > 0) SelectedIndex = 0;
        }

        public IReadOnlyList<Level> Search(string query, int difficulty = 0)
        {
            IEnumerable<Level> q = Data.levels;
            if (!string.IsNullOrWhiteSpace(query)) q = q.Where(l => l.name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0);
            if (difficulty >= 1 && difficulty <= 5) q = q.Where(l => l.difficulty == difficulty);
            return q.ToList();
        }
        public IReadOnlyList<Level> SortByDifficulty(bool descending = false) => descending ? Data.levels.OrderByDescending(l => l.difficulty).ToList() : Data.levels.OrderBy(l => l.difficulty).ToList();
        public IReadOnlyList<Level> SortByObjectCount(bool descending = false) => descending ? Data.levels.OrderByDescending(l => l.objectCount).ToList() : Data.levels.OrderBy(l => l.objectCount).ToList();

        public bool CreateLevel(string name, int difficulty, int width, int height)
        {
            var level = new Level { name = name, difficulty = difficulty, gridWidth = width, gridHeight = height,
                start = new GridPoint(0, 0), finish = new GridPoint(width - 1, height - 1) };
            level.objects.Add(new LevelObject(LevelObjectType.Start, new Vector2Int(0, 0)));
            level.objects.Add(new LevelObject(LevelObjectType.Finish, new Vector2Int(width - 1, height - 1)));
            var errors = LevelValidationService.Validate(level, Data.levels);
            if (errors.Count > 0) { Message?.Invoke(string.Join("\n", errors)); return false; }
            Data.levels.Add(level); SelectedIndex = Data.levels.Count - 1; Changed("Рівень створено."); return true;
        }

        public bool UpdateLevel(int index, Level edited)
        {
            if (index < 0 || index >= Data.levels.Count || edited == null) { Message?.Invoke("Вибраний рівень не знайдено."); return false; }
            var others = Data.levels.Where((_, i) => i != index);
            var errors = LevelValidationService.Validate(edited, others);
            if (errors.Count > 0) { Message?.Invoke(string.Join("\n", errors)); return false; }
            Data.levels[index] = edited; SelectedIndex = index; Changed("Рівень оновлено."); return true;
        }

        public bool DeleteLevel(int index)
        {
            if (index < 0 || index >= Data.levels.Count) { Message?.Invoke("Вибраний рівень не знайдено."); return false; }
            Data.levels.RemoveAt(index); SelectedIndex = Data.levels.Count == 0 ? -1 : Mathf.Clamp(index, 0, Data.levels.Count - 1);
            Changed("Рівень видалено."); return true;
        }

        public bool AddObject(int levelIndex, LevelObject obj)
        {
            if (!ValidIndex(levelIndex) || obj == null) return false;
            var level = Data.levels[levelIndex];
            if (!LevelValidationService.Inside(level, obj.position.x, obj.position.y)) { Message?.Invoke("Координати об'єкта поза сіткою."); return false; }
            if (obj.type == LevelObjectType.Start && level.objects.Any(o => o.type == LevelObjectType.Start)) { Message?.Invoke("Старт вже існує."); return false; }
            if (obj.type == LevelObjectType.Finish && level.objects.Any(o => o.type == LevelObjectType.Finish)) { Message?.Invoke("Фініш вже існує."); return false; }
            level.objects.Add(obj); Changed("Об'єкт додано."); return true;
        }
        public bool RemoveObject(int levelIndex, string objectId)
        {
            if (!ValidIndex(levelIndex)) return false;
            var list = Data.levels[levelIndex].objects;
            int removed = list.RemoveAll(o => o.id == objectId);
            if (removed == 0) { Message?.Invoke("Об'єкт не знайдено."); return false; }
            Changed("Об'єкт видалено."); return true;
        }
        public void Save() { try { repository.Save(Data); Message?.Invoke("Збережено: " + repository.DefaultPath); } catch (Exception ex) { Message?.Invoke("Помилка збереження: " + ex.Message); } }
        public void Reload() { try { Data = repository.Load(); SelectedIndex = Data.levels.Count > 0 ? 0 : -1; Changed("Рівні завантажено."); } catch (Exception ex) { Message?.Invoke("Помилка завантаження: " + ex.Message); } }
        public void ImportJson(string path) { try { Data = repository.Load(path); SelectedIndex = Data.levels.Count > 0 ? 0 : -1; Changed("JSON імпортовано."); } catch (Exception ex) { Message?.Invoke("Помилка імпорту: " + ex.Message); } }
        public void ExportJson(string path) { try { repository.Save(Data, path); Message?.Invoke("JSON експортовано."); } catch (Exception ex) { Message?.Invoke("Помилка експорту: " + ex.Message); } }

        private bool ValidIndex(int i) { if (i >= 0 && i < Data.levels.Count) return true; Message?.Invoke("Рівень не вибрано."); return false; }
        private void Changed(string message) { Message?.Invoke(message); DataChanged?.Invoke(); }
    }
}