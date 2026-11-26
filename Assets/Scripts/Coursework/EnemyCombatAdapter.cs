// EnemyCombatAdapter.cs
// AI-ASSISTED: ChatGPT (GPT-4), 2024-11-21.
// Призначення: адаптер між доменним Combatant і Unity EnemyHealth/EnemyAI.
// Синхронізує HP та надає Attack() з урахуванням дистанції.
using UnityEngine;

namespace Coursework2D
{
    [RequireComponent(typeof(EnemyHealth))]
    public class EnemyCombatAdapter : MonoBehaviour
    {
        public enum EnemyArchetype { Melee, Ranged }

        [SerializeField] private EnemyArchetype archetype = EnemyArchetype.Melee;
        [SerializeField] private int rangedAttackRange = 6;

        public Combatant Combatant { get; private set; }
        private EnemyHealth health;
        private EnemyAI ai;

        private void Awake()
        {
            health = GetComponent<EnemyHealth>();
            ai = GetComponent<EnemyAI>();
            RebuildCombatant();
        }

        public void SetArchetype(EnemyArchetype newArchetype)
        {
            archetype = newArchetype;
            RebuildCombatant();
        }

        private void RebuildCombatant()
        {
            if (health == null)
                health = GetComponent<EnemyHealth>();

            int max = health != null ? Mathf.Max(1, health.maxHealth) : 30;
            int power = archetype == EnemyArchetype.Melee ? 12 : 9;
            Combatant = archetype == EnemyArchetype.Melee
                ? new MeleeEnemy(maxHealth: max, attackPower: power)
                : new RangedEnemy(maxHealth: max, attackPower: power, attackRange: rangedAttackRange);

            if (health != null && health.health < Combatant.Health)
                Combatant.TakeDamage(Combatant.Health - health.health);
        }

        public int AttackPlayer(Player player)
        {
            if (player == null || Combatant == null || !Combatant.IsAlive) return 0;
            int distance = Mathf.RoundToInt(Vector2.Distance(transform.position, player.transform.position));
            int dmg = Combatant.Attack(new AttackContext(distance, !player.IsDead));
            if (dmg > 0) player.TakeDamage(dmg, transform.position, true);
            return dmg;
        }

        public void SyncFromHealth()
        {
            if (health == null || Combatant == null) return;
            if (health.health < Combatant.Health)
                Combatant.TakeDamage(Combatant.Health - health.health);
            else if (health.health > Combatant.Health)
                Combatant.Heal(health.health - Combatant.Health);
        }
    }
}