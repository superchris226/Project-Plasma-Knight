using System;
using UnityEngine;
using UnityEngine.UI;

namespace Coursework2D
{
    // Генерує інтерфейс uGUI безпосередньо з коду під час запуску сцени.
    public class CourseworkUIBootstrap : MonoBehaviour
    {
        [Header("Пов'язані компоненти сцени (якщо порожньо — знайдуться автоматично)")]
        public LevelEditorController editor;
        public LevelPlayabilityChecker checker;
        public ConcurrencyBenchmark benchmark;
        public GameplayStatisticsTracker stats;

        // Елементи інтерфейсу
        private Text status;                // Нижній рядок підказок і повідомлень
        private InputField nameInput;       // Поле вводу назви нового рівня
        private InputField searchInput;     // Поле вводу назви для пошуку
        private InputField difficultyInput; // Поле складності (1..5)
        private Text listText;              // Область виведення списку рівнів

        private void Start()
        {
            if (editor == null) editor = FindFirstObjectByType<LevelEditorController>();
            if (checker == null) checker = FindFirstObjectByType<LevelPlayabilityChecker>();
            if (benchmark == null) benchmark = FindFirstObjectByType<ConcurrencyBenchmark>();
            if (stats == null) stats = FindFirstObjectByType<GameplayStatisticsTracker>();

            BuildUI();

            if (editor != null)
            {
                editor.Message += SetStatus;
                editor.DataChanged += RefreshList;
                RefreshList();
            }

            if (checker != null)
            {
                checker.ResultReceived += r =>
                    SetStatus($"{r.levelName}: {(r.playable ? "прохідний" : "помилка")} — {r.message}");
            }

            if (benchmark != null)
            {
                benchmark.ReportReady += SetStatus;
            }
        }

        private void BuildUI()
        {
            // EventSystem
            if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
                new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.EventSystems.StandaloneInputModule));

            // Canvas
            var canvasObject = new GameObject("CourseworkRuntimeCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);

            // Ліва темна панель
            var panel = MakeRect("Panel", canvas.transform, new Vector2(0, 0), new Vector2(0.48f, 1), Vector2.zero, Vector2.zero);
            panel.gameObject.AddComponent<Image>().color = new Color(0.08f, 0.09f, 0.12f, 0.92f);

            // --- БЛОК 1: Створення та видалення ---
            MakeText(panel, "1. Створення / видалення (назва унікальна, складність 1-5):",
                new Vector2(0.04f, 0.945f), new Vector2(0.96f, 0.985f), 11, Color.gray);
            nameInput = MakeInput(panel, "Назва нового рівня", new Vector2(0.04f, 0.875f), new Vector2(0.58f, 0.94f));
            difficultyInput = MakeInput(panel, "Складність 1-5", new Vector2(0.62f, 0.875f), new Vector2(0.96f, 0.94f));

            MakeButton(panel, "Створити рівень", new Vector2(0.04f, 0.795f), new Vector2(0.48f, 0.86f), CreateLevel);
            MakeButton(panel, "Видалити вибраний", new Vector2(0.52f, 0.795f), new Vector2(0.96f, 0.86f), DeleteSelected);

            // --- БЛОК 2: Збереження та завантаження ---
            MakeButton(panel, "Зберегти JSON", new Vector2(0.04f, 0.715f), new Vector2(0.48f, 0.78f), () => editor?.Save());
            MakeButton(panel, "Завантажити JSON", new Vector2(0.52f, 0.715f), new Vector2(0.96f, 0.78f), () => editor?.Reload());

            // --- БЛОК 3: Пошук та фільтрація ---
            MakeText(panel, "2. Пошук за назвою + фільтр складності:",
                new Vector2(0.04f, 0.665f), new Vector2(0.96f, 0.705f), 11, Color.gray);
            searchInput = MakeInput(panel, "Частина назви для пошуку", new Vector2(0.04f, 0.60f), new Vector2(0.96f, 0.66f));

            MakeButton(panel, "Знайти / Фільтр", new Vector2(0.04f, 0.525f), new Vector2(0.48f, 0.585f), SearchLevels);
            MakeButton(panel, "Сорт. за складністю", new Vector2(0.52f, 0.525f), new Vector2(0.96f, 0.585f), () => ShowLevels(editor?.SortByDifficulty()));
            MakeButton(panel, "Сорт. за об'єктами", new Vector2(0.04f, 0.455f), new Vector2(0.48f, 0.515f), () => ShowLevels(editor?.SortByObjectCount()));
            MakeButton(panel, "Скинути фільтр (Усі)", new Vector2(0.52f, 0.455f), new Vector2(0.96f, 0.515f), RefreshList);

            // --- БЛОК 4: Дії та XML ---
            MakeButton(panel, "Перевірити прохідність", new Vector2(0.04f, 0.38f), new Vector2(0.48f, 0.44f),
                () => { if (editor != null && checker != null) checker.CheckMany(editor.Data.levels); });
            MakeButton(panel, "Тест 10 000+ об'єктів", new Vector2(0.52f, 0.38f), new Vector2(0.96f, 0.44f),
                () => benchmark?.RunBenchmark());

            MakeButton(panel, "Експорт XML", new Vector2(0.04f, 0.31f), new Vector2(0.48f, 0.37f),
                () => { stats?.ExportCurrentStatistics(); SetStatus("XML експортовано."); });
            MakeButton(panel, "Імпорт XML", new Vector2(0.52f, 0.31f), new Vector2(0.96f, 0.37f),
                () => { stats?.ImportStatistics(); SetStatus("XML імпортовано."); });

            // Вибір рівня (кнопки #1..#8)
            for (int i = 0; i < 8; i++)
            {
                int idx = i;
                MakeButton(panel, $"Вибрати #{i + 1}",
                    new Vector2(0.04f + (i % 4) * 0.23f, 0.23f),
                    new Vector2(0.04f + (i % 4) * 0.23f + 0.21f, 0.29f),
                    () => { editor?.Select(idx); RefreshList(); });
            }

            // --- БЛОК 5: Список та прогрес ---
            listText = MakeText(panel, "Список рівнів", new Vector2(0.04f, 0.14f), new Vector2(0.96f, 0.22f), 14, Color.white);
            
            // Прогрес-бар
            var barBg = MakeRect("ProgressBG", panel, new Vector2(0.04f, 0.10f), new Vector2(0.96f, 0.12f), Vector2.zero, Vector2.zero);
            barBg.gameObject.AddComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f, 1f);
            var barFill = MakeRect("ProgressFill", barBg, new Vector2(0, 0), new Vector2(0, 1), Vector2.zero, Vector2.zero);
            var fillImage = barFill.gameObject.AddComponent<Image>();
            fillImage.color = new Color(0.2f, 0.8f, 0.3f, 1f);
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillAmount = 0f;
            if (checker != null) checker.ProgressChanged += p => fillImage.fillAmount = Mathf.Clamp01(p);

