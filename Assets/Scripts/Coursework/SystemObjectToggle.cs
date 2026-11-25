using UnityEngine;
using UnityEngine.InputSystem;

namespace Coursework2D
{
    public class SystemObjectToggle : MonoBehaviour
    {
        [Header("Объект, который нужно включать/выключать")]
        [Tooltip("Сюда нужно перетащить CourseworkSystems")]
        public GameObject targetObject;

        [Header("Состояние при старте сцены")]
        [Tooltip("false — скрыт при запуске, включается по нажатию 'ё'")]
        public bool startActive = false;

        [Header("Опционально: ставить игру на паузу при открытии")]
        public bool pauseGameWhenOpen = false;

        private bool isInitialized = false;

        private void Start()
        {
            if (targetObject == null)
            {
                Debug.LogError("[SystemObjectToggle] Не назначен targetObject! Скрипт отключен.", this);
                enabled = false;
                return;
            }

            // Принудительно устанавливаем состояние при старте
            targetObject.SetActive(startActive);
            ApplyPauseState(startActive);
            isInitialized = true;
        }

        private void Update()
        {
            // Защита: если объект почему-то не был инициализирован, не выполняем логику
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
            
            // Включаем/выключаем объект
            targetObject.SetActive(nextState);
            
            // Применяем паузу, если нужно
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
                // Проверяем клавишу 'ё' / '`' / '~'
                if (kb.backquoteKey.wasPressedThisFrame)
                    return true;
            }
            
            // Фолбэк на старый Input Manager (если включен Both)
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.BackQuote) || Input.GetKeyDown(KeyCode.Tilde))
                return true;
#endif

            return false;
        }
    }
}