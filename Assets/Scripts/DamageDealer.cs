// DamageDealer.cs
// By me
using UnityEngine;
using System.Collections.Generic;

public enum DamageMode { AwayFromSource, BackwardsFromFacing }
public enum DamageSourceType { Enemy, Pain }

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

    [Tooltip("Если true — наносить урон один раз и уничтожиться")]
    public bool destroyAfterHit = false;

    [Tooltip("Кулдаун на каждого игрока отдельно, сек. 0 = каждый кадр")]
    public float perTargetCooldown = 0.5f;

    [Tooltip("Максимум целей за один кадр (0 = без лимита)")]
    public int maxTargetsPerFrame = 0;

    private readonly Dictionary<Player, float> _nextHitTimes = new();

    private void Awake()
    {
        var col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.isTrigger = true; // зона урона обязана быть триггером
        }
    }

    private void OnTriggerEnter2D(Collider2D other) => TryHit(other);
    private void OnTriggerStay2D(Collider2D other)  => TryHit(other);
    private void OnCollisionEnter2D(Collision2D c)  => TryHit(c.collider);
    private void OnCollisionStay2D(Collision2D c)   => TryHit(c.collider);

    private void TryHit(Collider2D other)
    {
        if (other == null) return;

        if (targetLayers.value != 0 &&
            ((targetLayers.value & (1 << other.gameObject.layer)) == 0)) return;

        var player = other.GetComponent<Player>() ?? other.GetComponentInParent<Player>();
        if (player == null) return;

        // Неуязвимость (после получения урона / респавн)
        if (player.IsInvulnerable) return;

        // Пока игрок атакует — он НЕ получает урон от касания.
        // ВАЖНО: это должно проверяться и в OnTriggerStay2D, иначе при
        // удержании врага игрок будет получать урон, даже если бьёт.
        if (player.IsAttacking) return;

        if (perTargetCooldown > 0f)
        {
            if (_nextHitTimes.TryGetValue(player, out float nextTime) && Time.time < nextTime)
                return;
            _nextHitTimes[player] = Time.time + perTargetCooldown;
        }

        bool useAttackerDir = mode == DamageMode.AwayFromSource;
        player.TakeDamage(damage, transform.position, useAttackerDir);

        if (destroyAfterHit) Destroy(gameObject);
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