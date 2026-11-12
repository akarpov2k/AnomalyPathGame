#nullable enable
using System;
using AnomalyPath.Runtime.Screens.Minigames;
using AnomalyPath.Runtime.Ui.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace AnomalyPath.Editor.Ui
{
    /// <summary>
    /// Собирает префабы мини-игр: «Взлом тайника» (код, попытки, история) и
    /// «Поиск артефакта» (поле, сканы, обыск). Префабы назначаются на <c>GameScreen</c>
    /// в сцене Main.
    /// </summary>
    public static partial class MinigameScreensPrefabBuilder
    {
        private const string ScreensFolder = "Assets/Prefabs/Screens";
        private const string LockPath = "Assets/Prefabs/Screens/LockMinigameScreen.prefab";
        private const string RadarPath = "Assets/Prefabs/Screens/RadarMinigameScreen.prefab";
        private const string ScenePath = "Assets/Scenes/Main.unity";
        private const string ActionFramePath = "Assets/Art/Buttons/ui_btn_mp_ind_empty.png";
        private const string ActionFrameGreenPath = "Assets/Art/Buttons/ui_btn_mp_ind_green.png";

        private const float LockWidth = 1200f;
        private const float LockHeight = 840f;
        private const float RadarWidth = 1300f;
        private const float RadarHeight = 720f;
        private const float SlotWidth = 88f;
        private const float SlotHeight = 110f;
        private const float SlotGap = 16f;
        private const float SymbolSize = 72f;
        private const float SymbolGap = 14f;
        private const float CellSize = 160f;
        private const float CellGap = 14f;
        private const int RadarColumns = 3;
        private const int RadarRows = 2;

        /// <summary>Пункт меню: собрать оба префаба и назначить их сцене.</summary>
        [MenuItem("Tools/AnomalyPath/UI/Собрать префабы «Взлом» и «Радар»")]
        public static void BuildFromMenu() => Build();

        /// <summary>Точка входа batch-режима.</summary>
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

        /// <summary>Собирает оба префаба и назначает их сцене.</summary>
        public static void Build()
        {
            AnomalyPath.Editor.Art.SpriteCatalogBuilder.Rebuild();
            EnsureFolder(ScreensFolder);
            var lockPrefab = SavePrefab(BuildLock(), LockPath);
            var radarPrefab = SavePrefab(BuildRadar(), RadarPath);
            AssetDatabase.SaveAssets();
            AssignToScene(lockPrefab, radarPrefab);
            Debug.Log("AnomalyPath: префабы мини-игр собраны");
        }

        private static GameObject BuildLock()
        {
            var root = BuildRoot<LockMinigameScreen>("LockMinigameScreen", ScreenBackdropIds.Lock);
            var window = CreateImage(root.transform.Find("Content"), "Window", MinigamePalette.Window);
            PanelArtAssets.ApplyPanel(window);
            PlaceCenter(window.rectTransform, new Vector2(LockWidth, LockHeight));

            var title = CreateText(window.transform, "Title", string.Empty, 32, TextAnchor.MiddleLeft,
                UiFactory.TextColor, bold: true);
            PlaceTopLeft(title.rectTransform, new Vector2(40f, -24f), new Vector2(600f, 48f));
            AddLocalized(title, UiTextKey.Lock_Title);

            var attempts = CreateText(window.transform, "Attempts", string.Empty, 20, TextAnchor.MiddleRight,
                UiFactory.TextColor);
            PlaceTopRight(attempts.rectTransform, new Vector2(-40f, -26f), new Vector2(400f, 36f));

            var code = CreateText(window.transform, "CodeLabel", string.Empty, 18, TextAnchor.MiddleLeft,
                UiFactory.MutedTextColor);
            PlaceTopLeft(code.rectTransform, new Vector2(40f, -104f), new Vector2(420f, 30f));

            var slots = CreateRect(window.transform, "Slots");
            PlaceTopLeft(slots, new Vector2(40f, -146f), new Vector2(8f * (SlotWidth + SlotGap), SlotHeight));
            for (var index = 0; index < 8; index++)
            {
                var slot = CreateSlot(slots, "Slot" + index, index * (SlotWidth + SlotGap));
                if (index >= 3)
                {
                    slot.SetActive(false);
                }
            }

            var symbols = CreateRect(window.transform, "Symbols");
            PlaceTopLeft(symbols, new Vector2(40f, -300f), new Vector2(9f * (SymbolSize + SymbolGap), SymbolSize));
            for (var index = 0; index < 9; index++)
            {
                var symbol = CreateSymbol(symbols, "Symbol" + index, index * (SymbolSize + SymbolGap));
                if (index >= 5)
                {
                    symbol.SetActive(false);
                }
            }

            var history = CreateImage(window.transform, "History", MinigamePalette.Panel);
            PanelArtAssets.ApplyPanel(history, PanelArtAssets.GreenTint);
            PlaceTopLeft(history.rectTransform, new Vector2(40f, -440f), new Vector2(1120f, 300f));
            var historyTitle = CreateText(history.transform, "HistoryTitle", "Попытки", 18, TextAnchor.MiddleLeft,
                UiFactory.TextColor, bold: true);
            PlaceTopLeft(historyTitle.rectTransform, new Vector2(16f, -12f), new Vector2(300f, 28f));
            CreateHistoryHeader(history.transform);
            var rows = CreateRect(history.transform, "Rows");
            PlaceTopLeft(rows, new Vector2(16f, -64f), new Vector2(1088f, 230f));
            for (var index = 0; index < 8; index++)
            {
                var row = CreateHistoryRow(rows, "HistoryRow" + index, index * 32f);
                if (index >= 7)
                {
                    row.SetActive(false);
                }
            }

            var status = CreateText(window.transform, "Status", string.Empty, 18, TextAnchor.MiddleLeft,
                UiFactory.MutedTextColor);
            PlaceTopLeft(status.rectTransform, new Vector2(40f, -400f), new Vector2(800f, 36f));

            var clear = CreateButton(window.transform, "LockClear", string.Empty, 20);
            PlaceTopLeft((RectTransform)clear.transform, new Vector2(40f, -760f), new Vector2(160f, 80f));
            AddLocalized(clear.GetComponentInChildren<Text>(), UiTextKey.Lock_Clear);

            var submit = CreateActionButton(window.transform, "LockSubmit", out var submitLabel);
            PlaceTopLeft((RectTransform)submit.transform, new Vector2(220f, -760f), new Vector2(240f, 80f));
            AddLocalized(submitLabel, UiTextKey.Lock_Submit);

            var leave = CreateButton(window.transform, "LockLeave", string.Empty, 20);
            PlaceTopRight((RectTransform)leave.transform, new Vector2(-40f, -760f), new Vector2(160f, 80f));
            AddLocalized(leave.GetComponentInChildren<Text>(), UiTextKey.Lock_Leave);

            var screen = root.GetComponent<LockMinigameScreen>();
            SetObject(screen, "title", title);
            SetObject(screen, "attempts", attempts);
            SetObject(screen, "code", code);
            SetObject(screen, "status", status);
            SetObject(screen, "slotsRoot", slots);
            SetObject(screen, "symbolsRoot", symbols);
            SetObject(screen, "historyRoot", rows);
            SetObject(screen, "clear", clear);
            SetObject(screen, "submit", submit);
            SetObject(screen, "leave", leave);
            return root;
        }

        private static GameObject BuildRadar()
        {
            var root = BuildRoot<RadarMinigameScreen>("RadarMinigameScreen", ScreenBackdropIds.Radar);
            var window = CreateImage(root.transform.Find("Content"), "Window", MinigamePalette.Window);
            PanelArtAssets.ApplyPanel(window);
            PlaceCenter(window.rectTransform, new Vector2(RadarWidth, RadarHeight));

            var title = CreateText(window.transform, "Title", string.Empty, 32, TextAnchor.MiddleLeft,
                UiFactory.TextColor, bold: true);
            PlaceTopLeft(title.rectTransform, new Vector2(40f, -24f), new Vector2(600f, 48f));
            AddLocalized(title, UiTextKey.Radar_Title);

            var attempts = CreateText(window.transform, "Attempts", string.Empty, 20, TextAnchor.MiddleRight,
                UiFactory.TextColor);
            PlaceTopRight(attempts.rectTransform, new Vector2(-40f, -26f), new Vector2(400f, 30f));
            var health = CreateText(window.transform, "Health", string.Empty, 20, TextAnchor.MiddleRight,
                UiFactory.TextColor);
            PlaceTopRight(health.rectTransform, new Vector2(-40f, -60f), new Vector2(400f, 30f));
            var scans = CreateText(window.transform, "Scans", string.Empty, 20, TextAnchor.MiddleRight,
                UiFactory.TextColor);
            PlaceTopRight(scans.rectTransform, new Vector2(-40f, -94f), new Vector2(400f, 30f));

            var grid = CreateRect(window.transform, "Grid");
            PlaceTopLeft(
                grid,
                new Vector2(40f, -110f),
                new Vector2(
                    RadarColumns * (CellSize + CellGap),
                    RadarRows * (CellSize + CellGap)));
            for (var index = 0; index < RadarColumns * RadarRows; index++)
            {
                CreateCell(grid, "Cell" + index, index);
            }

            var panel = CreateImage(window.transform, "Panel", MinigamePalette.Panel);
            PanelArtAssets.ApplyPanel(panel);
            PlaceTopLeft(panel.rectTransform, new Vector2(600f, -110f), new Vector2(640f, 420f));
            var signal = CreateText(panel.transform, "SignalTitle", "Сигнал", 18, TextAnchor.MiddleLeft,
                UiFactory.TextColor, bold: true);
            PlaceTopLeft(signal.rectTransform, new Vector2(16f, -12f), new Vector2(300f, 28f));
            var legend = CreateText(panel.transform, "Legend", "4 — сильный\n3\n2\n1 — слабый", 16,
                TextAnchor.UpperLeft, UiFactory.TextColor);
            PlaceTopLeft(legend.rectTransform, new Vector2(16f, -48f), new Vector2(300f, 120f));

            var logTitle = CreateText(panel.transform, "LogTitle", "Журнал сканов", 18, TextAnchor.MiddleLeft,
                UiFactory.TextColor, bold: true);
            PlaceTopLeft(logTitle.rectTransform, new Vector2(16f, -200f), new Vector2(300f, 28f));
            var log = CreateText(panel.transform, "Log", string.Empty, 15, TextAnchor.UpperLeft,
                UiFactory.MutedTextColor);
            PlaceTopLeft(log.rectTransform, new Vector2(16f, -236f), new Vector2(600f, 140f));

            var status = CreateText(window.transform, "Status", string.Empty, 18, TextAnchor.MiddleLeft,
                UiFactory.MutedTextColor);
            PlaceTopLeft(status.rectTransform, new Vector2(40f, -500f), new Vector2(520f, 40f));

            var scan = CreateButton(window.transform, "RadarScan", string.Empty, 20);
            PlaceTopLeft((RectTransform)scan.transform, new Vector2(40f, -570f), new Vector2(240f, 80f));
            AddLocalized(scan.GetComponentInChildren<Text>(), UiTextKey.Radar_Scan);

            var search = CreateActionButton(window.transform, "RadarSearch", out var searchLabel);
            PlaceTopLeft((RectTransform)search.transform, new Vector2(300f, -570f), new Vector2(240f, 80f));
            AddLocalized(searchLabel, UiTextKey.Radar_Search);

            var leave = CreateButton(window.transform, "RadarLeave", string.Empty, 20);
            PlaceTopRight((RectTransform)leave.transform, new Vector2(-40f, -570f), new Vector2(160f, 80f));
            AddLocalized(leave.GetComponentInChildren<Text>(), UiTextKey.Radar_Leave);

            var screen = root.GetComponent<RadarMinigameScreen>();
            SetObject(screen, "title", title);
            SetObject(screen, "attempts", attempts);
            SetObject(screen, "health", health);
            SetObject(screen, "scans", scans);
            SetObject(screen, "status", status);
            SetObject(screen, "log", log);
            SetObject(screen, "gridRoot", grid);
            SetObject(screen, "scan", scan);
            SetObject(screen, "search", search);
            SetObject(screen, "leave", leave);
            return root;
        }
    }
}
