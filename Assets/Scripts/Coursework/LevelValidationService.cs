using System.Collections.Generic;
using System.Linq;

namespace Coursework2D
{
    public static class LevelValidationService
    {
        public static List<string> Validate(Level level, IEnumerable<Level> otherLevels = null)
        {
            var errors = new List<string>();
            if (level == null) { errors.Add("Рівень не задано."); return errors; }
            if (string.IsNullOrWhiteSpace(level.name)) errors.Add("Назва рівня не може бути порожньою.");
            if (otherLevels != null && otherLevels.Any(x => x != null && !ReferenceEquals(x, level) &&
                string.Equals(x.name?.Trim(), level.name?.Trim(), System.StringComparison.OrdinalIgnoreCase)))
                errors.Add("Назва рівня має бути унікальною.");
            if (level.difficulty < 1 || level.difficulty > 5) errors.Add("Складність має бути від 1 до 5.");
            if (level.gridWidth < 2 || level.gridHeight < 2) errors.Add("Розмір сітки має бути не менше 2×2.");
            if (level.start == null || !Inside(level, level.start.x, level.start.y)) errors.Add("Старт поза межами сітки.");
            if (level.finish == null || !Inside(level, level.finish.x, level.finish.y)) errors.Add("Фініш поза межами сітки.");
            if (level.start != null && level.finish != null && level.start.x == level.finish.x && level.start.y == level.finish.y)
                errors.Add("Старт і фініш мають бути в різних клітинках.");
            if (level.objects == null) errors.Add("Список об'єктів відсутній.");
            else
            {
                foreach (var obj in level.objects)
                {
                    if (obj == null) { errors.Add("Список містить порожній об'єкт."); continue; }
                    if (!Inside(level, obj.position.x, obj.position.y)) errors.Add($"Об'єкт {obj.id} знаходиться поза сіткою.");
                }
                if (level.objects.Count(x => x != null && x.type == LevelObjectType.Start) != 1) errors.Add("Повинен бути рівно один об'єкт Start.");
                if (level.objects.Count(x => x != null && x.type == LevelObjectType.Finish) != 1) errors.Add("Повинен бути рівно один об'єкт Finish.");
            }
            if (level.enemies == null) errors.Add("Список ворогів відсутній.");
            else foreach (var enemy in level.enemies)
            {
                if (enemy == null) { errors.Add("Список містить порожнього ворога."); continue; }
                if (!Inside(level, enemy.position.x, enemy.position.y)) errors.Add($"Ворог {enemy.id} знаходиться поза сіткою.");
                if (enemy.health <= 0) errors.Add($"Здоров'я ворога {enemy.id} має бути більше нуля.");
            }
            return errors;
        }

        public static bool Inside(Level level, int x, int y) => level != null && x >= 0 && y >= 0 && x < level.gridWidth && y < level.gridHeight;
    }
}