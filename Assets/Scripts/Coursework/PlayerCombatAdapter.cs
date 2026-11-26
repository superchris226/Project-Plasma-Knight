// PlayerCombatAdapter.cs
// AI-ASSISTED: ChatGPT (GPT-4), 2024-11-21.
// Призначення: адаптер між доменним PlayerCharacter (Combatant) і
// Unity-компонентом Player. Синхронізує здоров'я та надає урон атаки
// для AttackHitbox, зберігаючи всю бізнес-логіку в Combatant.
using UnityEngine;
using Coursework2D;

[RequireComponent(typeof(Player))]
public class PlayerCombatAdapter : MonoBehaviour
{
    [Header("Domain model")]
    [SerializeField] private int baseMaxHealth = 100;
    [SerializeField] private int baseAttackPower = 10;
    [SerializeField] private int damagePerLevel = 5;

    public PlayerCharacter Character { get; private set; }
    private Player player;
    private PlayerLevel playerLevel;
    private int lastLevel;

    private void Awake()
    {
        player = GetComponent<Player>();
        playerLevel = GetComponent<PlayerLevel>();
    }

    private void Start()
    {
        int max = player != null && player.maxHealth > 0 ? player.maxHealth : baseMaxHealth;
        Character = new PlayerCharacter("Scarf", max, baseAttackPower);

        if (playerLevel != null)
        {
            lastLevel = playerLevel.level;
            playerLevel.OnLevelUp += OnLevelUp;
            ApplyLevelScaling();
        }
    }

    private void OnDestroy()
    {
        if (playerLevel != null) playerLevel.OnLevelUp -= OnLevelUp;
    }

    private void OnLevelUp(int newLevel)
    {
        lastLevel = newLevel;
        ApplyLevelScaling();
        FileLogger.Write($"[Combat] Player level up -> {newLevel}, attack={Character.AttackPower}");
    }

    private void ApplyLevelScaling()
    {
        int newAttack = baseAttackPower + (lastLevel - 1) * damagePerLevel;
        var oldHealth = Character?.Health ?? player?.health ?? 0;
        Character = new PlayerCharacter("Scarf",
            Character?.MaxHealth ?? (player?.maxHealth ?? baseMaxHealth),
            newAttack);
        if (oldHealth > 0 && oldHealth < Character.MaxHealth)
            Character.TakeDamage(Character.MaxHealth - oldHealth);
    }

    public void SyncFromPlayer()
    {
        if (player == null || Character == null) return;
        if (player.health < Character.Health)
            Character.TakeDamage(Character.Health - player.health);
        else if (player.health > Character.Health)
            Character.Heal(player.health - Character.Health);
    }

    public int ComputeAttackDamage(int distanceToTarget)
    {
        SyncFromPlayer();
        return Character?.Attack(new AttackContext(distanceToTarget, true)) ?? 0;
    }
}