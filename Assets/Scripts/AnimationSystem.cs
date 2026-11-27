// AnimationSystem.cs
// By SuperChris and Scarf/Me
using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Player))]
public class AnimationSystem : MonoBehaviour
{
    [Header("Character Tag")]
    public string characterTag = "Scarf";

    [Header("Resources path, without file extension")]
    public string animationsRoot = "Player";

    [Header("Thresholds")]
    public float walkThreshold = 0.2f;
    [SerializeField] private float runThreshold = 8f;

    [Header("Animation Speed Scaling")]
    [SerializeField] private float walkMinAnimSpeed = 0.5f;
    [SerializeField] private float walkMaxAnimSpeed = 1.6f;
    [SerializeField] private float runMinAnimSpeed = 1.0f;
    [SerializeField] private float runMaxAnimSpeed = 2.0f;

    public string dashStateName = "";
    public float dashAnimationThreshold = 12f;

    [Header("Idle transition")]
    public float idleDelay = 0.2f;

    [Header("Debug")]
    public bool debugAnimation = true;

    private Animator animator;
    private Player player;
    private AnimatorOverrideController overrideController;
    private string currentState = "";

    private AnimationClip idleClip;
    private AnimationClip waitClip;
    private AnimationClip walkClip;
    private AnimationClip runClip;
    private AnimationClip jumpClip;
    private AnimationClip springClip;
    private AnimationClip fallClip;
    private AnimationClip painAirClip;
    private AnimationClip painGroundClip;
    private AnimationClip deathClip;
    private AnimationClip attackClip;

    private float idleTimer;
    private int AcivateWait = 0;

    private readonly Dictionary<string, bool> stateCache = new();
    private readonly HashSet<string> warnedMissingStates = new();

    private string FolderPath => $"{animationsRoot}/{characterTag}";

    [Header("Wait loop")]
    [Tooltip("С какого кадра начинать повтор Wait (0 = с начала)")]
    public int waitLoopStartFrame = 2;
    [Tooltip("FPS, с которым записан клип Wait (для расчёта нормализованного времени)")]
    public float waitClipFps = 24f;

    private void Awake()
    {
        animator = GetComponentInChildren<Animator>();
        player = GetComponent<Player>();
    }

    private void Start()
    {
        if (characterTag != "Scarf")
        {
            if (debugAnimation)
                Debug.Log($"[AnimationSystem] Тег '{characterTag}' не Scarf — анимация отключена.", this);
            enabled = false;
            return;
        }

        if (animator == null)
        {
            Debug.LogError("[AnimationSystem] Не найден Animator в дочерних объектах.", this);
            enabled = false;
            return;
        }

        if (animator.runtimeAnimatorController == null)
        {
            Debug.LogError("[AnimationSystem] На Animator не назначен Animator Controller.", animator);
            enabled = false;
            return;
        }

        LoadClips();
    }

    private void LoadClips()
    {
        idleClip = LoadClip("Idle");
        waitClip = LoadClip("Wait");
        walkClip = LoadClip("Walk");
        runClip  = LoadClip("Run");
        jumpClip = LoadClip("Jump");
        springClip = LoadClip("Spring");
        fallClip = LoadClip("Fall");
        painAirClip = LoadClip("Pain_Air");
        painGroundClip = LoadClip("Pain_Ground");
        deathClip = LoadClip("Death");
        attackClip = LoadClip("Attack");

        overrideController = new AnimatorOverrideController(animator.runtimeAnimatorController);

        ReplaceClip("Idle", idleClip);
        ReplaceClip("Wait", waitClip);
        ReplaceClip("Walk", walkClip);
        ReplaceClip("Run", runClip);
        ReplaceClip("Jump", jumpClip);
        ReplaceClip("Spring", springClip);
        ReplaceClip("Fall", fallClip);
        ReplaceClip("Pain_Air", painAirClip);
        ReplaceClip("Pain_Ground", painGroundClip);
        ReplaceClip("Death", deathClip);
        ReplaceClip("Attack", attackClip);

        animator.runtimeAnimatorController = overrideController;
        animator.speed = 1f;
    }

    private AnimationClip LoadClip(string name)
    {
        string path = $"{FolderPath}/{name}";
        AnimationClip clip = Resources.Load<AnimationClip>(path);

        if (clip == null)
        {
            Debug.LogWarning(
                $"[AnimationSystem] Не найден клип Resources/{path}.anim. " +
                "Проверь путь и регистр букв.", this);
        }

        return clip;
    }

