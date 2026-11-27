// ШІ: ChatGPT – Створив Level.
// Змінено: фікс кода.
using System;
using System.Collections.Generic;

namespace Coursework2D
{
    [Serializable]
    public class Level
    {
        public string name = "Новий рівень";
        public int difficulty = 1; // 1..5
        public int gridWidth = 20;
        public int gridHeight = 12;
        public GridPoint start = new GridPoint(1, 1);
        public GridPoint finish = new GridPoint(18, 1);
        public List<LevelObject> objects = new List<LevelObject>();
        public List<EnemyData> enemies = new List<EnemyData>();
        public int objectCount => (objects?.Count ?? 0) + (enemies?.Count ?? 0);
    }

    [Serializable]
    public class GridPoint
    {
        public int x;
        public int y;
        public GridPoint() { }
        public GridPoint(int x, int y) { this.x = x; this.y = y; }
    }

    [Serializable]
    public class LevelCollection
    {
        public int schemaVersion = 1;
        public List<Level> levels = new List<Level>();
    }
}