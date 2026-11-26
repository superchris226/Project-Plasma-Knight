// EnemyHealth.cs
// AI-ASSISTED: ChatGPT (GPT-4), 2024-11-21.
// Змінено: TakeDamage тепер повідомляє EnemyCombatAdapter про урон,
// а Die() реєструє вбивство ворога в GameplayStatisticsTracker.
using UnityEngine;
using Coursework2D;

public class EnemyHealth : MonoBehaviour
{
    [Header("Health")]
    public int maxHealth = 30;
    public int health = 30;

    [Header("Death")]
    [Tooltip("Уничтожить объект при смерти")]
    public bool destroyOnDeath = true;
    [Tooltip("Задержка перед уничтожением (для проигрывания анимации)")]
    public float destroyDelay = 0.5f;

    [Header("Feedback")]
    [Tooltip("Короткая неуязвимость после удара, чтобы одна атака не била много раз")]
    public float hitCooldown = 0.4f;
    [Tooltip("Мигание при получении урона")]
    public float blinkDuration = 0.2f;

    [Header("Knockback")]
    [Tooltip("Горизонтальная сила отброса")]
    public float knockbackForce = 8f;
    [Tooltip("Вертикальный подскок при отбросе")]
    public float knockbackUpForce = 4f;

    public bool IsDead => health <= 0;

    private float _hitCooldownTimer;
    private float _blinkTimer;
    private SpriteRenderer _spriteRenderer;
    private Color _baseColor = Color.white;
    private bool _dead;

    private EnemyCombatAdapter _combatAdapter;

    private void Awake()
    {
        health = Mathf.Clamp(health, 0, maxHealth);
        _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (_spriteRenderer != null) _baseColor = _spriteRenderer.color;
        _combatAdapter = GetComponent<EnemyCombatAdapter>();
    }

    private void Update()
    {
        if (_hitCooldownTimer > 0f) _hitCooldownTimer -= Time.deltaTime;

        if (_blinkTimer > 0f)
        {
            _blinkTimer -= Time.deltaTime;
            if (_spriteRenderer != null)
            {
                bool visible = Mathf.FloorToInt(_blinkTimer / 0.05f) % 2 == 0;
                _spriteRenderer.enabled = visible;
                if (_blinkTimer <= 0f)
                {
                    _spriteRenderer.enabled = true;
                    _spriteRenderer.color = _baseColor;
                }
            }
        }
    }

    public bool TakeDamage(int amount, Vector2? attackerPosition = null)
    {
        if (_dead || amount <= 0) return false;
        if (_hitCooldownTimer > 0f) return false;

        health = Mathf.Max(0, health - amount);
        _hitCooldownTimer = hitCooldown;
        _blinkTimer = blinkDuration;

        if (_spriteRenderer != null)
            _spriteRenderer.color = Color.red;

        _combatAdapter?.SyncFromHealth();

        if (attackerPosition.HasValue)
        {
            float dir = Mathf.Sign(transform.position.x - attackerPosition.Value.x);
            if (dir == 0f) dir = 1f;

            Vector2 knockback = new Vector2(dir * knockbackForce, knockbackUpForce);

            var ai = GetComponent<EnemyAI>();
            if (ai != null)
                ai.ApplyKnockback(knockback, ai.knockbackDuration);
            else
            {
                var rb = GetComponent<Rigidbody2D>();
                if (rb != null) rb.linearVelocity = knockback;
            }
        }

        if (health <= 0) Die();
        return true;
    }

    private void Die()
    {
        if (_dead) return;
        _dead = true;

        var tracker = FindFirstObjectByType<GameplayStatisticsTracker>();
        if (tracker != null)
            tracker.RegisterEnemyDefeated();

        var ai = GetComponent<EnemyAI>();
        if (ai != null) ai.enabled = false;

        foreach (var col in GetComponentsInChildren<Collider2D>())
            col.enabled = false;

        var rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        if (destroyOnDeath)
            Destroy(gameObject, destroyDelay);

        Debug.Log($"[EnemyHealth] {name} убит.");
    }
}