// PlayerDeathHandler.cs
//By me
using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Player))]
public class PlayerDeathHandler : MonoBehaviour
{
    [Header("Timing")]
    [Tooltip("Скільки секунд нічого не відбувається після смерті")]
    public float freezeBeforeFade = 5f;
    public float fadeOutDuration = 1.5f;
    public float blackHold = 1f;
    public float fadeInDuration = 1.5f;

    [Header("Respawn")]
    public Transform respawnPoint;

    private Vector3 _spawnPosition;
    private bool _dying;

    private void Start()
    {
        _spawnPosition = respawnPoint != null ? respawnPoint.position : transform.position;

        if (ScreenFader.Instance == null)
            Debug.LogWarning("[PlayerDeathHandler] ScreenFader не знайдено. " +
                "Додай об'єкт ScreenFader з Image в Canvas.", this);
    }

    private void Update()
    {
        if (_dying) return;
        var player = GetComponent<Player>();
        if (player != null && player.DeathAnimation)
            StartCoroutine(DeathSequence());
    }

    private IEnumerator DeathSequence()
    {
        _dying = true;

        var player = GetComponent<Player>();
        
        // Чекаємо 5 секунд
        yield return new WaitForSeconds(freezeBeforeFade);

        // Ховаємо персонажа
        var renderers = GetComponentsInChildren<SpriteRenderer>(true);
        foreach (var r in renderers) r.enabled = false;

        // Затемнюємо екран
        if (ScreenFader.Instance != null)
            yield return ScreenFader.Instance.FadeOut(fadeOutDuration);
        else
            yield return new WaitForSeconds(fadeOutDuration);

        yield return new WaitForSeconds(blackHold);

        // Повертаємо на респавн і скидаємо стан
        transform.position = _spawnPosition;
        SceneManager.LoadScene("TestZoneLocation");
        Time.timeScale = 1f;
        if (player != null) 
            player.ResetAfterDeath(); 

        // Показуємо персонажа
        foreach (var r in renderers) r.enabled = true;

        // Плавно проявляємо екран
        if (ScreenFader.Instance != null)
            yield return ScreenFader.Instance.FadeIn(fadeInDuration);
        else
            yield return new WaitForSeconds(fadeInDuration);

        _dying = false;
    }
}