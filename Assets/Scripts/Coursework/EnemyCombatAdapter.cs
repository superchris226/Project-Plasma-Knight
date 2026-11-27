// ШІ: ChatGPT – Створив EnemyCombatAdapter.
// Змінено: призначення - адаптер між доменним Combatant і Unity EnemyHealth/EnemyAI, синхронізує HP та надає Attack()
// з урахуванням дистанції. Адаптер бере компоненти MeleeEnemy або RangedEnemy з об'єкта, а не створює їх.
using UnityEngine;
using Coursework2D;

namespace Coursework2D
{
    [RequireComponent(typeof(EnemyHealth))]
    public class EnemyCombatAdapter : MonoBehaviour
    {
        private EnemyHealth health;
        private EnemyAI ai;
        
        // Посилання на компонент (один з двох)
        private MeleeEnemy meleeEnemy;
        private RangedEnemy rangedEnemy;

        private void Awake()
        {
            health = GetComponent<EnemyHealth>();
            ai = GetComponent<EnemyAI>();
            
            // Шукаємо, який саме компонент повішено на об'єкт
            meleeEnemy = GetComponent<MeleeEnemy>();
            rangedEnemy = GetComponent<RangedEnemy>();

            if (meleeEnemy != null && rangedEnemy != null)
            {
                Debug.LogError($"[EnemyCombatAdapter] {name}: На об'єкті одночасно MeleeEnemy і RangedEnemy! Залиш тільки один.");
            }
        }

        private void Start()
        {
            // Синхронізуємо здоров'я ворога з EnemyHealth
            SyncFromHealth();
        }

        public int AttackPlayer(Player player)
        {
            if (player == null) return 0;

            int distance = Mathf.RoundToInt(Vector2.Distance(transform.position, player.transform.position));
            var ctx = new AttackContext(distance, !player.IsDead);
            int dmg = 0;

            if (meleeEnemy != null && meleeEnemy.IsAlive)
            {
                dmg = meleeEnemy.Attack(ctx);
            }
            else if (rangedEnemy != null && rangedEnemy.IsAlive)
            {
                dmg = rangedEnemy.Attack(ctx);
            }

            if (dmg > 0)
                player.TakeDamage(dmg, transform.position, true);

            return dmg;
        }

        public void SyncFromHealth()
        {
            if (health == null) return;

            if (meleeEnemy != null)
            {
                meleeEnemy.maxHealth = health.maxHealth;
                meleeEnemy.currentHealth = health.health;
            }
            else if (rangedEnemy != null)
            {
                rangedEnemy.maxHealth = health.maxHealth;
                rangedEnemy.currentHealth = health.health;
            }
        }
    }
}