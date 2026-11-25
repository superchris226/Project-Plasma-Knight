namespace Coursework2D
{
    public sealed class RangedEnemy : Combatant
    {
        public int AttackRange { get; }
        public RangedEnemy(string name = "Ranged Enemy", int maxHealth = 35, int attackPower = 9, int attackRange = 6)
            : base(name, maxHealth, attackPower)
        {
            if (attackRange <= 0) throw new System.ArgumentOutOfRangeException(nameof(attackRange));
            AttackRange = attackRange;
        }
        public override int Attack() => IsAlive ? AttackPower : 0;
    }
}
