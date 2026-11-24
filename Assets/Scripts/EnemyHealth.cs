// EnemyHealth.cs
using UnityEngine;

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

    public bool IsDead => health <= 0;

    private float _hitCooldownTimer;
    private float _blinkTimer;
    private SpriteRenderer _spriteRenderer;
    private Color _baseColor = Color.white;
    private bool _dead;

    private void Awake()
    {
        health = Mathf.Clamp(health, 0, maxHealth);
        _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (_spriteRenderer != null) _baseColor = _spriteRenderer.color;
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

    /// <summary>
    /// Возвращает true, если урон реально прошёл (не был на кулдауне и враг жив).
    /// </summary>
    public bool TakeDamage(int amount, Vector2? attackerPosition = null)
    {
        if (_dead || amount <= 0) return false;
        if (_hitCooldownTimer > 0f) return false;

        health = Mathf.Max(0, health - amount);
        _hitCooldownTimer = hitCooldown;
        _blinkTimer = blinkDuration;

        if (_spriteRenderer != null)
            _spriteRenderer.color = Color.red;

        // Небольшой отброс (опционально)
        if (attackerPosition.HasValue)
        {
            var rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                float dir = Mathf.Sign(transform.position.x - attackerPosition.Value.x);
                if (dir == 0f) dir = 1f;
                rb.linearVelocity = new Vector2(dir * 4f, rb.linearVelocity.y);
            }
        }

        if (health <= 0) Die();
        return true;
    }

    private void Die()
    {
        if (_dead) return;
        _dead = true;

        // Отключаем ИИ и коллайдеры
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