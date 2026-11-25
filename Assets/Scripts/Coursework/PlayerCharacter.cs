namespace Coursework2D
{
    public sealed class PlayerCharacter : Combatant
    {
        public PlayerCharacter(string name = "Scarf", int maxHealth = 100, int attackPower = 20)
            : base(name, maxHealth, attackPower) { }
        public override int Attack() => IsAlive ? AttackPower : 0;
    }
}
