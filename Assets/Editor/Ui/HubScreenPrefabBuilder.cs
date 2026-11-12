#nullable enable
using System;
using AnomalyPath.Runtime.Screens.Game.Hub;
using AnomalyPath.Runtime.Ui.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace AnomalyPath.Editor.Ui
{
    /// <summary>
    /// Собирает префаб комнаты торговца (малого хаба): интерьер во весь экран, по центру
    /// выбранный торговец в полный рост за передним столом-стойкой, слева дверь (обычная
    /// и со свечением) и часы, внизу — настройки слева и инвентарь справа. Торговец кликабелен
    /// и открывает своё окно; дверь возвращает в большой хаб. Карта, доска, выпадашка торговцев
    /// и полоса опыта из префаба убраны.
    /// </summary>
    public static partial class HubScreenPrefabBuilder
    {
        private const string PrefabPath = "Assets/Prefabs/Screens/HubScreen.prefab";
        private const string ScreensFolder = "Assets/Prefabs/Screens";
        private const string ScenePath = "Assets/Scenes/Main.unity";
        private const string IconsFolder = "Assets/Art/Icons";

        /// <summary>Картинки объектов хаба: <c>hub_door</c> и <c>hub_door_glow</c>.</summary>
        private const string HubArtFolder = "Assets/Art/Hub";

        /// <summary>Полоса переднего стола, которая закрывает низ фигуры торговца.</summary>
        private const string CounterPath = "Assets/Art/Hub/hub_counter.png";

        /// <summary>Картинка торговца по умолчанию; в игре её подменяет <c>hubPortrait</c> актёра.</summary>
        private const string DefaultMerchantPath = "Assets/Art/Portraits/trd_hub_smuggler_full.png";

        private const float ClockLeft = 65f;
        private const float ClockTop = 52f;
        private const float ClockSize = 132f;
        private const float ClockTimeGap = 12f;

        private const float HudSize = 112f;
        private const float IconSize = 64f;
        private const float IconBottom = 40f;
        private const float IconSide = 40f;

        /// <summary>Значок выбора торговца стоит слева от окна диалога, рядом с шестерёнкой.</summary>
        private const float TradersLeft = IconSide + HudSize + 16f;

        /// <summary>Зазор между значком ремонта и рюкзаком инвентаря.</summary>
        private const float RepairGap = 16f;

        /// <summary>
        /// Низ строки сообщений: выше меню диалога торговца (его высота около 292), иначе
        /// сообщение «Отремонтировано» и отказы прятались за окном разговора.
        /// </summary>
        private const float ToastBottom = 320f;
        private const float TooltipWidth = 460f;
        private const float TooltipHeight = 64f;

        /// <summary>Дверь слева от торговца: холст 633×1035, вписан в эти доли сцены.</summary>
        private const float DoorLeft = 0.05208f;

        private const float DoorRight = 0.24219f;
        private const float DoorBottom = 0.14352f;
        private const float DoorTop = 0.69537f;

        /// <summary>Картинка торговца: 700×1262, верх фигуры — на 200 единиц ниже верха сцены.</summary>
        private const float MerchantWidth = 700f;

        private const float MerchantHeight = 1262f;
        private const float MerchantTop = 200f;

        /// <summary>Кликабельная зона торговца: от головы до линии стола.</summary>
        private const float MerchantHitWidth = 560f;

        private const float MerchantHitHeight = 725f;

        /// <summary>Полоса переднего стола внизу сцены: 2752×221 исходника.</summary>
        private const float CounterHeight = 155.5f;

        /// <summary>Пункт меню: собрать префаб экрана хаба и привязать его к сцене.</summary>
        [MenuItem("Tools/AnomalyPath/UI/Собрать префаб «Хаб»")]
        public static void BuildFromMenu() => Build();

        /// <summary>Точка входа batch-режима: каталог спрайтов и префаб за один запуск.</summary>
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

        /// <summary>Собирает префаб экрана и назначает его в сцене Main.</summary>
        public static void Build()
        {
            AnomalyPath.Editor.Art.SpriteCatalogBuilder.Rebuild();
            EnsureFolder("Assets/Prefabs");
            EnsureFolder(ScreensFolder);

            var prefab = SavePrefab(BuildScreen(), PrefabPath);
            AssetDatabase.SaveAssets();
            AssignToScene(prefab);
            Debug.Log("AnomalyPath: префаб экрана хаба собран");
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

            SetObject(screen, "hubScreenPrefab", prefab);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
        }

        private static GameObject BuildScreen()
        {
            var icons = LoadIcons();
            var root = new GameObject("Hub", typeof(RectTransform), typeof(Image));
            Stretch((RectTransform)root.transform);
            root.GetComponent<Image>().color = UiFactory.BackgroundColor;

            var backdrop = AddBackdrop(root.transform);
            var content = CreateRect(root.transform, "Content");
            Stretch(content);

            // Сцена заполняет весь экран: полос по краям быть не должно.
            var stage = CreateRect(content, "Stage");
            Stretch(stage);

            var interior = CreateImage(stage, "Interior", Color.white);
            Stretch(interior.rectTransform);
            var interiorBackdrop = interior.gameObject.AddComponent<ScreenBackdrop>();
            SetObject(interiorBackdrop, "image", interior);

            var tooltip = BuildTooltip(stage);

            BuildMerchant(stage, tooltip, out var merchant, out var merchantButton, out var merchantZone);
            BuildCounter(stage);

            var door = BuildZone(
                stage,
                "LeaveHub",
                UiText.Code(UiTextKey.Hub_Back),
                DoorLeft,
                DoorRight,
                DoorBottom,
                DoorTop,
                LoadObjectArt("hub_door"),
                tooltip);

            BuildClock(stage, icons, out var clockHand, out var clockTime);

            var gear = BuildIconButton(
                stage,
                "Gear",
                icons.Gear,
                UiText.Code(UiTextKey.GameMenu_Title),
                tooltip,
                new Vector2(0f, 0f),
                new Vector2(0f, 0f),
                new Vector2(IconSide, IconBottom));
            var traders = BuildIconButton(
                stage,
                "Traders",
                icons.Bag,
                UiText.Code(UiTextKey.Trader_Traders),
                tooltip,
                new Vector2(0f, 0f),
                new Vector2(0f, 0f),
                new Vector2(TradersLeft, IconBottom));
            var repair = BuildIconButton(
                stage,
                "Repair",
                icons.Wrench,
                UiText.Code(UiTextKey.Trader_Repair),
                tooltip,
                new Vector2(1f, 0f),
                new Vector2(1f, 0f),
                new Vector2(-(IconSide + HudSize + RepairGap), IconBottom));
            var inventory = BuildIconButton(
                stage,
                "Backpack",
                icons.Backpack,
                UiText.Code(UiTextKey.Inventory_Open),
                tooltip,
                new Vector2(1f, 0f),
                new Vector2(1f, 0f),
                new Vector2(-IconSide, IconBottom));

            var toast = CreateText(stage, "Toast", string.Empty, 20, TextAnchor.MiddleCenter, UiFactory.ToastColor);
            Place(
                toast.rectTransform,
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0f, ToastBottom),
                new Vector2(900f, 56f));

            var screen = root.AddComponent<HubScreen>();
            SetObject(screen, "backdrop", backdrop);
            SetObject(screen, "content", content);
            SetObject(screen, "background", interiorBackdrop);
            SetObject(screen, "clockHand", clockHand);
            SetObject(screen, "clockTime", clockTime);
            SetObject(screen, "gear", gear);
            SetObject(screen, "door", door);
            SetObject(screen, "inventory", inventory);
            SetObject(screen, "traders", traders);
            SetObject(screen, "repair", repair);
            SetObject(screen, "merchant", merchant);
            SetObject(screen, "merchantButton", merchantButton);
            SetObject(screen, "merchantZone", merchantZone);
            SetObject(screen, "tooltip", tooltip);
            SetObject(screen, "toast", toast);
            return root;
        }
    }
}
