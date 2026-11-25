// AttackHitbox.cs
// By me
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

    private readonly HashSet<EnemyHealth> _hitThisSwing = new();
    private Collider2D _col;
    private bool _isActive;
    private PlayerLevel _level;

    private void Awake()
    {
        _col = GetComponent<Collider2D>();
        _col.isTrigger = true;
        _col.enabled = false;
        _isActive = false;

        _level = GetComponentInParent<PlayerLevel>();
        if (_level != null && usePlayerLevelDamage)
            damage = _level.CurrentDamage;
    }

    public void Activate()
    {
        _hitThisSwing.Clear();
        _isActive = true;
        if (_col != null) _col.enabled = true;
    }

    public void Deactivate()
    {
        _isActive = false;
        if (_col != null) _col.enabled = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!_isActive) return;
        if (((1 << other.gameObject.layer) & enemyLayers) == 0) return;

        var enemy = other.GetComponent<EnemyHealth>();
        if (enemy == null) enemy = other.GetComponentInParent<EnemyHealth>();
        if (enemy == null || enemy.IsDead) return;

        if (hitEachTargetOnce && !_hitThisSwing.Add(enemy)) return;

        var player = GetComponentInParent<Player>();
        Vector2 attackerPos = player != null
            ? (Vector2)player.transform.position
            : (Vector2)transform.position;

        int finalDamage = (usePlayerLevelDamage && _level != null) ? _level.CurrentDamage : damage;
        bool killed = enemy.health <= finalDamage;
        enemy.TakeDamage(finalDamage, attackerPos);

        // Начисляем опыт только если враг умер от этого удара
        if (killed && _level != null && enemy.IsDead)
            _level.AddExperience(_level.xpPerEnemy);
    }

    private void OnDisable()
    {
        _isActive = false;
        _hitThisSwing.Clear();
    }
}