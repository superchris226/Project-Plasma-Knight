// AttackHitbox.cs
using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Collider2D))]
public class AttackHitbox : MonoBehaviour
{
    [Header("Damage")]
    public int damage = 10;

    [Tooltip("Слои, которые считаются врагами")]
    public LayerMask enemyLayers = ~0;

    [Tooltip("Бить каждую цель только один раз за одну активацию")]
    public bool hitEachTargetOnce = true;

    private readonly HashSet<EnemyHealth> _hitThisSwing = new();
    private Collider2D _col;
    private bool _isActive;

    private void Awake()
    {
        _col = GetComponent<Collider2D>();
        _col.isTrigger = true;
        _col.enabled = false;
        _isActive = false;
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

        enemy.TakeDamage(damage, attackerPos);
    }

    private void OnDisable()
    {
        _isActive = false;
        _hitThisSwing.Clear();
    }
}