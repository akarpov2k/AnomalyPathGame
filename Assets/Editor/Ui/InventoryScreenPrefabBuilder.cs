#nullable enable
using System;
using AnomalyPath.Core;
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Core.Di;
using AnomalyPath.Runtime.Screens.Character;
using AnomalyPath.Runtime.Ui.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace AnomalyPath.Editor.Ui
{
    /// <summary>
    /// Собирает префаб окна «инвентарь» по прототипу `.prototypes/инвентарь.penpot`:
    /// слева персонаж (костюм в полный рост, слоты, артефакты, панель защиты с кнопкой-глазом и
    /// полоса состояния), справа инвентарь с сеткой, валютой и фильтрами внизу; крестик выхода —
    /// в левом верхнем углу. Префаб после сборки — источник истины: разметка и кнопки дальше
    /// правятся в инспекторе.
    /// </summary>
    public static partial class InventoryScreenPrefabBuilder
    {
        private const string PrefabPath = "Assets/Prefabs/Screens/InventoryScreen.prefab";
        private const string ScreensFolder = "Assets/Prefabs/Screens";
        private const string ScenePath = "Assets/Scenes/Main.unity";
        private const string DataRoot = "Assets/Data";

        /// <summary>Ширина композиции персонажа в прототипе.</summary>
        private const float ReferenceWidth = 983f;

        /// <summary>Сдвиг слотов, фигуры и нижней полосы к инвентарю: слева освобождается место под статистику.</summary>
        private const float SlotsShift = 240f;

        /// <summary>Ширина композиции с колонкой статистики слева.</summary>
        private const float LayoutWidth = ReferenceWidth + SlotsShift;

        /// <summary>Высота композиции персонажа в прототипе.</summary>
        private const float ReferenceHeight = 1036f;

        /// <summary>Левая граница панели статистики в композиции.</summary>
        private const float StatsLeft = 16f;

        /// <summary>Верхняя граница панели защиты: ниже кнопок крестика и глаза.</summary>
        private const float StatsTop = 150f;

        /// <summary>Ширина панели защиты.</summary>
        private const float StatsWidth = 240f;

        /// <summary>Левая граница кнопки-глаза: по центру под кнопкой выхода.</summary>
        private const float EyeLeft = 92f;

        /// <summary>Верхняя граница кнопки-глаза: сразу под кнопкой выхода.</summary>
        private const float EyeTop = 84f;

        /// <summary>Сторона кнопки-глаза.</summary>
        private const float EyeSize = 56f;

        /// <summary>Левая граница кнопки «Таланты»: слева, под панелью защиты.</summary>
        private const float TalentsLeft = 16f;

        /// <summary>Верхняя граница кнопки «Таланты»: у нижней полосы состояния.</summary>
        private const float TalentsTop = 930f;

        /// <summary>Ширина кнопки «Таланты».</summary>
        private const float TalentsWidth = 240f;

        /// <summary>Высота кнопки «Таланты».</summary>
        private const float TalentsHeight = 58f;

        /// <summary>Левая граница столбца быстрых слотов: в колонке персонажа под панелью «Защита».</summary>
        private const float QuickLeft = 16f;

        /// <summary>Верхняя граница заголовка «Быстрый доступ».</summary>
        private const float QuickTop = 380f;

        /// <summary>Ширина столбца быстрых слотов.</summary>
        private const float QuickWidth = 240f;

        /// <summary>Высота одного быстрого слота.</summary>
        private const float QuickSlotHeight = 72f;

        /// <summary>Зазор между быстрыми слотами.</summary>
        private const float QuickGap = 8f;

        /// <summary>Прозрачность панели персонажа поверх фона-сундука.</summary>
        private const float CharacterPanelAlpha = 0.88f;

        /// <summary>Кант рамки слота у крупных слотов — как у панели диалога.</summary>
        private const float SlotFrameLarge = 1f;

        /// <summary>Во сколько раз тоньше кант рамки у маленьких слотов.</summary>
        private const float SlotFrameSmall = 3f;

        /// <summary>Прозрачность панели защиты поверх фона-сундука.</summary>
        private const float StatsPanelAlpha = 0.95f;

        /// <summary>Во сколько раз тоньше кант рамки панели защиты.</summary>
        private const float StatsFrameScale = 2f;

        /// <summary>Ширина окна инвентаря в прототипе.</summary>
        private const float ReferenceInventoryWidth = 465f;

        /// <summary>Зазор между панелями в прототипе.</summary>
        private const float ReferenceGap = 24f;

        /// <summary>Полная ширина макета: персонаж, зазор и окно инвентаря.</summary>
        private const float ReferenceTotal = ReferenceWidth + ReferenceGap + ReferenceInventoryWidth;

        /// <summary>Пункт меню: собрать префаб окна «инвентарь» и привязать его к сцене.</summary>
        [MenuItem("Tools/AnomalyPath/UI/Собрать префаб «Инвентарь»")]
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

        /// <summary>Собирает префаб окна и назначает его в сцене Main.</summary>
        public static void Build()
        {
            var services = LoadServices();
            var texts = services.Resolve<IGameTexts>();
            EnsureFolder("Assets/Prefabs");
            EnsureFolder(ScreensFolder);

            var ui = new UiFactory(UiFactory.FindFont(), ButtonArtAssets.Normal(), ButtonArtAssets.Highlighted());
            var prefab = SavePrefab(BuildScreen(ui, texts), PrefabPath);
            AssetDatabase.SaveAssets();
            AssignToScene(prefab);
            Debug.Log("AnomalyPath: префаб окна «инвентарь» собран");
        }

        private static ServiceContainer LoadServices()
        {
            var result = Preflight.Run(DataRoot);
            if (!result.CanStart || result.Services == null)
            {
                throw new InvalidOperationException(result.Report.Summary);
            }

            return result.Services;
        }

        private static GameObject SavePrefab(GameObject root, string path)
        {
            root.SetActive(false);
            ButtonArtAssets.ApplyPlatesToFlatButtons(root);
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
            if (prefab == null)
            {
                throw new InvalidOperationException($"Не удалось сохранить префаб {path}");
            }

            return prefab;
        }

        private static void AssignToScene(GameObject prefab)
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var screen = UnityEngine.Object.FindAnyObjectByType<AnomalyPath.Runtime.GameScreen>();
            if (screen == null)
            {
                throw new InvalidOperationException($"В сцене {ScenePath} нет GameScreen");
            }

            SetObject(screen, "inventoryScreenPrefab", prefab);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
        }

        private static GameObject BuildScreen(UiFactory ui, IGameTexts texts)
        {
            var root = CreatePanel("Inventory");
            root.GetComponent<Image>().color = UiFactory.BackgroundColor;
            var backdrop = AddBackdrop(root.transform);
            ScreenBackdropAssets.Apply(backdrop, ScreenBackdropIds.Inventory);
            var content = CreateRect(root.transform, "Content");
            Stretch(content);

            var frames = CreateRect(content, "Frames");
            Stretch(frames);

            var character = BuildCharacterPanel(ui, texts, frames);
            var inventory = BuildInventoryPanel(ui, texts, frames);

            var message = CreateLabel(content, "Message", string.Empty, 18, TextAnchor.MiddleCenter, UiFactory.ToastColor);
            Place(message.rectTransform,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -6f), new Vector2(900f, 26f));

            var ghost = CreateLabel(content, "DragGhost", string.Empty, 18, TextAnchor.MiddleCenter, UiFactory.ToastColor);
            ghost.raycastTarget = false;
            Place(ghost.rectTransform,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(360f, 40f));
            ghost.gameObject.SetActive(false);

            var noGame = CreateLabel(content, "NoGame", string.Empty, 20, TextAnchor.MiddleCenter, UiFactory.MutedTextColor);
            Localize(noGame, UiTextKey.Common_No_Game);
            Place(noGame.rectTransform,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(900f, 40f));
            noGame.gameObject.SetActive(false);

            var screen = root.AddComponent<InventoryScreen>();
            SetObject(screen, "backdrop", backdrop);
            SetObject(screen, "content", content);
            SetObject(screen, "figure", character.Figure);
            SetObject(screen, "artifactRow", character.ArtifactRow);
            SetObject(screen, "artifactTemplate", character.ArtifactTemplate);
            SetObject(screen, "quickRow", character.QuickRow);
            SetObject(screen, "quickTemplate", character.QuickTemplate);
            SetObject(screen, "injuries", character.Injuries);
            SetObject(screen, "nickname", character.Nickname);
            SetObject(screen, "level", character.Level);
            SetObject(screen, "close", character.Close);
            SetObject(screen, "talents", character.Talents);
            SetArray(screen, "slotZones", character.SlotZones);
            SetArray(screen, "slotBodies", character.SlotBodies);
            SetArray(screen, "statValues", character.StatValues);
            SetObject(screen, "statsPanel", character.StatsPanel.gameObject);
            SetObject(screen, "statsDefenseRows", character.StatsDefenseRows);
            SetObject(screen, "statsRowTemplate", character.StatsRowTemplate);
            SetObject(screen, "statsEye", character.StatsEye);
            SetObject(screen, "statsEyeIcon", character.StatsEyeIcon);
            SetObject(screen, "inventoryFrame", inventory.Frame);
            SetObject(screen, "gridArea", inventory.GridScroll);
            SetObject(screen, "money", inventory.Money);
            SetObject(screen, "weight", inventory.Weight);
            SetObject(screen, "overflow", inventory.Overflow);
            SetArray(screen, "filterButtons", inventory.Filters);
            SetObject(screen, "message", message);
            SetObject(screen, "ghost", ghost);
            SetObject(screen, "noGame", noGame);
            return root;
        }
    }
}
