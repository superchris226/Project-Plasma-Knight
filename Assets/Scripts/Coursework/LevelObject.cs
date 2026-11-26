using System;
using UnityEngine;

namespace Coursework2D
{
    public enum LevelObjectType { Platform, Hazard, Coin, Start, Finish, Decoration }
    public enum EnemyType { Melee, Ranged }

    [Serializable]
    public class LevelObject
    {
        public string id = Guid.NewGuid().ToString("N");
        public LevelObjectType type = LevelObjectType.Platform;
        public Vector2Int position;

        public LevelObject() { }
        public LevelObject(LevelObjectType type, Vector2Int position)
        { this.type = type; this.position = position; }
    }

    [Serializable]
    public class EnemyData
    {
        public string id = Guid.NewGuid().ToString("N");
        public EnemyType type = EnemyType.Melee;
        public Vector2Int position;
        public int health = 30;
    }
}