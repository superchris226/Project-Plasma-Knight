// ШІ: ChatGPT – Створив PlayerCharacter.
// Змінено: Attack() тепер по-різному: кожен 4-й удар — критичний (x2). А також зробив так, щоб був пов'язан з Player.cs тег з його системою
using UnityEngine;

namespace Coursework2D
{
    public sealed class PlayerCharacter : MonoBehaviour
    {
        [Header("Stats")]
        public string combatantName = "Scarf";
        public int maxHealth = 100;
        public int currentHealth;
        public int attackPower = 20;

        public bool IsAlive => currentHealth > 0;

        private int attackCounter;

        private void Awake()
        {
            currentHealth = maxHealth;
        }

        public int Attack(AttackContext context)
        {
            if (!IsAlive || !context.TargetIsAlive) return 0;
            
            attackCounter++;
            bool crit = attackCounter % 4 == 0;
            return crit ? attackPower * 2 : attackPower;
        }

        public void TakeDamage(int amount)
        {
            if (amount <= 0 || !IsAlive) return;
            currentHealth = Mathf.Max(0, currentHealth - amount);
        }
    }
}