    private void HandleWaitLoop()
    {
        if (waitClip == null) return;
        var stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        if (stateInfo.shortNameHash != Animator.StringToHash("Wait")) return;
        float totalFrames = waitClip.length * waitClipFps;
        if (totalFrames <= 0f) return;
        float startNorm = Mathf.Clamp01(waitLoopStartFrame / totalFrames);
        float t = stateInfo.normalizedTime % 1f;
        if (t >= 1f - (1f / totalFrames))
        {
            animator.Play("Wait", 0, startNorm);
        }
    }

    private void ReplaceClip(string originalClipName, AnimationClip replacement)
    {
        if (replacement == null) return;

        var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
        overrideController.GetOverrides(overrides);

        foreach (var pair in overrides)
        {
            if (pair.Key != null && pair.Key.name == originalClipName)
            {
                overrideController[pair.Key] = replacement;
                return;
            }
        }

        Debug.LogWarning(
            $"[AnimationSystem] Не найден исходный клип '{originalClipName}' " +
            "в Animator Controller.", animator);
    }

    private void LateUpdate()
    {
        if (animator == null || player == null) return;

        string target;
        float speed = player.HorizontalSpeed;
        if (player.DeathAnimation)
        {
            target = "Death";
            AcivateWait = 0;
            idleTimer = 0f;
        }
        else if (player.PainAnimationState > 0)
        {
            target = player.PainIsGrounded ? "Pain_Ground" : "Pain_Air";
            if (!HasState(target))
                target = "Pain_Air";
            AcivateWait = 0;
            idleTimer = 0f;
        }
        else if (player.IsAttacking)
        {
            target = "Attack";
            AcivateWait = 0;
            idleTimer = 0f;
        }
        else
        {
            if (!player.IsGrounded)
            {
                AcivateWait = 0;
                if (player.SpriteDoubleJumping)
                    target = player.VerticalSpeed > 0.01f ? "Jump" : "Fall";
                else
                    target = player.VerticalSpeed > 0.01f ? "Spring" : "Fall";
            }
            else if (player.IsSkidding)
            {
                target = HasState("Skid")
                    ? "Skid"
                    : (speed >= runThreshold ? "Run" : "Walk");
            }
            else if (!string.IsNullOrEmpty(dashStateName) &&
                    speed >= dashAnimationThreshold &&
                    HasState(dashStateName))
            {
                target = dashStateName;
            }
            else if (speed >= runThreshold)
            {
                target = "Run";
                AcivateWait = 0;
            }
            else if (speed > walkThreshold)
            {
                target = "Walk";
                AcivateWait = 0;
            }
            else
            {
                if (currentState == "Walk" && speed <= walkThreshold)
                {
                    idleTimer += Time.deltaTime;
                    target = idleTimer < idleDelay ? "Walk" : "Idle";
                    AcivateWait = 0;
                }
                else
                {
                    idleTimer = 0f;
                    if (AcivateWait < 300)
                    {
                        target = "Idle";
                        AcivateWait++;
                    }
                    else
                    {
                        target = "Wait";
                        AcivateWait = 300;
                    }
                }
            }
        }

        if (!HasState(target))
        {
            if (debugAnimation && warnedMissingStates.Add(target))
            {
                Debug.LogWarning(
                    $"[AnimationSystem] Состояние '{target}' отсутствует " +
                    "в Base Layer Animator Controller.", animator);
            }
            return;
        }

        if (target != currentState)
        {
            animator.Play(target, 0, 0f);
            if (target == "Wait")
            {
                HandleWaitLoop();
            }
            currentState = target;

            if (target == "Idle") idleTimer = 0f;

            if (debugAnimation)
            {
                Debug.Log(
                    $"[AnimationSystem] state={target}, speed={speed:F2}, " +
                    $"grounded={player.IsGrounded}", this);
            }
        }

        if (target == "Walk" || target == "Run")
            animator.speed = CalculateAnimSpeed(speed);
        else
            animator.speed = 1f;
    }

    private float CalculateAnimSpeed(float speed)
    {
        float maxSpeed = Mathf.Max(0.01f, player.maxSpeed);

        if (speed >= runThreshold)
        {
            float t = Mathf.InverseLerp(runThreshold, maxSpeed, speed);
            return Mathf.Lerp(runMinAnimSpeed, runMaxAnimSpeed, t);
        }

        float tw = Mathf.InverseLerp(walkThreshold, runThreshold, speed);
        return Mathf.Lerp(walkMinAnimSpeed, walkMaxAnimSpeed, tw);
    }

    private bool HasState(string stateName)
    {
        if (animator == null) return false;

        if (stateCache.TryGetValue(stateName, out bool cached))
            return cached;

        bool has = animator.HasState(0, Animator.StringToHash(stateName));
        stateCache[stateName] = has;
        return has;
    }
}