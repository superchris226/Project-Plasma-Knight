// Player.cs
// By SuperChris and Scarf/Me
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class Player : MonoBehaviour
{
    [Header("Health")]
    public int health = 100;
    public int maxHealth = 100;

    [Header("Ground Movement")]
    public float maxSpeed = 12f;
    public float acceleration = 30f;
    public float deceleration = 40f;
    public float friction = 20f;
    public float turnMultiplier = 2f;

    [Header("Air Movement")]
    public float airAcceleration = 12f;
    public float airDeceleration = 8f;

    [Header("Sonic Jump")]
    public float jumpForce = 14f;
    public float jumpCutMultiplier = 0.55f;
    public float airDragThreshold = 4f;
    public float airDragFactor = 0.96875f;

    [Header("Double Jump")]
    public float doubleJumpAnimDuration = 0.4f;

    [Header("Attack")]
    [Tooltip("Длительность состояния атаки (сек). Должна совпадать с длиной клипа Attack.")]
    public float attackDuration = 0.5f;
    [Tooltip("Множитель скорости движения во время атаки (0 = стоим на месте, 1 = полная скорость)")]
    [Range(0f, 1f)] public float attackMoveMultiplier = 0f;
    [Tooltip("Можно ли прервать атаку прыжком")]
    public bool attackCancelableByJump = false;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.15f;
    public LayerMask groundLayer;

    [Header("Damage / Knockback")]
    public float painDuration = 0.6f;
    public float knockbackHorizontal = 9f;
    public float knockbackVertical = 8f;
    public float knockbackDrag = 22f;
    public float invulnerabilityDuration = 1.2f;
    public float invulnerabilityBlinkInterval = 0.08f;

    [Header("Low Health Blink")]
    public int hitsToDieForLowHealth = 2;
    public int assumedHitDamage = 10;
    public int lowHealthThreshold = 20;
    public Color lowHealthColor = new Color(1f, 0.25f, 0.25f, 1f);
    public float lowHealthBlinkInterval = 0.15f;

    [Header("Visual")]
    [SerializeField] private Transform spriteTransform;

    private Rigidbody2D rb;
    private float moveInput;
    private bool isGrounded;
    private bool isDoubleJumping;
    private bool doubleJumpPressed;
    private float jumpBufferCounter;
    private float jumpBufferTime = 0.12f;
    private bool hasJumped;
    private bool canCutJump;
    private float doubleJumpAnimTimer;

    private float facingDirection = 1f;

    private float painTimer;
    private float invulnerabilityTimer;
    private float blinkTimer;

    private float attackTimer;
    private bool attackInputPressed;

    public bool DeathAnimation;
    private bool isDead;

    private SpriteRenderer spriteRenderer;
    private Color baseSpriteColor = Color.white;

    [Header("HP Bar Animation")]
    public float healthBarLerpSpeed = 1.5f;
    public float healthBarDelay = 0.15f;

    [Header("HP Bar Ghost")]
    public Image HealthImageGhost;
    public float ghostLerpSpeed = 0.6f;
    public float ghostDelay = 0.3f;

    private float displayedGhostHealth;
    private float ghostDelayTimer;

    private float displayedHealth;
    private float healthBarDelayTimer;
    public Image HealthImage;

    private const float SpeedEpsilon = 0.01f;

    public float HorizontalSpeed => rb != null ? Mathf.Abs(rb.linearVelocity.x) : 0f;
    public float VerticalSpeed => rb != null ? rb.linearVelocity.y : 0f;
    public bool IsGrounded => isGrounded;
    public bool JustJumped => hasJumped && !isGrounded;
    public bool SpriteDoubleJumping => doubleJumpAnimTimer > 0f && !isGrounded;
    public bool IsSkidding => false;
    public float FacingDirection => facingDirection;

    public int PainAnimationState => painTimer > 0f ? 1 : 0;
    public bool PainIsGrounded { get; private set; }
    public bool IsInvulnerable => invulnerabilityTimer > 0f;
    public bool IsDead => isDead;
    public bool IsLowHealthState => IsLowHealth();

    public bool IsAttacking => attackTimer > 0f;
    public AttackHitbox attackHitbox;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        health = Mathf.Clamp(health, 0, maxHealth);
        displayedHealth = maxHealth > 0 ? (float)health / maxHealth : 0f;
        if (spriteTransform == null)
        {
            var sr = GetComponentInChildren<SpriteRenderer>();
            if (sr != null)
                spriteTransform = sr.transform;
        }

        if (spriteTransform != null)
            spriteRenderer = spriteTransform.GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (spriteRenderer != null)
            baseSpriteColor = spriteRenderer.color;
    }

    private void UpdateHealthBar()
    {
        if (HealthImage == null) return;
        float target = maxHealth > 0 ? Mathf.Clamp01((float)health / maxHealth) : 0f;
        if (target < displayedHealth)
        {
            healthBarDelayTimer -= Time.deltaTime;
            if (healthBarDelayTimer > 0f)
            {
                HealthImage.fillAmount = displayedHealth;
                return;
            }
        } else {
            healthBarDelayTimer = healthBarDelay;
        }

        displayedHealth = Mathf.MoveTowards(displayedHealth, target, healthBarLerpSpeed * Time.deltaTime);
        HealthImage.fillAmount = displayedHealth;
    }

    private void UpdateGhostHealthBar()
    {
        if (HealthImage == null) return;
        float target = maxHealth > 0 ? Mathf.Clamp01((float)health / maxHealth) : 0f;
        if (HealthImageGhost != null)
        {
            if (displayedHealth < displayedGhostHealth)
            {
                ghostDelayTimer -= Time.deltaTime;
                if (ghostDelayTimer > 0f)
                {
                    HealthImageGhost.fillAmount = displayedGhostHealth;
                    return;
                }
            } else {
                displayedGhostHealth = displayedHealth;
                ghostDelayTimer = ghostDelay;
            }

            displayedGhostHealth = Mathf.MoveTowards(displayedGhostHealth, displayedHealth, ghostLerpSpeed * Time.deltaTime);
            HealthImageGhost.fillAmount = displayedGhostHealth;
        }
    }

    private void Update()
    {
        UpdateTimers();
        ReadInput();
        UpdateInvulnerabilityBlink();
        UpdateHealthBar();
        UpdateGhostHealthBar();
    }

    private void FixedUpdate()
    {
        UpdateDoubleJumpTimer();
        CheckGround();

        if (health <= 0)
        {
            OnDeath();
            return;
        }

        if (painTimer > 0f)
            ApplyKnockbackDrag();
        else
        {
            TryStartAttack();
            ApplyHorizontalMovement();
            HandleJump();
            ApplySonicAirDrag();
        }

        UpdateFacing();
    }

    private void UpdateTimers()
    {
        float dt = Time.deltaTime;

        if (painTimer > 0f)
        {
            painTimer -= dt;
            if (painTimer <= 0f)
            {
                painTimer = 0f;
                PainIsGrounded = false;
            }
        }

        if (invulnerabilityTimer > 0f)
            invulnerabilityTimer -= dt;

        if (attackTimer > 0f)
        {
            attackTimer -= dt;
            if (attackTimer <= 0f)
            {
                attackTimer = 0f;
                if (attackHitbox != null)
                    attackHitbox.Deactivate();
            }
        }
    }

    private void UpdateInvulnerabilityBlink()
    {
        if (spriteTransform == null || spriteRenderer == null) return;

        if (isDead)
        {
            spriteRenderer.enabled = true;
            spriteRenderer.color = baseSpriteColor;
            return;
        }

        if (invulnerabilityTimer > 0f)
        {
            blinkTimer += Time.deltaTime;
            bool visible = Mathf.FloorToInt(blinkTimer / Mathf.Max(0.01f, invulnerabilityBlinkInterval)) % 2 == 0;
            spriteRenderer.enabled = visible;
            spriteRenderer.color = baseSpriteColor;
            return;
        }

        if (IsLowHealth())
        {
            spriteRenderer.enabled = true;
            blinkTimer += Time.deltaTime;
            bool redOn = Mathf.FloorToInt(blinkTimer / Mathf.Max(0.01f, lowHealthBlinkInterval)) % 2 == 0;
            spriteRenderer.color = redOn ? lowHealthColor : baseSpriteColor;
            return;
        }

        blinkTimer = 0f;
        spriteRenderer.enabled = true;
        spriteRenderer.color = baseSpriteColor;
    }

    private bool IsLowHealth()
    {
        if (health <= 0) return false;

        int threshold;
        if (assumedHitDamage > 0)
            threshold = Mathf.Max(1, hitsToDieForLowHealth * assumedHitDamage);
        else
            threshold = Mathf.Max(1, lowHealthThreshold);

        return health <= threshold;
    }

    private void ReadInput()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null) return;
        if (painTimer > 0f) return;
        if (DeathAnimation == true) return;

        moveInput = 0f;
        if (kb.leftArrowKey.isPressed) moveInput = -1f;
        else if (kb.rightArrowKey.isPressed) moveInput = 1f;

        if (kb.zKey.wasPressedThisFrame)
        {
            jumpBufferCounter = jumpBufferTime;
            doubleJumpPressed = true;
        }

        if (kb.xKey.wasPressedThisFrame)
        {
            attackInputPressed = true;
        }

        if (kb.zKey.wasReleasedThisFrame && canCutJump && rb.linearVelocity.y > 0f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);
            canCutJump = false;
        }

        if (jumpBufferCounter > 0f)
            jumpBufferCounter -= Time.deltaTime;
    }

    private void UpdateFacing()
    {
        if (spriteTransform == null) return;

        if (painTimer <= 0f)
        {
            if (Mathf.Abs(moveInput) > SpeedEpsilon)
                facingDirection = Mathf.Sign(moveInput);
            else if (Mathf.Abs(rb.linearVelocity.x) > 0.1f)
                facingDirection = Mathf.Sign(rb.linearVelocity.x);
        }

        Vector3 scale = spriteTransform.localScale;
        scale.x = Mathf.Abs(scale.x) * facingDirection;
        spriteTransform.localScale = scale;
    }

    private void CheckGround()
    {
        if (groundCheck != null)
            isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        else
            isGrounded = Mathf.Abs(rb.linearVelocity.y) < 0.01f;

        if (isGrounded && rb.linearVelocity.y <= 0f)
        {
            hasJumped = false;
            isDoubleJumping = false;
            canCutJump = false;
        }
    }

    private void TryStartAttack()
    {
        if (!attackInputPressed) return;
        attackInputPressed = false;

        if (attackTimer > 0f || isDead) return;
        if (painTimer > 0f) return;

        attackTimer = attackDuration;

        if (attackHitbox != null)
            attackHitbox.Activate();
    }
    private void HandleJump()
    {
        if (IsAttacking && !attackCancelableByJump) return;

        if (jumpBufferCounter > 0f)
        {
            if (isGrounded)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
                hasJumped = true;
                canCutJump = true;
                isDoubleJumping = false;
                jumpBufferCounter = 0f;
                doubleJumpPressed = false;
            }
            else if (hasJumped && !isDoubleJumping && doubleJumpPressed)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
                doubleJumpAnimTimer = doubleJumpAnimDuration;
                isDoubleJumping = true;
                canCutJump = true;
                doubleJumpPressed = false;
            }
        }
    }

    private void UpdateDoubleJumpTimer()
    {
        if (doubleJumpAnimTimer > 0f)
            doubleJumpAnimTimer -= Time.fixedDeltaTime;
    }

    private void ApplySonicAirDrag()
    {
        if (!isGrounded && rb.linearVelocity.y > 0f && rb.linearVelocity.y < airDragThreshold)
        {
            rb.linearVelocity = new Vector2(
                rb.linearVelocity.x * airDragFactor,
                rb.linearVelocity.y
            );
        }
    }

    private void ApplyHorizontalMovement()
    {
        float currentSpeed = rb.linearVelocity.x;
        float accel = isGrounded ? acceleration : airAcceleration;
        float decel = isGrounded ? deceleration : airDeceleration;

        float inputMultiplier = IsAttacking ? attackMoveMultiplier : 1f;

        if (Mathf.Abs(moveInput) > SpeedEpsilon && inputMultiplier > 0.01f)
        {
            float targetDir = Mathf.Sign(moveInput);
            float currentDir = Mathf.Abs(currentSpeed) > SpeedEpsilon ? Mathf.Sign(currentSpeed) : 0f;
            float rate = (currentDir != 0f && targetDir != currentDir)
                ? accel * turnMultiplier
                : accel;

            currentSpeed = Mathf.MoveTowards(
                currentSpeed,
                moveInput * maxSpeed * inputMultiplier,
                rate * Time.fixedDeltaTime
            );
        }
        else
        {
            float rate = isGrounded ? decel + friction : decel;
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, rate * Time.fixedDeltaTime);
        }

        rb.linearVelocity = new Vector2(currentSpeed, rb.linearVelocity.y);
    }

    private void ApplyKnockbackDrag()
    {
        float vx = Mathf.MoveTowards(rb.linearVelocity.x, 0f, knockbackDrag * Time.fixedDeltaTime);
        rb.linearVelocity = new Vector2(vx, rb.linearVelocity.y);
    }

    private void OnDeath()
    {
        if (isDead) return;
        isDead = true;
        DeathAnimation = true;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.gravityScale = 0f;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        moveInput = 0f;
        jumpBufferCounter = 0f;
        doubleJumpPressed = false;
        canCutJump = false;
        hasJumped = false;
        isDoubleJumping = false;
        doubleJumpAnimTimer = 0f;
        painTimer = 0f;
        invulnerabilityTimer = 0f;
        attackTimer = 0f;
        attackInputPressed = false;

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
            spriteRenderer.color = baseSpriteColor;
        }

        Debug.Log("[Player] Умер. Движение заблокировано, физика остановлена.");
    }

    public void TakeDamage(int amount, Vector2? attackerPosition = null, bool useAttackerDirection = true, bool ignoreInvulnerability = false)
    {
        if (amount <= 0 || health <= 0 || isDead) return;
        if (IsInvulnerable && !ignoreInvulnerability) return;

        health = Mathf.Max(0, health - amount);

        float knockbackDir;
        if (useAttackerDirection && attackerPosition.HasValue)
        {
            float deltaX = rb.position.x - attackerPosition.Value.x;
            knockbackDir = Mathf.Abs(deltaX) > 0.001f
                ? Mathf.Sign(deltaX)
                : -facingDirection;
        }
        else
        {
            knockbackDir = -facingDirection;
        }

        PainIsGrounded = isGrounded;
        rb.linearVelocity = new Vector2(knockbackDir * knockbackHorizontal, knockbackVertical);
        painTimer = Mathf.Max(painTimer, painDuration);
        invulnerabilityTimer = Mathf.Max(invulnerabilityTimer, invulnerabilityDuration);
        blinkTimer = 0f;

        hasJumped = false;
        isDoubleJumping = false;
        canCutJump = false;
        jumpBufferCounter = 0f;
        doubleJumpPressed = false;
        doubleJumpAnimTimer = 0f;

        attackTimer = 0f;
        attackInputPressed = false;
    }

    public void Heal(int amount)
    {
        if (amount <= 0 || isDead) return;
        health = Mathf.Min(maxHealth, health + amount);
    }

    // ==========================================
    // ДОБАВЛЕНО ДЛЯ РЕСПАВНА
    // ==========================================
    public void ResetAfterDeath()
    {
        isDead = false;
        DeathAnimation = false;

        health = maxHealth;
        displayedHealth = 1f;
        displayedGhostHealth = 1f;
        if (HealthImage != null) HealthImage.fillAmount = 1f;
        if (HealthImageGhost != null) HealthImageGhost.fillAmount = 1f;

        moveInput = 0f;
        jumpBufferCounter = 0f;
        doubleJumpPressed = false;
        canCutJump = false;
        hasJumped = false;
        isDoubleJumping = false;
        doubleJumpAnimTimer = 0f;
        
        painTimer = 0f;
        invulnerabilityTimer = 1.5f; // Неуязвимость после респавна
        blinkTimer = 0f;
        attackTimer = 0f;
        attackInputPressed = false;

        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 3f; // Установи свой стандартный gravityScale, если он другой!
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
            spriteRenderer.color = baseSpriteColor;
        }
        
        Debug.Log("[Player] Возрожден.");
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}