            status = MakeText(panel, "Готово", new Vector2(0.04f, 0.02f), new Vector2(0.96f, 0.08f), 12, new Color(0.75f, 0.9f, 1f));

            SetStatus("Підказка: введіть назву та складність (1-5) для створення рівня.");
        }

        private void CreateLevel()
        {
            if (editor == null) return;
            int d = int.TryParse(difficultyInput?.text, out int parsed) ? parsed : 1;
            editor.CreateLevel(nameInput?.text, d, 20, 12);
        }

        private void DeleteSelected()
        {
            if (editor != null) editor.DeleteLevel(editor.SelectedIndex);
        }

        private void SearchLevels()
        {
            if (editor == null) return;
            int diff = int.TryParse(difficultyInput?.text, out int parsed) ? parsed : 0;
            ShowLevels(editor.Search(searchInput?.text, diff));
        }

        private void ShowLevels(System.Collections.Generic.IReadOnlyList<Level> levels)
        {
            if (listText == null) return;
            if (levels == null || levels.Count == 0)
            {
                listText.text = "Рівнів не знайдено.";
                return;
            }

            listText.text = "";
            for (int i = 0; i < levels.Count; i++)
            {
                bool sel = editor != null && ReferenceEquals(levels[i], editor.SelectedLevel);
                listText.text += $"{(sel ? "★ " : "")}{i + 1}. {levels[i].name} | складн. {levels[i].difficulty} | об'єктів {levels[i].objectCount}\n";
            }
        }

        private void RefreshList()
        {
            if (editor != null) ShowLevels(editor.Data.levels);
        }

        private void SetStatus(string message)
        {
            if (status != null) status.text = message;
        }

        private static RectTransform MakeRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var r = go.GetComponent<RectTransform>();
            r.anchorMin = anchorMin;
            r.anchorMax = anchorMax;
            r.offsetMin = offsetMin;
            r.offsetMax = offsetMax;
            return r;
        }

        private static Text MakeText(Transform parent, string text, Vector2 min, Vector2 max, int size, Color color)
        {
            var r = MakeRect("Text", parent, min, max, Vector2.zero, Vector2.zero);
            var t = r.gameObject.AddComponent<Text>();
            t.text = text;
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = size;
            t.color = color;
            t.alignment = TextAnchor.MiddleLeft;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        private static InputField MakeInput(Transform parent, string placeholder, Vector2 min, Vector2 max)
        {
            var r = MakeRect("Input_" + placeholder, parent, min, max, Vector2.zero, Vector2.zero);
            r.gameObject.AddComponent<Image>().color = Color.white;

            var input = r.gameObject.AddComponent<InputField>();
            var text = MakeText(r, "", new Vector2(0.03f, 0.05f), new Vector2(0.97f, 0.95f), 13, Color.black);
            input.textComponent = text;

            var p = MakeText(r, placeholder, new Vector2(0.03f, 0.05f), new Vector2(0.97f, 0.95f), 13, Color.gray);
            input.placeholder = p;
            return input;
        }

        private static void MakeButton(Transform parent, string label, Vector2 min, Vector2 max, Action action)
        {
            var r = MakeRect("Button_" + label, parent, min, max, Vector2.zero, Vector2.zero);
            r.gameObject.AddComponent<Image>().color = new Color(0.15f, 0.35f, 0.55f);

            var b = r.gameObject.AddComponent<Button>();
            MakeText(r, label, new Vector2(0.03f, 0.03f), new Vector2(0.97f, 0.97f), 12, Color.white).alignment = TextAnchor.MiddleCenter;
            b.onClick.AddListener(() => action?.Invoke());
        }
    }
}