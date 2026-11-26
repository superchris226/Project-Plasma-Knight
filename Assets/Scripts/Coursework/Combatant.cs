using System;

namespace Coursework2D
{
    // Доменна модель: навмисно не наслідується від MonoBehaviour.
    public abstract class Combatant : IDamageable
    {
        public string Name { get; protected set; }
        public int MaxHealth { get; protected set; }
        public int Health { get; protected set; }
        public int AttackPower { get; protected set; }
        public bool IsAlive => Health > 0;

        protected Combatant(string name, int maxHealth, int attackPower)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Ім'я не може бути порожнім.", nameof(name));
            if (maxHealth <= 0) throw new ArgumentOutOfRangeException(nameof(maxHealth));
            if (attackPower < 0) throw new ArgumentOutOfRangeException(nameof(attackPower));
            Name = name; MaxHealth = maxHealth; Health = maxHealth; AttackPower = attackPower;
        }

        public abstract int Attack();

        public virtual void TakeDamage(int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            Health = Math.Max(0, Health - amount);
        }

        public void Heal(int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            Health = Math.Min(MaxHealth, Health + amount);
        }
    }
}