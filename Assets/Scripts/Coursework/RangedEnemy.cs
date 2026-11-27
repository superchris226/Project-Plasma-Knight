// ШІ: ChatGPT – Створив RangedEnemy.
// Змінено: фікс кода
using UnityEngine;

namespace Coursework2D
{
    public sealed class RangedEnemy : MonoBehaviour
    {
        [Header("Stats")]
        public string combatantName = "Ranged Enemy";
        public int maxHealth = 35;
        public int currentHealth;
        public int attackPower = 9;
        public int attackRange = 6;

        public bool IsAlive => currentHealth > 0;

        private void Awake()
        {
            currentHealth = maxHealth;
            if (attackRange <= 0) attackRange = 1; // Захист від ділення на нуль
        }

        public int Attack(AttackContext context)
        {
            if (!IsAlive || !context.TargetIsAlive) return 0;
            if (context.Distance > attackRange) return 0;
            
            double factor = 1.0 - 0.5 * ((double)context.Distance / attackRange);
            return (int)System.Math.Round(attackPower * factor);
        }

        public void TakeDamage(int amount)
        {
            if (amount <= 0 || !IsAlive) return;
            currentHealth = Mathf.Max(0, currentHealth - amount);
        }
    }
}