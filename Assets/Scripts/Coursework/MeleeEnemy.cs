//використання Chat GPT
namespace Coursework2D
{
    public sealed class MeleeEnemy : Combatant
    {
        public MeleeEnemy(string name = "Melee Enemy", int maxHealth = 50, int attackPower = 12)
            : base(name, maxHealth, attackPower) { }

        public override int Attack(AttackContext context)
        {
            if (!IsAlive || !context.TargetIsAlive) return 0;
            return context.Distance <= 1 ? AttackPower : 0;
        }
    }
}