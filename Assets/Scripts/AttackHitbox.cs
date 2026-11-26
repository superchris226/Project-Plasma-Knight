// AttackHitbox.cs
// AI-ASSISTED: ChatGPT (GPT-4), 2024-11-21.
// Змінено: урон тепер може братися з PlayerCombatAdapter (доменний PlayerCharacter),
// зі збереженням сумісності з PlayerLevel.
using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Collider2D))]
public class AttackHitbox : MonoBehaviour
{
    [Header("Damage")]
    public int damage = 10;

    [Tooltip("Слои, которые считаются врагами")]
    public LayerMask enemyLayers = ~0;

    [Tooltip("Бить каждую цель только один раз за активацию")]
    public bool hitEachTargetOnce = true;

    [Tooltip("Брать урон из PlayerLevel (иначе — фиксированный damage)")]
    public bool usePlayerLevelDamage = true;

    private readonly HashSet<EnemyHealth> _hitThisSwing = new HashSet<EnemyHealth>();
    private Collider2D _col;
    private bool _isActive;
    private PlayerLevel _level;
    private PlayerCombatAdapter _combat; // Додано поле

    private void Awake()
    {
        _col = GetComponent<Collider2D>();
        _col.isTrigger = true;
        _col.enabled = false;
        _isActive = false;

        _level = GetComponentInParent<PlayerLevel>();
        _combat = GetComponentInParent<PlayerCombatAdapter>(); // Ініціалізація
        
        if (_level != null && usePlayerLevelDamage)
            damage = _level.CurrentDamage;
    }

    public void Activate()
    {
        _hitThisSwing.Clear();
        _isActive = true;
        if (_col != null) _col.enabled = true;

        if (_col != null)
        {
            var results = new Collider2D[16];
            var filter = new ContactFilter2D();
            filter.SetLayerMask(enemyLayers);
            filter.useTriggers = true;
            int count = Physics2D.OverlapCollider(_col, filter, results);
            for (int i = 0; i < count; i++)
            {
                if (results[i] != null)
                    TryHit(results[i]);
            }
        }
    }

    public void Deactivate()
    {
        _isActive = false;
        if (_col != null) _col.enabled = false;
    }

    private void OnTriggerEnter2D(Collider2D other) => TryHit(other);
    private void OnTriggerStay2D(Collider2D other)  => TryHit(other);

    private void TryHit(Collider2D other)
    {
        if (!_isActive || other == null) return;
        if (((1 << other.gameObject.layer) & enemyLayers) == 0) return;

        var enemy = other.GetComponent<EnemyHealth>();
        if (enemy == null) enemy = other.GetComponentInParent<EnemyHealth>();
        if (enemy == null || enemy.IsDead) return;

        if (hitEachTargetOnce && !_hitThisSwing.Add(enemy)) return;

        var player = GetComponentInParent<Player>();
        Vector2 attackerPos = player != null
            ? (Vector2)player.transform.position
            : (Vector2)transform.position;

        int finalDamage;
        if (_combat != null)
        {
            float dist = Vector2.Distance(attackerPos, enemy.transform.position);
            finalDamage = _combat.ComputeAttackDamage(Mathf.RoundToInt(dist));
        }
        else if (usePlayerLevelDamage && _level != null)
        {
            finalDamage = _level.CurrentDamage;
        }
        else
        {
            finalDamage = damage;
        }

        bool applied = enemy.TakeDamage(finalDamage, attackerPos);

        if (applied && enemy.IsDead && _level != null)
            _level.AddExperience(_level.xpPerEnemy);
    }

    private void OnDisable()
    {
        _isActive = false;
        _hitThisSwing.Clear();
    }
}