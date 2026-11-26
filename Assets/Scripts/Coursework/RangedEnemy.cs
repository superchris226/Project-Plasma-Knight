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

        public override int Attack(AttackContext context)
        {
            if (!IsAlive || !context.TargetIsAlive) return 0;
            if (context.Distance > AttackRange) return 0;
            double factor = 1.0 - 0.5 * ((double)context.Distance / AttackRange);
            return (int)System.Math.Round(AttackPower * factor);
        }
    }
}