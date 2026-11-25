using System;

namespace Coursework2D
{
    // Доменная модель: намеренно не наследуется от MonoBehaviour.
    public abstract class Combatant : IDamageable
    {
        public string Name { get; protected set; }
        public int MaxHealth { get; protected set; }
        public int Health { get; protected set; }
        public int AttackPower { get; protected set; }
        public bool IsAlive => Health > 0;

        protected Combatant(string name, int maxHealth, int attackPower)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Имя не может быть пустым.", nameof(name));
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
