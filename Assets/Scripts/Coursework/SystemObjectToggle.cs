using UnityEngine;
using UnityEngine.InputSystem;

namespace Coursework2D
{
    public class SystemObjectToggle : MonoBehaviour
    {
        [Header("Об'єкт, який потрібно вмикати/вимикати")]
        [Tooltip("Сюди потрібно перетягнути CourseworkSystems")]
        public GameObject targetObject;

        [Header("Стан при старті сцени")]
        [Tooltip("false — прихований при запуску, вмикається за натисканням 'ё'")]
        public bool startActive = false;

        [Header("Опційно: ставити гру на паузу при відкритті")]
        public bool pauseGameWhenOpen = false;

        private bool isInitialized = false;

        private void Start()
        {
            if (targetObject == null)
            {
                Debug.LogError("[SystemObjectToggle] Не призначено targetObject! Скрипт вимкнено.", this);
                enabled = false;
                return;
            }

            // Примусово встановлюємо стан при старті
            targetObject.SetActive(startActive);
            ApplyPauseState(startActive);
            isInitialized = true;
        }

        private void Update()
        {
            // Захист: якщо об'єкт чомусь не був ініціалізований, не виконуємо логіку
            if (!isInitialized) return;

            if (WasToggleKeyPressed())
            {
                Toggle();
            }
        }

        public void Toggle()
        {
            if (targetObject == null) return;

            bool nextState = !targetObject.activeSelf;
            
            // Вмикаємо/вимикаємо об'єкт
            targetObject.SetActive(nextState);
            
            // Застосовуємо паузу, якщо потрібно
            ApplyPauseState(nextState);
        }

        private void ApplyPauseState(bool isOpen)
        {
            if (!pauseGameWhenOpen) return;
            Time.timeScale = isOpen ? 0f : 1f;
        }

        private static bool WasToggleKeyPressed()
        {
            var kb = Keyboard.current;
            if (kb != null)
            {
                // Перевіряємо клавішу 'ё' / '`' / '~'
                if (kb.backquoteKey.wasPressedThisFrame)
                    return true;
            }
            
            // Фолбек на старий Input Manager (якщо увімкнено Both)
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.BackQuote) || Input.GetKeyDown(KeyCode.Tilde))
                return true;
#endif

            return false;
        }
    }
}