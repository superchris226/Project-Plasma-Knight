// ШІ: ChatGPT – Створив PlayerCombatAdapter.
// Змінено: фікс кода, адаптер між доменним PlayerCharacter (Combatant) і Unity-компонентом Player. Синхронізує здоров'я та надає урон
// атаки для AttackHitbox, зберігаючи всю бізнес-логіку в Combatant. тепер адаптер бере готовий компонент PlayerCharacter з об'єкта, а не створює його через new.
using UnityEngine;
using Coursework2D;

[RequireComponent(typeof(Player))]
[RequireComponent(typeof(PlayerCharacter))] // Вимагаємо, щоб цей компонент був на об'єкті
public class PlayerCombatAdapter : MonoBehaviour
{
    [Header("Domain model")]
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
        
        // Беремо компонент з об'єкта (він додається в інспекторі)
        Character = GetComponent<PlayerCharacter>(); 
    }

    private void Start()
    {
        if (player != null && Character != null)
        {
            // Синхронізуємо здоров'я на старті
            Character.maxHealth = player.maxHealth;
            Character.currentHealth = player.health;
        }

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
    }

    private void ApplyLevelScaling()
    {
        if (Character != null)
        {
            // Оновлюємо силу атаки в компоненті
            Character.attackPower = baseAttackPower + (lastLevel - 1) * damagePerLevel;
        }
    }

    public void SyncFromPlayer()
    {
        if (player == null || Character == null) return;
        
        // Синхронізуємо HP
        Character.currentHealth = player.health;
        Character.maxHealth = player.maxHealth;
    }

    public int ComputeAttackDamage(int distanceToTarget)
    {
        SyncFromPlayer();
        if (Character == null) return 0;
        return Character.Attack(new AttackContext(distanceToTarget, true));
    }
}