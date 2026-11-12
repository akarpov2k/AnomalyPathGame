#nullable enable
using System;
using System.IO;
using AnomalyPath.Core;
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Core.Quests;
using AnomalyPath.Runtime.Screens.Quests;
using AnomalyPath.Runtime.Ui.Art;
using AnomalyPath.Runtime.Ui.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace AnomalyPath.Editor.Ui
{
    /// <summary>
    /// Снимает кадры доски объявлений и журнала заданий из собранных префабов: карточки слотов,
    /// пустая карточка с таймером, категории журнала, список и описание выбранного задания.
    /// Кадр собирается из самого префаба, поэтому показывает реальную вёрстку.
    /// </summary>
    public static class QuestScreensScreenshot
    {
        private const string BoardPrefabPath = "Assets/Prefabs/Screens/QuestBoardScreen.prefab";
        private const string JournalPrefabPath = "Assets/Prefabs/Screens/QuestJournalScreen.prefab";
        private const string OutputFolder = "Logs/Screenshots";
        private const string DataRoot = "Assets/Data";
        private const int Width = 1920;
        private const int Height = 1080;

        /// <summary>Пункт меню: снять кадры доски и журнала заданий.</summary>
        [MenuItem("Tools/AnomalyPath/UI/Скриншоты доски и журнала заданий")]
        public static void CaptureFromMenu() => CaptureAll();

        /// <summary>Точка входа batch-режима.</summary>
        public static void CaptureFromBatch()
        {
            try
            {
                CaptureAll();
                EditorApplication.Exit(0);
            }
            catch (Exception error)
            {
                Debug.LogException(error);
                EditorApplication.Exit(1);
            }
        }

        /// <summary>Снимает кадры обоих экранов.</summary>
        public static void CaptureAll()
        {
            Directory.CreateDirectory(OutputFolder);
            var result = Preflight.Run(DataRoot);
            if (!result.CanStart || result.Services == null)
            {
                throw new InvalidOperationException(result.Report.Summary);
            }

            var services = result.Services;
            var texts = services.Resolve<IGameTexts>();
            var locations = services.Resolve<ILocationRepository>();
            Capture(
                "quest-board",
                BoardPrefabPath,
                panel => FillBoard(panel, texts, services.Resolve<IQuestRepository>(), locations));
            Capture("quest-journal", JournalPrefabPath, panel => FillJournal(
                panel,
                texts,
                services.Resolve<IQuestRepository>()));
            Debug.Log($"AnomalyPath: кадры доски и журнала заданий в {OutputFolder}");
        }

        private static void Capture(string fileName, string prefabPath, Action<GameObject> fill)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                throw new InvalidOperationException($"Нет префаба {prefabPath}");
            }

            var cameraObject = new GameObject("CaptureCamera", typeof(Camera));
            var camera = cameraObject.GetComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = Height / 2f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 100f;

            var canvasObject = new GameObject("CaptureCanvas", typeof(Canvas));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var canvasRect = (RectTransform)canvasObject.transform;
            canvasRect.sizeDelta = new Vector2(Width, Height);
            canvasRect.position = Vector3.zero;

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, canvasObject.transform);
            instance.SetActive(true);
            ScreenBackdropAssets.ApplyForScreenshot(instance);
            fill(instance);
            Canvas.ForceUpdateCanvases();
            SyncScrollbars(instance);
            Canvas.ForceUpdateCanvases();
            ActionButtonLabel.FitAll(instance.transform);
            Canvas.ForceUpdateCanvases();

            var target = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32);
            target.Create();
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            var texture = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0f, 0f, Width, Height), 0, 0);
            texture.Apply();
            RenderTexture.active = null;
            camera.targetTexture = null;

            var path = Path.Combine(OutputFolder, fileName + ".png");
            File.WriteAllBytes(path, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            target.Release();
            UnityEngine.Object.DestroyImmediate(target);
            UnityEngine.Object.DestroyImmediate(canvasObject);
            UnityEngine.Object.DestroyImmediate(cameraObject);
            Debug.Log($"AnomalyPath: скриншот {path}");
        }

        /// <summary>Прячет полосу прокрутки, когда строки помещаются: в batch-режиме ScrollRect не обновляет её сам.</summary>
        private static void SyncScrollbars(GameObject panel)
        {
            foreach (var scroll in panel.GetComponentsInChildren<ScrollRect>(true))
            {
                if (scroll.content == null || scroll.viewport == null)
                {
                    continue;
                }

                LayoutRebuilder.ForceRebuildLayoutImmediate(scroll.content);
                var fits = scroll.content.rect.height <= scroll.viewport.rect.height + 0.5f;
                if (scroll.verticalScrollbar != null)
                {
                    scroll.verticalScrollbar.gameObject.SetActive(!fits);
                }
            }
        }

        private static void FillBoard(
            GameObject panel,
            IGameTexts texts,
            IQuestRepository quests,
            ILocationRepository locations)
        {
            ApplyTexts(panel, texts);
            var content = panel.transform.Find("Content/Window/Slots/Viewport/Content");
            var template = content?.Find("CardTemplate")?.GetComponent<QuestBoardCard>();
            if (content == null || template == null)
            {
                throw new InvalidOperationException("В префабе доски нет Content/Window/Slots/Viewport/Content/CardTemplate");
            }

            var catalog = LoadCatalog();
            var shown = 0;
            foreach (var templateQuest in quests.All)
            {
                if (shown >= 5)
                {
                    break;
                }

                var card = UnityEngine.Object.Instantiate(template, content);
                card.gameObject.name = "Board_" + shown;
                card.gameObject.SetActive(true);
                card.ShowOffer(
                    QuestPalette.Row,
                    Resolve(catalog, templateQuest),
                    Difficulty(texts, shown),
                    texts.Get(templateQuest.TitleKey),
                    texts.Get(templateQuest.DescriptionKey),
                    Progress(texts, 0, 1),
                    () => { },
                    () => { });
                shown++;
            }

            var timerCard = UnityEngine.Object.Instantiate(template, content);
            timerCard.gameObject.name = "Board_" + shown;
            timerCard.gameObject.SetActive(true);
            timerCard.ShowTimer(
                QuestPalette.Row,
                Difficulty(texts, shown),
                string.Format(texts.Get("ui.quest_refresh_in"), 12));

            var title = panel.transform.Find("Content/Window/Title")?.GetComponent<Text>();
            if (title != null && locations.All.Count > 0)
            {
                title.text = string.Format(
                    texts.Get("ui.quest_board_title"),
                    texts.Get(locations.All[0].TitleKey));
            }
        }

        private static void FillJournal(
            GameObject panel,
            IGameTexts texts,
            IQuestRepository quests)
        {
            ApplyTexts(panel, texts);
            SelectTab(panel, "RepeatableTab");
            var content = panel.transform.Find("Content/Window/ListPanel/List/Viewport/Content");
            var template = content?.Find("RowTemplate")?.GetComponent<QuestJournalRow>();
            if (content == null || template == null)
            {
                throw new InvalidOperationException("В префабе журнала нет Content/Window/ListPanel/List/Viewport/Content/RowTemplate");
            }

            var rows = 0;
            foreach (var quest in quests.All)
            {
                if (rows >= 4)
                {
                    break;
                }

                AddRow(
                    texts,
                    content,
                    template,
                    quest.TitleKey,
                    Progress(texts, 0, 1),
                    texts.Get("ui.quest_fail_death"),
                    rows == 0);
                rows++;
            }

            var detailsTitle = panel.transform.Find("Content/Window/DetailsPanel/DetailsTitle")?.GetComponent<Text>();
            var detailsKind = panel.transform.Find("Content/Window/DetailsPanel/DetailsKind")?.GetComponent<Text>();
            var detailsDescription = panel.transform.Find("Content/Window/DetailsPanel/DetailsDescription")?.GetComponent<Text>();
            var detailsProgress = panel.transform.Find("Content/Window/DetailsPanel/DetailsProgress")?.GetComponent<Text>();
            var detailsConditions = panel.transform.Find("Content/Window/DetailsPanel/DetailsConditions")?.GetComponent<Text>();
            var detailsTargets = panel.transform.Find("Content/Window/DetailsPanel/DetailsTargets")?.GetComponent<Text>();
            var detailsReward = panel.transform.Find("Content/Window/DetailsPanel/DetailsReward")?.GetComponent<Text>();
            var actionLabel = panel.transform.Find("Content/Window/JournalAction/Label")?.GetComponent<Text>();
            if (quests.All.Count > 0)
            {
                var first = quests.All[0];
                Set(detailsTitle, texts.Get(first.TitleKey));
                Set(detailsKind, texts.Get(TypeKey(first.Type)));
                Set(detailsDescription, texts.Get(first.DescriptionKey));
                Set(detailsProgress, Progress(texts, 0, 1));
                Set(detailsConditions, texts.Get("ui.quest_fail_death"));
                Set(detailsTargets, string.Empty);
                Set(detailsReward, string.Format(texts.Get("ui.quest_reward"), first.Rewards.Money));
            }

            Set(actionLabel, texts.Get("ui.quest_cancel"));
        }

        /// <summary>Ключ подписи типа цели для кадра.</summary>
        private static string TypeKey(QuestType type) => type switch
        {
            QuestType.ClearLocation => "ui.quest_type_clear_location",
            QuestType.KillMutants => "ui.quest_type_kill_mutants",
            QuestType.GatherArtifacts => "ui.quest_type_gather_artifacts",
            _ => "ui.quest_type_find_items"
        };

        private static void SelectTab(GameObject panel, string name)
        {
            foreach (var tabName in new[] { "StoryTab", "SideTab", "RepeatableTab" })
            {
                var tab = panel.transform.Find("Content/Window/ListPanel/Tabs/" + tabName)?.GetComponent<Image>();
                if (tab != null)
                {
                    tab.color = tabName == name ? QuestPalette.TabActive : QuestPalette.Tab;
                }
            }
        }

        private static void AddRow(
            IGameTexts texts,
            Transform content,
            QuestJournalRow template,
            string titleKey,
            string progress,
            string time,
            bool selected)
        {
            var row = UnityEngine.Object.Instantiate(template, content);
            row.gameObject.name = "QuestRow_" + titleKey;
            row.gameObject.SetActive(true);
            row.Bind(titleKey, texts.Get(titleKey), progress, time, ready: false, selected, () => { });
        }

        private static string Difficulty(IGameTexts texts, int index)
        {
            var key = index switch
            {
                0 => "ui.quest_difficulty_easy",
                1 => "ui.quest_difficulty_medium",
                _ => "ui.quest_difficulty_hard"
            };
            return string.Format(texts.Get("ui.quest_difficulty"), texts.Get(key));
        }

        private static string Progress(IGameTexts texts, int current, int required) =>
            string.Format(texts.Get("ui.quest_progress"), current, required);

        private static SpriteCatalog? LoadCatalog() =>
            AssetDatabase.LoadAssetAtPath<SpriteCatalog>(AnomalyPath.Editor.Art.SpriteCatalogBuilder.CatalogPath);

        private static Sprite? Resolve(SpriteCatalog? catalog, QuestTemplate template)
        {
            if (catalog == null)
            {
                return null;
            }

            if (!string.IsNullOrWhiteSpace(template.Art) && catalog.TryGet(template.Art!, out var art) && art != null)
            {
                return art;
            }

            return catalog.TryGet(QuestArt.IdFor(template.Type), out var fallback) ? fallback : null;
        }

        private static void ApplyTexts(GameObject panel, IGameTexts texts)
        {
            foreach (var label in panel.GetComponentsInChildren<LocalizedText>(true))
            {
                label.Apply(texts);
            }
        }

        private static void Set(Text? label, string text)
        {
            if (label != null)
            {
                label.text = text;
            }
        }
    }
}
