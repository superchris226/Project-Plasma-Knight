// ШІ: ChatGPT – Створив CourseworkUIBootstrap.
// Змінено: фікс кода, зробив цикл, виправив проблему з 10000 об'єктів.
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace Coursework2D
{
    public class CourseworkUIBootstrap : MonoBehaviour
    {
        [Header("Пов'язані компоненти сцени (якщо порожньо — знайдуться автоматично)")]
        public LevelEditorController editor;
        public LevelPlayabilityChecker checker;
        public ConcurrencyBenchmark benchmark;
        public GameplayStatisticsTracker stats;

        private Text status;
        private InputField nameInput;
        private InputField searchInput;
        private InputField difficultyInput;

        private Dropdown objectTypeDropdown;
        private InputField objXInput;
        private InputField objYInput;
        private InputField objIdInput;
        private InputField jsonPathInput;
        private Text selectIndexLabel;
        private Text listText;

        private int _pageIndex = 0;
        private const int PageSize = 10;

        private void Start()
        {
            FileLogger.CaptureMainThreadPath();

            if (editor == null) editor = FindFirstObjectByType<LevelEditorController>();
            if (checker == null) checker = FindFirstObjectByType<LevelPlayabilityChecker>();
            if (benchmark == null) benchmark = FindFirstObjectByType<ConcurrencyBenchmark>();
            if (stats == null) stats = FindFirstObjectByType<GameplayStatisticsTracker>();

            BuildUI();

            if (jsonPathInput != null && string.IsNullOrWhiteSpace(jsonPathInput.text))
            {
                jsonPathInput.text = Path.Combine(Application.persistentDataPath, "levels_custom.json");
            }

            if (editor != null)
            {
                editor.Message += SetStatus;
                editor.DataChanged += RefreshAll;
                RefreshAll();
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
            if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
                new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.EventSystems.StandaloneInputModule));

            var canvasObject = new GameObject("CourseworkRuntimeCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);

            var panel = MakeRect("Panel", canvas.transform, new Vector2(0, 0), new Vector2(0.54f, 1), Vector2.zero, Vector2.zero);
            panel.gameObject.AddComponent<Image>().color = new Color(0.08f, 0.09f, 0.12f, 0.94f);

            nameInput = MakeInput(panel, "Назва рівня", new Vector2(0.03f, 0.92f), new Vector2(0.55f, 0.97f));
            difficultyInput = MakeInput(panel, "Складн. 1-5", new Vector2(0.57f, 0.92f), new Vector2(0.75f, 0.97f));
            MakeButton(panel, "Створити", new Vector2(0.77f, 0.92f), new Vector2(0.97f, 0.97f), CreateLevel);

            MakeButton(panel, "Оновити рівень", new Vector2(0.03f, 0.86f), new Vector2(0.48f, 0.91f), UpdateSelectedLevel);
            MakeButton(panel, "Видалити рівень", new Vector2(0.52f, 0.86f), new Vector2(0.97f, 0.91f), DeleteSelected);

            objectTypeDropdown = MakeDropdown(panel, Enum.GetNames(typeof(LevelObjectType)), new Vector2(0.03f, 0.80f), new Vector2(0.28f, 0.85f));
            objXInput = MakeInput(panel, "X", new Vector2(0.30f, 0.80f), new Vector2(0.39f, 0.85f));
            objYInput = MakeInput(panel, "Y", new Vector2(0.40f, 0.80f), new Vector2(0.49f, 0.85f));
            MakeButton(panel, "+ Об'єкт", new Vector2(0.51f, 0.80f), new Vector2(0.73f, 0.85f), AddObjectToSelected);

            objIdInput = MakeInput(panel, "ID об'єкта", new Vector2(0.03f, 0.74f), new Vector2(0.73f, 0.79f));
            MakeButton(panel, "- Об'єкт", new Vector2(0.75f, 0.74f), new Vector2(0.97f, 0.85f), RemoveObjectFromSelected);

            MakeButton(panel, "Зберегти JSON", new Vector2(0.03f, 0.68f), new Vector2(0.48f, 0.73f), () => editor?.Save());
            MakeButton(panel, "Перезавантажити", new Vector2(0.52f, 0.68f), new Vector2(0.97f, 0.73f), () => editor?.Reload());

            jsonPathInput = MakeInput(panel, "Шлях до JSON файлу", new Vector2(0.03f, 0.62f), new Vector2(0.55f, 0.67f));
            MakeButton(panel, "Імпорт JSON", new Vector2(0.57f, 0.62f), new Vector2(0.76f, 0.67f), ImportJsonFromField);
            MakeButton(panel, "Експорт JSON", new Vector2(0.78f, 0.62f), new Vector2(0.97f, 0.67f), ExportJsonToField);

            searchInput = MakeInput(panel, "Пошук за назвою", new Vector2(0.03f, 0.56f), new Vector2(0.55f, 0.61f));
            MakeButton(panel, "Знайти", new Vector2(0.57f, 0.56f), new Vector2(0.76f, 0.61f), SearchLevels);
            MakeButton(panel, "Усі рівні", new Vector2(0.78f, 0.56f), new Vector2(0.97f, 0.61f), RefreshAll);

            MakeButton(panel, "Сорт. складність", new Vector2(0.03f, 0.50f), new Vector2(0.48f, 0.55f), () => ShowLevels(editor?.SortByDifficulty()));
            MakeButton(panel, "Сорт. об'єкти", new Vector2(0.52f, 0.50f), new Vector2(0.97f, 0.55f), () => ShowLevels(editor?.SortByObjectCount()));

            MakeButton(panel, "Перевірити прохідність", new Vector2(0.03f, 0.44f), new Vector2(0.48f, 0.49f),
                () => { if (editor != null && checker != null) checker.CheckMany(editor.Data.levels); });
            MakeButton(panel, "Тест продуктивності", new Vector2(0.52f, 0.44f), new Vector2(0.97f, 0.49f),
                () => benchmark?.RunBenchmark());

            MakeButton(panel, "Експорт XML", new Vector2(0.03f, 0.38f), new Vector2(0.48f, 0.43f), ExportXml);
            MakeButton(panel, "Імпорт XML", new Vector2(0.52f, 0.38f), new Vector2(0.97f, 0.43f), ImportXml);

            MakeButton(panel, "< Сторінка", new Vector2(0.03f, 0.325f), new Vector2(0.24f, 0.37f), () => { _pageIndex = Mathf.Max(0, _pageIndex - 1); RefreshSelectButtons(); });
            selectIndexLabel = MakeText(panel, "Сторінка 1", new Vector2(0.26f, 0.325f), new Vector2(0.74f, 0.37f), 12, Color.yellow);
            selectIndexLabel.alignment = TextAnchor.MiddleCenter;
            MakeButton(panel, "Сторінка >", new Vector2(0.76f, 0.325f), new Vector2(0.97f, 0.37f), () => { _pageIndex++; RefreshSelectButtons(); });

            for (int i = 0; i < PageSize; i++)
            {
                int slot = i;
                MakeButton(panel, $"#{i + 1}",
                    new Vector2(0.03f + (i % 5) * 0.186f, i < 5 ? 0.275f : 0.225f),
                    new Vector2(0.03f + (i % 5) * 0.186f + 0.174f, i < 5 ? 0.32f : 0.27f),
                    () =>
                    {
                        int targetIdx = _pageIndex * PageSize + slot;
                        editor?.Select(targetIdx);
                        RefreshAll();
                    });
            }

            listText = MakeText(panel, "Список рівнів", new Vector2(0.03f, 0.10f), new Vector2(0.97f, 0.215f), 12, Color.white);
            listText.verticalOverflow = VerticalWrapMode.Truncate;

            var barBg = MakeRect("ProgressBG", panel, new Vector2(0.03f, 0.075f), new Vector2(0.97f, 0.092f), Vector2.zero, Vector2.zero);
            barBg.gameObject.AddComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f, 1f);
            var barFill = MakeRect("ProgressFill", barBg, new Vector2(0, 0), new Vector2(0, 1), Vector2.zero, Vector2.zero);
            var fillImage = barFill.gameObject.AddComponent<Image>();
            fillImage.color = new Color(0.2f, 0.8f, 0.3f, 1f);
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillAmount = 0f;
            if (checker != null) checker.ProgressChanged += p => fillImage.fillAmount = Mathf.Clamp01(p);

            status = MakeText(panel, "Готово", new Vector2(0.03f, 0.01f), new Vector2(0.97f, 0.07f), 11, new Color(0.75f, 0.9f, 1f));
        }

        private void RefreshSelectButtons()
        {
            int total = editor?.Data?.levels?.Count ?? 0;
            int maxPage = Mathf.Max(0, (total - 1) / PageSize);
            _pageIndex = Mathf.Clamp(_pageIndex, 0, maxPage);
            if (selectIndexLabel != null)
                selectIndexLabel.text = $"Рівні {_pageIndex * PageSize + 1}..{Mathf.Min(total, (_pageIndex + 1) * PageSize)} (всього {total})";
        }

        private void CreateLevel()
        {
            if (editor == null) return;
            int d = int.TryParse(difficultyInput?.text, out int parsed) ? parsed : 1;
            editor.CreateLevel(nameInput?.text, d, 20, 12);
        }

        private void UpdateSelectedLevel()
        {
            if (editor == null) return;
            int idx = editor.SelectedIndex;
            var current = editor.SelectedLevel;
            if (current == null)
            {
                SetStatus("Рівень не вибрано.");
                return;
            }

            string newName = string.IsNullOrWhiteSpace(nameInput?.text) ? current.name : nameInput.text;
            int newDiff = int.TryParse(difficultyInput?.text, out int d) ? d : current.difficulty;

            var copy = new Level
            {
                name = newName,
                difficulty = newDiff,
                gridWidth = current.gridWidth,
                gridHeight = current.gridHeight,
                start = current.start != null ? new GridPoint(current.start.x, current.start.y) : new GridPoint(0, 0),
                finish = current.finish != null ? new GridPoint(current.finish.x, current.finish.y) : new GridPoint(current.gridWidth - 1, current.gridHeight - 1),
                objects = current.objects != null ? new List<LevelObject>(current.objects) : new List<LevelObject>(),
                enemies = current.enemies != null ? new List<EnemyData>(current.enemies) : new List<EnemyData>()
            };

            editor.UpdateLevel(idx, copy);
        }

        private void DeleteSelected()
        {
            if (editor != null) editor.DeleteLevel(editor.SelectedIndex);
        }

        private void AddObjectToSelected()
        {
            if (editor == null) return;
            int idx = editor.SelectedIndex;
            if (idx < 0 || idx >= editor.Data.levels.Count)
            {
                SetStatus("Рівень не вибрано.");
                return;
            }

            if (!int.TryParse(objXInput?.text, out int x) || !int.TryParse(objYInput?.text, out int y))
            {
                SetStatus("Введіть коректні цілі координати X та Y.");
                return;
            }

            var type = (LevelObjectType)(objectTypeDropdown?.value ?? 0);
            var obj = new LevelObject(type, new Vector2Int(x, y));
            if (editor.AddObject(idx, obj) && objIdInput != null)
            {
                objIdInput.text = obj.id;
            }
        }

        private void RemoveObjectFromSelected()
        {
            if (editor == null) return;
            int idx = editor.SelectedIndex;
            if (idx < 0 || idx >= editor.Data.levels.Count)
            {
                SetStatus("Рівень не вибрано.");
                return;
            }

            string targetId = objIdInput?.text?.Trim();
            if (string.IsNullOrEmpty(targetId))
            {
                var last = editor.Data.levels[idx].objects;
                if (last != null && last.Count > 0)
                    targetId = last[last.Count - 1].id;
            }

            if (string.IsNullOrEmpty(targetId))
            {
                SetStatus("Вкажіть ID об'єкта або додайте об'єкт.");
                return;
            }

            editor.RemoveObject(idx, targetId);
        }

        private void ImportJsonFromField()
        {
            if (editor == null) return;
            string path = jsonPathInput?.text?.Trim();
            if (string.IsNullOrEmpty(path))
            {
                SetStatus("Вкажіть шлях до JSON для імпорту.");
                return;
            }
            editor.ImportJson(path);
        }

        private void ExportJsonToField()
        {
            if (editor == null) return;
            string path = jsonPathInput?.text?.Trim();
            if (string.IsNullOrEmpty(path))
            {
                SetStatus("Вкажіть шлях до JSON для експорту.");
                return;
            }
            editor.ExportJson(path);
        }

        private void ExportXml()
        {
            if (stats == null)
            {
                SetStatus("Помилка: GameplayStatisticsTracker відсутній.");
                return;
            }
            if (stats.ExportCurrentStatistics())
                SetStatus("XML експортовано.");
            else
                SetStatus("Помилка експорту XML (деталі в coursework.log).");
        }

        private void ImportXml()
        {
            if (stats == null)
            {
                SetStatus("Помилка: GameplayStatisticsTracker відсутній.");
                return;
            }
            if (stats.ImportStatistics())
                SetStatus("XML імпортовано.");
            else
                SetStatus("Помилка імпорту XML (деталі в coursework.log).");
        }

        private void SearchLevels()
        {
            if (editor == null) return;
            int diff = int.TryParse(difficultyInput?.text, out int parsed) ? parsed : 0;
            ShowLevels(editor.Search(searchInput?.text, diff));
        }

        private void ShowLevels(IReadOnlyList<Level> levels)
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
                var lvl = levels[i];
                listText.text += $"{(sel ? "★ " : "")}{i + 1}. {lvl.name} | скл.{lvl.difficulty} | об'єктів:{lvl.objectCount} | ({lvl.start?.x},{lvl.start?.y})->({lvl.finish?.x},{lvl.finish?.y})\n";
            }
        }

        private void RefreshAll()
        {
            RefreshSelectButtons();
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
            var text = MakeText(r, "", new Vector2(0.03f, 0.05f), new Vector2(0.97f, 0.95f), 12, Color.black);
            input.textComponent = text;

            var p = MakeText(r, placeholder, new Vector2(0.03f, 0.05f), new Vector2(0.97f, 0.95f), 12, Color.gray);
            input.placeholder = p;
            return input;
        }

        private static Dropdown MakeDropdown(Transform parent, string[] options, Vector2 min, Vector2 max)
        {
            var r = MakeRect("Dropdown", parent, min, max, Vector2.zero, Vector2.zero);
            r.gameObject.AddComponent<Image>().color = Color.white;
            var dd = r.gameObject.AddComponent<Dropdown>();
            var label = MakeText(r, options.Length > 0 ? options[0] : "", new Vector2(0.05f, 0.05f), new Vector2(0.95f, 0.95f), 11, Color.black);
            dd.captionText = label;
            dd.ClearOptions();
            dd.AddOptions(new List<string>(options));
            return dd;
        }

        private static void MakeButton(Transform parent, string label, Vector2 min, Vector2 max, Action action)
        {
            var r = MakeRect("Button_" + label, parent, min, max, Vector2.zero, Vector2.zero);
            r.gameObject.AddComponent<Image>().color = new Color(0.15f, 0.35f, 0.55f);

            var b = r.gameObject.AddComponent<Button>();
            MakeText(r, label, new Vector2(0.02f, 0.02f), new Vector2(0.98f, 0.98f), 11, Color.white).alignment = TextAnchor.MiddleCenter;
            b.onClick.AddListener(() => action?.Invoke());
        }
    }
}