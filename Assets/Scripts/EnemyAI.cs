// EnemyAI.cs
// AI-ASSISTED: ChatGPT (GPT-4), 2024-11-21.
// Змінено: у ближньому бою ворог тепер викликає доменний Attack().
using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    [Header("Скорости")]
    public float patrolSpeed = 2f;
    public float chaseSpeed = 5f;

    [Header("Обнаружение")]
    public float detectionRadius = 5f;
    public Transform player;

    [Header("Патруль")]
    public float patrolDistance = 5f;

    [Header("Прыжок и стены")]
    public float jumpForce = 7f;
    public float wallCheckDistance = 0.8f;
    public LayerMask groundLayer;

    [Header("Физика")]
    public float groundGravity = 3f;
    public float groundCheckDistance = 0.6f;
    public float groundCheckOffsetY = -0.5f;

    [Header("Knockback")]
    [Tooltip("Длительность паузы ИИ после отброса")]
    public float knockbackDuration = 0.25f;
    [Tooltip("Трение, гасящее горизонтальную скорость отброса")]
    public float knockbackDrag = 20f;

    private Vector2 startPosition;
    private int patrolDirection = 1;
    private Rigidbody2D rb;
    private bool isFlying = false;
    private bool isGrounded = false;

    private float _knockbackTimer;
    
    // Додано для Combat
    private EnemyCombatAdapter _combatAdapter;
    private float _attackCooldown;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        _combatAdapter = GetComponent<EnemyCombatAdapter>();
        
        if (rb == null)
        {
            Debug.LogError($"[EnemyAI] {name}: нет Rigidbody2D!", this);
            enabled = false;
            return;
        }

        rb.freezeRotation = true;
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.simulated = true;

        isFlying = gameObject.CompareTag("Flying");
        rb.gravityScale = isFlying ? 0f : groundGravity;

        var mainCol = GetComponent<Collider2D>();
        if (mainCol != null && mainCol.isTrigger)
        {
            Debug.LogWarning($"[EnemyAI] {name}: основной Collider2D стоит IsTrigger=true. Выключи!", this);
        }

        foreach (var dd in GetComponentsInChildren<DamageDealer>())
        {
            var c = dd.GetComponent<Collider2D>();
            if (c != null) c.isTrigger = true;
        }

        if (player == null)
        {
            var p = FindAnyObjectByType<Player>();
            if (p != null) player = p.transform;
        }

        startPosition = transform.position;
    }

    public void ApplyKnockback(Vector2 velocity, float duration)
    {
        if (rb == null) return;
        if (isFlying) velocity.y = 0f;
        rb.linearVelocity = velocity;
        _knockbackTimer = Mathf.Max(_knockbackTimer, duration);
    }

    void FixedUpdate()
    {
        if (rb == null) return;

        if (_knockbackTimer > 0f)
        {
            _knockbackTimer -= Time.fixedDeltaTime;
            var v = GetVelocity();
            v.x = Mathf.MoveTowards(v.x, 0f, knockbackDrag * Time.fixedDeltaTime);
            SetVelocity(v);
            FlipSprite();
            return;
        }

        if (player == null)
        {
            var p = FindAnyObjectByType<Player>();
            if (p != null) player = p.transform;
        }

        if (!isFlying) CheckGrounded();

        bool playerValid = player != null;
        float distance = playerValid
            ? Vector2.Distance(transform.position, player.position)
            : float.MaxValue;

        if (playerValid && distance <= detectionRadius)
            Chase();
        else
            Patrol();

        FlipSprite();
    }

    void Patrol()
    {
        if (transform.position.x > startPosition.x + patrolDistance)
            patrolDirection = -1;
        else if (transform.position.x < startPosition.x - patrolDistance)
            patrolDirection = 1;

        if (!isFlying && isGrounded && IsWallAhead(patrolDirection))
            Jump();

        float targetYVelocity = isFlying ? 0f : GetVelocity().y;
        SetVelocity(new Vector2(patrolDirection * patrolSpeed, targetYVelocity));
    }

    void Chase()
    {
        Vector2 direction = ((Vector2)player.position - (Vector2)transform.position).normalized;

        if (!isFlying)
        {
            direction.y = 0f;
            if (isGrounded && IsWallAhead(direction.x))
                Jump();
        }

        Vector2 velocity = direction * chaseSpeed;
        if (!isFlying) velocity.y = GetVelocity().y;

        SetVelocity(velocity);

        // Атака через доменну модель
        _attackCooldown -= Time.fixedDeltaTime;
        if (_combatAdapter != null && _attackCooldown <= 0f)
        {
            var p = player.GetComponent<Player>();
            if (p != null && _combatAdapter.AttackPlayer(p) > 0)
                _attackCooldown = 0.8f;
        }
    }

    bool IsWallAhead(float directionX)
    {
        if (Mathf.Abs(directionX) < 0.1f) return false;
        Vector2 origin = (Vector2)transform.position + Vector2.up * groundCheckOffsetY;
        RaycastHit2D hit = Physics2D.Raycast(
            origin, new Vector2(Mathf.Sign(directionX), 0f),
            wallCheckDistance, groundLayer);
        return hit.collider != null;
    }

    void CheckGrounded()
    {
        Vector2 origin = (Vector2)transform.position + Vector2.up * groundCheckOffsetY;
        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, groundCheckDistance, groundLayer);
        isGrounded = hit.collider != null;
    }

    void Jump()
    {
        Vector2 vel = GetVelocity();
        vel.y = jumpForce;
        SetVelocity(vel);
    }

    Vector2 GetVelocity()
    {
        #if UNITY_6000_0_OR_NEWER
            return rb.linearVelocity;
        #else
            return rb.velocity;
        #endif
    }

    void SetVelocity(Vector2 vel)
    {
        #if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = vel;
        #else
            rb.velocity = vel;
        #endif
    }

    void FlipSprite()
    {
        float vx = GetVelocity().x;
        if (vx > 0.01f)
            transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        else if (vx < -0.01f)
            transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        Gizmos.color = Color.yellow;
        Vector2 origin = Application.isPlaying ? startPosition : (Vector2)transform.position;
        Gizmos.DrawLine(origin + Vector2.left * patrolDistance, origin + Vector2.right * patrolDistance);

        if (Application.isPlaying)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position, new Vector2(patrolDirection * wallCheckDistance, 0));
            Gizmos.color = Color.green;
            Gizmos.DrawRay(transform.position, Vector2.down * groundCheckDistance);
        }
    }
}