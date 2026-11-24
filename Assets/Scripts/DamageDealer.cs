// DamageDealer.cs
// By me
using UnityEngine;
using System.Collections.Generic;

public enum DamageMode
{
    AwayFromSource,
    BackwardsFromFacing
}

public enum DamageSourceType
{
    Enemy,
    Pain
}

[RequireComponent(typeof(Collider2D))]
public class DamageDealer : MonoBehaviour
{
    [Header("Source")]
    public DamageSourceType sourceType = DamageSourceType.Enemy;

    [Header("Damage")]
    public int damage = 10;

    [Header("Knockback")]
    public DamageMode mode = DamageMode.AwayFromSource;

    [Header("Target filtering")]
    public LayerMask targetLayers = ~0;

    [Tooltip("Если true — наносить урон только один раз и уничтожиться")]
    public bool destroyAfterHit = false;

    [Tooltip("Кулдаун на каждого игрока отдельно, сек. 0 = каждый кадр")]
    public float perTargetCooldown = 0.5f;

    [Tooltip("Максимум целей за один кадр (0 = без лимита)")]
    public int maxTargetsPerFrame = 0;

    private readonly Dictionary<Player, float> _nextHitTimes = new();

    private void Awake()
    {
        var col = GetComponent<Collider2D>();

        // Автопроверка: DamageDealer должен быть на ТРИГГЕРЕ.
        // Если это не так — предупреждаем, чтобы враг не проваливался.
        if (col != null && !col.isTrigger)
        {
            Debug.LogWarning(
                $"[DamageDealer] {name}: коллайдер НЕ триггер. " +
                "Если это единственный коллайдер на враге — он провалится сквозь пол! " +
                "Вынеси зону урона в дочерний объект с IsTrigger = true.", this);
        }
    }

    private void OnTriggerEnter2D(Collider2D other) => TryHit(other);
    private void OnTriggerStay2D(Collider2D other)  => TryHit(other);
    private void OnCollisionEnter2D(Collision2D collision) => TryHit(collision.collider);
    private void OnCollisionStay2D(Collision2D collision)  => TryHit(collision.collider);

    private void TryHit(Collider2D other)
    {
        if (other == null) return;

        if (targetLayers.value != 0 &&
            ((targetLayers.value & (1 << other.gameObject.layer)) == 0)) return;

        var player = other.GetComponent<Player>()
                  ?? other.GetComponentInParent<Player>();
        if (player == null) return;
        if (player.IsInvulnerable) return;

        if (perTargetCooldown > 0f)
        {
            if (_nextHitTimes.TryGetValue(player, out float nextTime) && Time.time < nextTime)
                return;
            _nextHitTimes[player] = Time.time + perTargetCooldown;
        }

        bool useAttackerDir = mode == DamageMode.AwayFromSource;
        player.TakeDamage(damage, transform.position, useAttackerDir);

        if (destroyAfterHit)
            Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = sourceType == DamageSourceType.Enemy
            ? new Color(1f, 0.2f, 0.2f, 0.35f)
            : new Color(1f, 0.6f, 0.1f, 0.35f);
        var col = GetComponent<Collider2D>();
        if (col != null) Gizmos.DrawCube(col.bounds.center, col.bounds.size);
    }
}