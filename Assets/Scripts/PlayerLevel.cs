// PlayerLevel.cs
//By me
using UnityEngine;

public class PlayerLevel : MonoBehaviour
{
    [Header("Level")]
    public int level = 1;
    public int experience = 0;
    public int expToNext = 100;

    [Header("Damage Scaling")]
    [Tooltip("Базовий урон (AttackHitbox)")]
    public int baseDamage = 10;
    [Tooltip("Прибавка урону за кожен рівень")]
    public int damagePerLevel = 5;

    [Header("XP Sources")]
    [Tooltip("Скільки досвіду за вбивство звичайного ворога")]
    public int xpPerEnemy = 25;

    public int CurrentDamage => baseDamage + (level - 1) * damagePerLevel;
    public float ProgressToNextLevel => expToNext > 0 ? (float)experience / expToNext : 0f;

    public System.Action<int> OnLevelUp;

    public void AddExperience(int amount)
    {
        if (amount <= 0) return;
        experience += amount;
        while (experience >= expToNext)
        {
            experience -= expToNext;
            LevelUp();
        }
    }

    private void LevelUp()
    {
        level++;
        expToNext = Mathf.RoundToInt(expToNext * 1.25f);
        OnLevelUp?.Invoke(level);
        Debug.Log($"[PlayerLevel] Рівень {level}, урон {CurrentDamage}, досвід до наст. {expToNext}");
    }

    public void ResetLevel()
    {
        level = 1;
        experience = 0;
        expToNext = 100;
    }
}