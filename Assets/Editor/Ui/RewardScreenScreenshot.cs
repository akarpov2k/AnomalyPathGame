#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using AnomalyPath.Core;
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Core.Di;
using AnomalyPath.Core.Inventory;
using AnomalyPath.Core.Quests;
using AnomalyPath.Core.Session;
using AnomalyPath.Runtime.Screens.Trader;
using AnomalyPath.Runtime.Ui.Art;
using AnomalyPath.Runtime.Ui.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace AnomalyPath.Editor.Ui
{
    /// <summary>
    /// Снимает кадры экрана наград из собранного префаба: «Трофеи» после победы над врагом
    /// и «Награда» за сданный сюжетный квест с деньгами. Данные для кадров берутся из
    /// настоящих данных игры: бросок лута первого противника и награда первого сюжетного квеста.
    /// </summary>
    public static class RewardScreenScreenshot
    {
        private const string PrefabPath = "Assets/Prefabs/Screens/RewardScreen.prefab";
        private const string OutputFolder = "Logs/Screenshots";
        private const string DataRoot = "Assets/Data";
        private const string CatalogPath = "Assets/Art/SpriteCatalog.asset";
        private const int Width = 1920;
        private const int Height = 1080;
        private const int Columns = 6;
        private const int LootSeed = 42;
        private const int LootTier = 1;

        /// <summary>Пункт меню: снять кадры экрана наград.</summary>
        [MenuItem("Tools/AnomalyPath/UI/Скриншот экрана наград")]
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

        /// <summary>Снимает кадры в <c>Logs/Screenshots/rewards-*.png</c>.</summary>
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
            var catalog = AssetDatabase.LoadAssetAtPath<SpriteCatalog>(CatalogPath);
            var player = NewPlayer(services);

            var trophies = RollTrophies(services);
            Capture("rewards-victory.png", panel => Fill(
                panel,
                texts,
                catalog,
                player,
                trophies,
                0,
                UiTextKey.Rewards_Source_Victory));

            var reward = FirstStoryReward(services);
            Capture("rewards-quest.png", panel => Fill(
                panel,
                texts,
                catalog,
                player,
                reward.Loot,
                reward.Money,
                UiTextKey.Rewards_Source_Quest));

            Debug.Log($"AnomalyPath: кадры экрана наград в {OutputFolder}");
        }

        private static void Fill(
            GameObject panel,
            IGameTexts texts,
            SpriteCatalog? catalog,
            PlayerStateReference player,
            IReadOnlyList<ItemInstance> loot,
            int money,
            UiTextKey sourceKey)
        {
            ApplyLabels(panel, texts);
            SetText(panel, "RewardSource", UiText.Get(texts, sourceKey));
            SetText(panel, "PlayerMoney", UiText.Format(texts, UiTextKey.Common_Money, player.Money));

            var moneyLabel = FindDeep(panel.transform, "RewardMoney");
            if (moneyLabel != null)
            {
                var label = moneyLabel.GetComponent<Text>();
                label.text = money > 0 ? UiText.Format(texts, UiTextKey.Rewards_Money, money) : string.Empty;
            }

            var ui = new UiFactory(UiFactory.FindFont());
            var items = new List<TradeGridItem>();
            foreach (var item in loot)
            {
                var template = player.Items.TryGet(item.ItemId, out var found) ? found : null;
                items.Add(new TradeGridItem(
                    item.InstanceId,
                    item.ItemId,
                    template?.FootprintWidth ?? 1,
                    template?.FootprintHeight ?? 1,
                    player.Describe(item),
                    template?.Icon,
                    item.Stack));
            }

            var playerItems = new List<TradeGridItem>();
            foreach (var item in player.Inventory.Items)
            {
                if (item.Overflow)
                {
                    continue;
                }

                var template = player.Items.TryGet(item.ItemId, out var found) ? found : null;
                playerItems.Add(new TradeGridItem(
                    item.InstanceId,
                    item.ItemId,
                    item.PlacedWidth > 0 ? item.PlacedWidth : template?.FootprintWidth ?? 1,
                    item.PlacedHeight > 0 ? item.PlacedHeight : template?.FootprintHeight ?? 1,
                    player.Describe(item),
                    template?.Icon,
                    item.Stack,
                    item.X,
                    item.Y));
            }

            FillGrid(panel, "RewardPanel", ui, catalog, items, "RewardItem", pack: true);
            FillGrid(panel, "PlayerPanel", ui, catalog, playerItems, "PlayerItem", pack: false);
        }

        private static void Capture(string file, Action<GameObject> fill)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
            {
                throw new InvalidOperationException($"Нет префаба {PrefabPath}");
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
            FillBackdrop(instance);
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

            var path = Path.Combine(OutputFolder, file);
            File.WriteAllBytes(path, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            target.Release();
            UnityEngine.Object.DestroyImmediate(target);
            UnityEngine.Object.DestroyImmediate(canvasObject);
            UnityEngine.Object.DestroyImmediate(cameraObject);
            Debug.Log($"AnomalyPath: скриншот {path}");
        }

        private static PlayerStateReference NewPlayer(ServiceContainer services)
        {
            var newGame = services.Resolve<INewGameService>();
            var state = newGame.Create(42, FirstLoadoutId(services), new NewGameSettings(0, 1));
            var player = state.Player ?? throw new InvalidOperationException("Новая игра не создала игрока");
            return new PlayerStateReference(
                player,
                services.Resolve<IItemRepository>(),
                services.Resolve<IGameTexts>());
        }

        private static string FirstLoadoutId(ServiceContainer services)
        {
            foreach (var loadout in services.Resolve<ILoadoutRepository>().All)
            {
                return loadout.Id;
            }

            throw new InvalidOperationException("В данных нет наборов снаряжения");
        }

        private static IReadOnlyList<ItemInstance> RollTrophies(ServiceContainer services)
        {
            var actors = services.Resolve<IActorRepository>();
            var loot = services.Resolve<ILootService>();
            foreach (var actor in actors.All)
            {
                if (actor.LootTableId == null)
                {
                    continue;
                }

                var rolled = loot.RollTable(actor.LootTableId, LootTier, LootSeed, ignoreTierBand: false);
                if (rolled.Count > 0)
                {
                    return rolled;
                }
            }

            throw new InvalidOperationException("Ни один противник не дал лут для кадра");
        }

        private static StoryReward FirstStoryReward(ServiceContainer services)
        {
            var items = services.Resolve<IItemRepository>();
            StoryReward? fallback = null;
            foreach (var template in services.Resolve<IStoryQuestRepository>().All)
            {
                if (template.Rewards.Items.Count == 0)
                {
                    continue;
                }

                var loot = QuestRewardLoot.Build("shot:" + template.Id, template.Rewards, items);
                if (template.Rewards.Money > 0)
                {
                    return new StoryReward(loot, template.Rewards.Money);
                }

                fallback ??= new StoryReward(loot, 0);
            }

            return fallback ?? throw new InvalidOperationException(
                "В данных нет сюжетного квеста с предметами награды");
        }

        private static void FillGrid(
            GameObject panel,
            string panelName,
            UiFactory ui,
            SpriteCatalog? catalog,
            IReadOnlyList<TradeGridItem> items,
            string namePrefix,
            bool pack)
        {
            var panelTransform = FindDeep(panel.transform, panelName);
            if (panelTransform == null)
            {
                throw new InvalidOperationException($"В префабе нет панели {panelName}");
            }

            var scroll = panelTransform.GetComponentInChildren<ScrollArea>(true);
            if (scroll == null)
            {
                throw new InvalidOperationException($"В панели {panelName} нет области прокрутки");
            }

            var content = ReadField(scroll, "prefabContent") as RectTransform;
            var viewport = ReadField(scroll, "prefabViewport") as RectTransform;
            if (content == null || viewport == null)
            {
                throw new InvalidOperationException($"Область прокрутки {panelName} не настроена");
            }

            var height = TradeGridBuilder.Build(
                ui,
                content,
                viewport.rect.width,
                viewport.rect.height,
                items,
                Columns,
                1,
                null,
                null,
                iconId => catalog != null && catalog.TryGet(iconId, out var sprite) ? sprite : null,
                namePrefix,
                pack);
            content.sizeDelta = new Vector2(0f, height);
        }

        private static void SetText(GameObject panel, string path, string text)
        {
            var target = FindDeep(panel.transform, path);
            var label = target != null ? target.GetComponent<Text>() : null;
            if (label != null)
            {
                label.text = text;
            }
        }

        private static UnityEngine.Object? ReadField(UnityEngine.Object target, string field)
        {
            var serialized = new SerializedObject(target);
            var property = serialized.FindProperty(field);
            return property != null ? property.objectReferenceValue : null;
        }

        /// <summary>
        /// Гасит полосы прокрутки: в batch-режиме ScrollRect не обновляет размер ползунка,
        /// и полоса рисуется во всю высоту, даже когда сетка помещается.
        /// </summary>
        private static void SyncScrollbars(GameObject panel)
        {
            foreach (var scroll in panel.GetComponentsInChildren<ScrollRect>(true))
            {
                if (scroll.content == null || scroll.viewport == null)
                {
                    continue;
                }

                LayoutRebuilder.ForceRebuildLayoutImmediate(scroll.content);
                if (scroll.verticalScrollbar != null)
                {
                    scroll.verticalScrollbar.gameObject.SetActive(false);
                }
            }
        }

        private static void ApplyLabels(GameObject panel, IGameTexts texts)
        {
            foreach (var label in panel.GetComponentsInChildren<LocalizedText>(true))
            {
                label.Apply(texts);
            }
        }

        private static void FillBackdrop(GameObject panel) => ScreenBackdropAssets.ApplyForScreenshot(panel);

        private static Transform? FindDeep(Transform root, string name)
        {
            foreach (Transform child in root)
            {
                if (child.name == name)
                {
                    return child;
                }

                var nested = FindDeep(child, name);
                if (nested != null)
                {
                    return nested;
                }
            }

            return null;
        }

        /// <summary>Данные игрока и каталога предметов для подписей кадра.</summary>
        private sealed class PlayerStateReference
        {
            private readonly IGameTexts _texts;

            public PlayerStateReference(AnomalyPath.Core.Player.PlayerState player, IItemRepository items, IGameTexts texts)
            {
                Player = player;
                Items = items;
                _texts = texts;
            }

            public AnomalyPath.Core.Player.PlayerState Player { get; }

            public IItemRepository Items { get; }

            public int Money => Player.Money;

            public GridContainer Inventory => Player.Inventory;

            public string Describe(ItemInstance item)
            {
                var template = Items.TryGet(item.ItemId, out var found) ? found : null;
                var name = template != null ? _texts.Get(template.NameKey) : item.ItemId;
                if (item.Stack > 1)
                {
                    name += " ×" + item.Stack;
                }

                return name;
            }
        }

        /// <summary>Награда сюжетного квеста для кадра: предметы и деньги.</summary>
        private sealed class StoryReward
        {
            public StoryReward(IReadOnlyList<ItemInstance> loot, int money)
            {
                Loot = loot;
                Money = money;
            }

            public IReadOnlyList<ItemInstance> Loot { get; }

            public int Money { get; }
        }
    }
}
