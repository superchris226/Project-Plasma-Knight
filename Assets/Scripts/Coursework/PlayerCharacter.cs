// PlayerCharacter.cs
// AI-ASSISTED: ChatGPT (GPT-4), 2024-11-21.
// Змінено: Attack() тепер по-різному: кожен 4-й удар — критичний (x2).

namespace Coursework2D
{
    public sealed class PlayerCharacter : Combatant
    {
        private int attackCounter;

        public PlayerCharacter(string name = "Scarf", int maxHealth = 100, int attackPower = 20)
            : base(name, maxHealth, attackPower) { }

        public override int Attack(AttackContext context)
        {
            if (!IsAlive || !context.TargetIsAlive) return 0;
            
            attackCounter++;
            bool crit = attackCounter % 4 == 0;
            return crit ? AttackPower * 2 : AttackPower;
        }
    }
}