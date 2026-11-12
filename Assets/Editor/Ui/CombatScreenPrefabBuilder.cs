#nullable enable
using System;
using AnomalyPath.Editor.Art;
using AnomalyPath.Runtime.Screens.Combat;
using AnomalyPath.Runtime.Ui.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace AnomalyPath.Editor.Ui
{
    /// <summary>
    /// Собирает префаб боевого экрана по прототипу <c>.prototypes/бой.penpot</c>:
    /// поле с фигурами, полосы здоровья с очками действия, оружие с патронами,
    /// форма действий с выпадашкой скиллов, быстрые слоты, конец хода и побег, полоса опыта
    /// и журнал действий. Префаб назначается на <c>GameScreen</c> в сцене Main.
    /// </summary>
    public static partial class CombatScreenPrefabBuilder
    {
        private const string PrefabPath = "Assets/Prefabs/Screens/CombatScreen.prefab";
        private const string ScreensFolder = "Assets/Prefabs/Screens";
        private const string ScenePath = "Assets/Scenes/Main.unity";
        private const string ActionFramePath = "Assets/Art/Buttons/ui_btn_mp_ind_empty.png";
        private const string ActionFrameGreenPath = "Assets/Art/Buttons/ui_btn_mp_ind_green.png";
        private const string ApCirclePath = "Assets/Art/Icons/ui_ap_circle.png";
        private const string BarFillPath = "Assets/Art/Icons/ui_bar_fill.png";
        private const string MuzzleFlashPath = "Assets/Art/Icons/fx_muzzle_flash.png";
        private const float Edge = 66f;
        private const float FieldTop = 112f;
        private const float FieldBottom = 384f;
        private const float FigureFeet = 0.3305f;

        /// <summary>Горизонталь фигуры игрока в долях поля: прежняя средняя зона.</summary>
        private const float PlayerFigureX = 0.2275f;

        /// <summary>Горизонталь фигуры противника в долях поля: прежняя средняя зона.</summary>
        private const float EnemyFigureX = 0.792f;

        private const float PlateWidth = 400f;
        private const float PlateHeight = 120f;
        private const float PlateInset = 24f;
        private const float PlatePadding = 8f;
        private const float HealthBarTop = 34f;
        private const float HealthBarHeight = 26f;
        private const float ApRowTop = 64f;
        private const float ApRowHeight = 24f;
        private const float ApCircle = 16f;
        private const float FormBottom = 51f;
        private const float FormHeight = 294f;
        private const float ActionWidth = 160f;
        private const float ActionHeight = 80f;
        private const float XpBottom = 8f;
        private const float XpHeight = 40f;
        private const float JournalTop = 22f;
        private const float JournalWidth = 280f;
        private const float JournalHeight = 48f;
        private const float JournalPanelWidth = 720f;
        private const float JournalPanelHeight = 470f;
        private const float TooltipWidth = 560f;
        private const float TooltipHeight = 130f;
        private const float ResultWidth = 520f;
        private const float ResultHeight = 260f;

        /// <summary>Точка хвата оружия игрока в долях тела: по кулакам профилей брони.</summary>
        private static readonly Vector2 PlayerWeaponMount = new Vector2(0.90f, 0.70f);

        /// <summary>Точка хвата оружия противника в долях тела: по кулакам профилей врагов.</summary>
        private static readonly Vector2 EnemyWeaponMount = new Vector2(0.875f, 0.69f);

        /// <summary>Пункт меню: собрать префаб боевого экрана и привязать его к сцене.</summary>
        [MenuItem("Tools/AnomalyPath/UI/Собрать префаб «Бой»")]
        public static void BuildFromMenu() => Build();

        /// <summary>Точка входа batch-режима: префаб и привязка к сцене.</summary>
        public static void BuildFromBatch()
        {
            try
            {
                Build();
                EditorApplication.Exit(0);
            }
            catch (Exception error)
            {
                Debug.LogException(error);
                EditorApplication.Exit(1);
            }
        }

        /// <summary>Точка входа batch-режима: префаб, привязка к сцене и кадр экрана.</summary>
        public static void BuildAndCaptureFromBatch()
        {
            try
            {
                Build();
                CombatScreenScreenshot.CaptureAll();
                EditorApplication.Exit(0);
            }
            catch (Exception error)
            {
                Debug.LogException(error);
                EditorApplication.Exit(1);
            }
        }

        /// <summary>Собирает префаб и назначает его на <c>GameScreen</c>.</summary>
        public static void Build()
        {
            SpriteCatalogBuilder.Rebuild();
            EnsureFolder("Assets/Prefabs");
            EnsureFolder(ScreensFolder);

            var prefab = SavePrefab(BuildScreen(), PrefabPath);
            AssetDatabase.SaveAssets();
            AssignToScene(prefab);
            AssetDatabase.Refresh();
            Debug.Log("AnomalyPath: префаб боевого экрана собран");
        }

        /// <summary>Собирает объекты префаба: корень, поле, форма действий, журнал и итог боя.</summary>
        /// <returns>Корень префаба.</returns>
        private static GameObject BuildScreen()
        {
            var root = BuildRoot(out var content);
            var screen = root.GetComponent<CombatScreen>();
            var field = BuildField(content, screen);
            BuildMenuCatcher(content, screen);
            BuildForm(content, screen);
            BuildXpBar(content, screen);
            BuildJournal(content, screen);
            BuildTooltip(content, screen);
            BuildResult(content, screen);
            field.SetAsFirstSibling();
            return root;
        }

        private static GameObject BuildRoot(out RectTransform content)
        {
            var root = new GameObject("CombatScreen", typeof(RectTransform), typeof(Image));
            Stretch((RectTransform)root.transform, 0f);
            root.GetComponent<Image>().color = UiFactory.BackgroundColor;
            var backdrop = AddBackdrop(root.transform);
            content = CreateRect(root.transform, "Content");
            Stretch(content, 0f);
            var screen = root.AddComponent<CombatScreen>();
            SetObject(screen, "backdrop", backdrop);
            SetObject(screen, "content", content);
            return root;
        }

        private static ScreenBackdrop AddBackdrop(Transform parent)
        {
            var image = CreateImage(parent, "ScreenBackdrop", Color.white);
            Stretch(image.rectTransform, 0f);
            var backdrop = image.gameObject.AddComponent<ScreenBackdrop>();
            SetObject(backdrop, "image", image);
            return backdrop;
        }

        /// <summary>Собирает поле боя: плашки бойцов, фигуры и оружие.</summary>
        /// <param name="content">Корень содержимого.</param>
        /// <param name="screen">Собираемый экран.</param>
        /// <returns>Прямоугольник поля.</returns>
        private static RectTransform BuildField(RectTransform content, CombatScreen screen)
        {
            var field = CreateImage(content, "CombatField", CombatPalette.Field);
            var rect = field.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(Edge, FieldBottom);
            rect.offsetMax = new Vector2(-Edge, -FieldTop);

            BuildHealthPlate(rect, screen, player: true);
            BuildHealthPlate(rect, screen, player: false);
            BuildFigure(rect, screen, player: true);
            BuildFigure(rect, screen, player: false);
            BuildWeaponPlate(rect, screen, player: true);
            BuildWeaponPlate(rect, screen, player: false);
            return rect;
        }

        /// <summary>Плашка бойца: имя, полоса здоровья и очки действия игрока.</summary>
        /// <param name="field">Прямоугольник поля.</param>
        /// <param name="screen">Собираемый экран.</param>
        /// <param name="player">Плашка игрока или противника.</param>
        private static void BuildWeaponPlate(RectTransform field, CombatScreen screen, bool player)
        {
            var plate = CreateImage(
                field,
                player ? "PlayerWeapon" : "EnemyWeapon",
                CombatPalette.WeaponPlate);
            if (player)
            {
                PlaceFraction(plate.rectTransform, new Vector2(0.024f, 0f), new Vector2(0.434f, 0.157f));
            }
            else
            {
                PlaceFraction(plate.rectTransform, new Vector2(0.614f, 0.025f), new Vector2(0.982f, 0.182f));
            }

            var icon = CreateImage(plate.transform, "Icon", Color.white);
            icon.preserveAspect = true;
            icon.enabled = false;
            icon.rectTransform.sizeDelta = new Vector2(64f, 48f);
            icon.rectTransform.anchorMin = new Vector2(player ? 0f : 1f, 0.5f);
            icon.rectTransform.anchorMax = new Vector2(player ? 0f : 1f, 0.5f);
            icon.rectTransform.pivot = new Vector2(player ? 0f : 1f, 0.5f);
            icon.rectTransform.anchoredPosition = new Vector2(player ? 16f : -16f, 0f);

            var ammo = CreateText(
                plate.transform,
                "Ammo",
                string.Empty,
                18,
                player ? TextAnchor.MiddleLeft : TextAnchor.MiddleRight,
                UiFactory.TextColor);
            var pad = player ? 96f : 96f;
            ammo.rectTransform.anchorMin = new Vector2(0f, 0f);
            ammo.rectTransform.anchorMax = new Vector2(1f, 1f);
            ammo.rectTransform.offsetMin = new Vector2(player ? pad : 16f, 0f);
            ammo.rectTransform.offsetMax = new Vector2(player ? -16f : -pad, 0f);

            if (player)
            {
                SetObject(screen, "playerWeaponIcon", icon);
                SetObject(screen, "playerAmmo", ammo);
            }
            else
            {
                SetObject(screen, "enemyWeaponIcon", icon);
                SetObject(screen, "enemyAmmo", ammo);
            }
        }

        /// <summary>Полоса опыта внизу экрана.</summary>
        /// <param name="content">Корень содержимого.</param>
        /// <param name="screen">Собираемый экран.</param>
        private static void BuildXpBar(RectTransform content, CombatScreen screen)
        {
            var bar = CreateImage(content, "CombatXp", CombatPalette.XpBack);
            bar.rectTransform.anchorMin = new Vector2(0f, 0f);
            bar.rectTransform.anchorMax = new Vector2(1f, 0f);
            bar.rectTransform.pivot = new Vector2(0.5f, 0f);
            bar.rectTransform.sizeDelta = new Vector2(-(2f * Edge), XpHeight);
            bar.rectTransform.anchoredPosition = new Vector2(0f, XpBottom);

            var fill = CreateImage(bar.transform, "Fill", CombatPalette.XpFill);
            Stretch(fill.rectTransform, 2f);
            ApplySprite(fill, BarFillPath);
            MakeFilled(fill);
            var label = CreateText(bar.transform, "Label", string.Empty, 14, TextAnchor.MiddleCenter, UiFactory.TextColor);
            Stretch(label.rectTransform, 0f);
            SetObject(screen, "xpFill", fill);
            SetObject(screen, "xpText", label);
        }

        /// <summary>Кнопка журнала и полупрозрачное окно журнала действий.</summary>
        /// <param name="content">Корень содержимого.</param>
        /// <param name="screen">Собираемый экран.</param>
        private static void BuildJournal(RectTransform content, CombatScreen screen)
        {
            var button = CreateButton(content, "CombatJournal", 20);
            PlaceTopRight(
                (RectTransform)button.transform,
                new Vector2(-Edge, -JournalTop),
                new Vector2(JournalWidth, JournalHeight));
            AddLocalized(button.GetComponentInChildren<Text>(), UiTextKey.Combat_Log_Title);
            SetObject(screen, "journal", button);

            var catcher = CreateButton(content, "CombatJournalCatcher", 16);
            Stretch((RectTransform)catcher.transform, 0f);
            catcher.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0f);
            catcher.gameObject.SetActive(false);
            SetObject(screen, "journalCatcher", catcher.gameObject);

            var panel = CreateImage(content, "CombatJournalPanel", CombatPalette.Journal);
            PanelArtAssets.Apply(panel, 0.9f);
            PlaceTopRight(
                panel.rectTransform,
                new Vector2(-Edge, -(JournalTop + JournalHeight + 12f)),
                new Vector2(JournalPanelWidth, JournalPanelHeight));
            var title = CreateText(
                panel.transform,
                "Title",
                string.Empty,
                20,
                TextAnchor.MiddleLeft,
                UiFactory.MutedTextColor,
                bold: true);
            PlaceTopLeft(title.rectTransform, new Vector2(26f, -22f), new Vector2(JournalPanelWidth - 52f, 28f));
            AddLocalized(title, UiTextKey.Combat_Log_Title);
            var text = CreateText(panel.transform, "Text", string.Empty, 16, TextAnchor.UpperLeft, UiFactory.TextColor);
            PlaceTopLeft(
                text.rectTransform,
                new Vector2(26f, -54f),
                new Vector2(JournalPanelWidth - 52f, JournalPanelHeight - 74f));
            panel.gameObject.SetActive(false);
            SetObject(screen, "journalPanel", panel.gameObject);
            SetObject(screen, "journalText", text);
        }

        /// <summary>Подсказка при наведении на действие.</summary>
        /// <param name="content">Корень содержимого.</param>
        /// <param name="screen">Собираемый экран.</param>
        private static void BuildTooltip(RectTransform content, CombatScreen screen)
        {
            var panel = CreateImage(content, "CombatTooltip", CombatPalette.Tooltip);
            PanelArtAssets.Apply(panel);
            PlaceAt(
                panel.rectTransform,
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -(FieldTop + 12f)),
                new Vector2(TooltipWidth, TooltipHeight));
            var text = CreateText(panel.transform, "Text", string.Empty, 15, TextAnchor.UpperLeft, UiFactory.TextColor);
            Stretch(text.rectTransform, 22f);
            panel.gameObject.SetActive(false);
            SetObject(screen, "tooltip", panel.gameObject);
            SetObject(screen, "tooltipText", text);
        }

        /// <summary>Итог боя: заголовок победы или поражения и кнопка возврата.</summary>
        /// <param name="content">Корень содержимого.</param>
        /// <param name="screen">Собираемый экран.</param>
        private static void BuildResult(RectTransform content, CombatScreen screen)
        {
            var panel = CreateImage(content, "CombatResult", new Color(0.03f, 0.05f, 0.04f, 0.92f));
            PanelArtAssets.Apply(panel);
            panel.raycastTarget = true;
            PlaceCenter(panel.rectTransform, Vector2.zero, new Vector2(ResultWidth, ResultHeight));
            var title = CreateText(
                panel.transform,
                "Title",
                string.Empty,
                26,
                TextAnchor.MiddleCenter,
                UiFactory.TextColor,
                bold: true);
            PlaceTopLeft(title.rectTransform, new Vector2(30f, -32f), new Vector2(ResultWidth - 60f, 40f));
            var backButton = CreateActionButton(panel.transform, "CombatBack", out var backLabel);
            PlaceTopLeft(
                (RectTransform)backButton.transform,
                new Vector2((ResultWidth - 160f) / 2f, -150f),
                new Vector2(160f, 80f));
            AddLocalized(backLabel, UiTextKey.Combat_Back);
            panel.gameObject.SetActive(false);
            SetObject(screen, "resultPanel", panel.gameObject);
            SetObject(screen, "resultTitle", title);
            SetObject(screen, "back", backButton);
        }

        private static void AssignToScene(GameObject prefab)
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var screen = UnityEngine.Object.FindAnyObjectByType<AnomalyPath.Runtime.GameScreen>();
            if (screen == null)
            {
                throw new InvalidOperationException($"В сцене {ScenePath} нет GameScreen");
            }

            SetObject(screen, "combatScreenPrefab", prefab);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
        }
    }
}
