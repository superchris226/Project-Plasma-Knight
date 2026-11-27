// EnemyHealth.cs
//By me
// Змінено: Chat GPT змімив TakeDamage. Тепер повідомляє EnemyCombatAdapter про урон,
// а Die() реєструє вбивство ворога в GameplayStatisticsTracker.
using UnityEngine;
using Coursework2D;

public class EnemyHealth : MonoBehaviour
{
    [Header("Health")]
    public int maxHealth = 30;
    public int health = 30;

    [Header("Death")]
    [Tooltip("Знищити об'єкт при смерті")]
    public bool destroyOnDeath = true;
    [Tooltip("Затримка перед знищенням (для програвання анімації)")]
    public float destroyDelay = 0.5f;

    [Header("Feedback")]
    [Tooltip("Коротка невразливість після удару, щоб одна атака не била багато разів")]
    public float hitCooldown = 0.4f;
    [Tooltip("Миготіння при отриманні урону")]
    public float blinkDuration = 0.2f;

    [Header("Knockback")]
    [Tooltip("Горизонтальна сила відкидання")]
    public float knockbackForce = 8f;
    [Tooltip("Вертикальний підскок при відкиданні")]
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