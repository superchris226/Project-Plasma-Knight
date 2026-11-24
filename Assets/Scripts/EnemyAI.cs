using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    [Header("Скорости")]
    public float patrolSpeed = 2f;
    public float chaseSpeed = 5f;

    [Header("Обнаружение")]
    public float detectionRadius = 2f;
    public Transform player;

    [Header("Патруль")]
    public float patrolDistance = 5f;

    [Header("Прыжок и стены")]
    public float jumpForce = 7f;
    public float wallCheckDistance = 0.8f;
    public LayerMask groundLayer;

    [Header("Физика")]
    public float groundGravity = 3f;

    private Vector2 startPosition;
    private int patrolDirection = 1;
    private Rigidbody2D rb;
    private bool isFlying = false;
    private bool isGrounded = false;

    [Header("Physics")]
    public float groundCheckDistance = 0.6f;
    public float groundCheckOffsetY = -0.5f;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        if (rb == null)
        {
            Debug.LogError("На враге нет Rigidbody2D!");
            return;
        }

        rb.freezeRotation = true;

        if (gameObject.CompareTag("Flying"))
        {
            isFlying = true;
            rb.gravityScale = 0f;
        }
        else
        {
            isFlying = false;
            rb.gravityScale = groundGravity;
        }
        var cols = GetComponentsInChildren<Collider2D>();
        foreach (var c in cols)
        {
            Debug.Log($"[Enemy] {name}: Collider {c.GetType().Name} " +
                    $"enabled={c.enabled} isTrigger={c.isTrigger} " +
                    $"layer={LayerMask.LayerToName(gameObject.layer)}", this);
        }
        Debug.Log($"[Enemy] {name}: RB bodyType={rb.bodyType} gravity={rb.gravityScale} simulated={rb.simulated}", this);
        startPosition = transform.position;
    }

    void FixedUpdate()
    {
        if (player == null || rb == null) return;

        if (!isFlying)
        {
            CheckGrounded();
        }

        float distance = Vector2.Distance(transform.position, player.position);

        if (distance <= detectionRadius)
        {
            Chase();
        }
        else
        {
            Patrol();
        }

        FlipSprite();
    }

    void Patrol()
    {
        if (transform.position.x > startPosition.x + patrolDistance)
            patrolDirection = -1;
        else if (transform.position.x < startPosition.x - patrolDistance)
            patrolDirection = 1;

        if (!isFlying && IsWallAhead(patrolDirection) && isGrounded)
        {
            Jump();
        }

        float targetYVelocity = isFlying ? 0f : GetVelocity().y;
        Vector2 velocity = new Vector2(patrolDirection * patrolSpeed, targetYVelocity);

        SetVelocity(velocity);
    }

    void Chase()
    {
        Vector2 direction = ((Vector2)player.position - (Vector2)transform.position).normalized;

        if (!isFlying)
        {
            direction.y = 0f;

            if (IsWallAhead(direction.x) && isGrounded)
            {
                Jump();
            }
        }

        Vector2 velocity = direction * chaseSpeed;

        if (!isFlying)
        {
            velocity.y = GetVelocity().y;
        }

        SetVelocity(velocity);
    }

    bool IsWallAhead(float directionX)
    {
        if (Mathf.Abs(directionX) < 0.1f) return false;
        Vector2 origin = (Vector2)transform.position + Vector2.up * groundCheckOffsetY;
        RaycastHit2D hit = Physics2D.Raycast(origin, new Vector2(Mathf.Sign(directionX), 0f), wallCheckDistance, groundLayer);
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
        float currentVelocityX = GetVelocity().x;

        if (currentVelocityX > 0.01f)
            transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        else if (currentVelocityX < -0.01f)
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
            Gizmos.DrawRay(transform.position, Vector2.down * 0.6f);
        }
    }
}