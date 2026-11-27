// ШІ: ChatGPT – Створив MeleeEnemy.
// Змінено: фікс кода, щоб був пов'язан з Enemy Health
using UnityEngine;

namespace Coursework2D
{
    // Прибрано : Combatant, додано : MonoBehaviour
    public sealed class MeleeEnemy : MonoBehaviour
    {
        [Header("Stats")]
        public string combatantName = "Melee Enemy";
        public int maxHealth = 50;
        public int currentHealth;
        public int attackPower = 12;

        public bool IsAlive => currentHealth > 0;

        private void Awake()
        {
            // Unity не викликає конструктори для MonoBehaviour, тому ініціалізуємо тут
            currentHealth = maxHealth;
        }

        public int Attack(AttackContext context)
        {
            if (!IsAlive || !context.TargetIsAlive) return 0;
            return context.Distance <= 1 ? attackPower : 0;
        }

        public void TakeDamage(int amount)
        {
            if (amount <= 0 || !IsAlive) return;
            currentHealth = Mathf.Max(0, currentHealth - amount);
        }
    }
}