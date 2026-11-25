using System.Collections.Generic;
using System.Linq;

namespace Coursework2D
{
    public static class LevelValidationService
    {
        public static List<string> Validate(Level level, IEnumerable<Level> otherLevels = null)
        {
            var errors = new List<string>();
            if (level == null) { errors.Add("Уровень не задан."); return errors; }
            if (string.IsNullOrWhiteSpace(level.name)) errors.Add("Название уровня не может быть пустым.");
            if (otherLevels != null && otherLevels.Any(x => x != null && !ReferenceEquals(x, level) &&
                string.Equals(x.name?.Trim(), level.name?.Trim(), System.StringComparison.OrdinalIgnoreCase)))
                errors.Add("Название уровня должно быть уникальным.");
            if (level.difficulty < 1 || level.difficulty > 5) errors.Add("Сложность должна быть от 1 до 5.");
            if (level.gridWidth < 2 || level.gridHeight < 2) errors.Add("Размер сетки должен быть не меньше 2×2.");
            if (level.start == null || !Inside(level, level.start.x, level.start.y)) errors.Add("Старт вне границ сетки.");
            if (level.finish == null || !Inside(level, level.finish.x, level.finish.y)) errors.Add("Финиш вне границ сетки.");
            if (level.start != null && level.finish != null && level.start.x == level.finish.x && level.start.y == level.finish.y)
                errors.Add("Старт и финиш должны находиться в разных клетках.");
            if (level.objects == null) errors.Add("Список объектов отсутствует.");
            else
            {
                foreach (var obj in level.objects)
                {
                    if (obj == null) { errors.Add("Список содержит пустой объект."); continue; }
                    if (!Inside(level, obj.position.x, obj.position.y)) errors.Add($"Объект {obj.id} находится вне сетки.");
                }
                if (level.objects.Count(x => x != null && x.type == LevelObjectType.Start) != 1) errors.Add("Должен быть ровно один объект Start.");
                if (level.objects.Count(x => x != null && x.type == LevelObjectType.Finish) != 1) errors.Add("Должен быть ровно один объект Finish.");
            }
            if (level.enemies == null) errors.Add("Список врагов отсутствует.");
            else foreach (var enemy in level.enemies)
            {
                if (enemy == null) { errors.Add("Список содержит пустого врага."); continue; }
                if (!Inside(level, enemy.position.x, enemy.position.y)) errors.Add($"Враг {enemy.id} находится вне сетки.");
                if (enemy.health <= 0) errors.Add($"Здоровье врага {enemy.id} должно быть больше нуля.");
            }
            return errors;
        }

        public static bool Inside(Level level, int x, int y) => level != null && x >= 0 && y >= 0 && x < level.gridWidth && y < level.gridHeight;
    }
}
