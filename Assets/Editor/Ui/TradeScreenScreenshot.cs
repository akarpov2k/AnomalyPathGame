#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using AnomalyPath.Core;
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Core.Combat;
using AnomalyPath.Core.Di;
using AnomalyPath.Core.Inventory;
using AnomalyPath.Core.Services;
using AnomalyPath.Core.Templates;
using AnomalyPath.Runtime.Screens.Trader;
using AnomalyPath.Runtime.Ui.Art;
using AnomalyPath.Runtime.Ui.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace AnomalyPath.Editor.Ui
{
    /// <summary>
    /// Снимает кадр окна торговли из собранного префаба: товары торговца слева, инвентарь
    /// игрока справа с деньгами на плашке, выбранный предмет и количество в середине.
    /// Данные для кадра берутся из настоящих данных игры: ассортимент первого торговца.
    /// </summary>
    public static class TradeScreenScreenshot
    {
        private const string PrefabPath = "Assets/Prefabs/Screens/TradeScreen.prefab";
        private const string OutputFolder = "Logs/Screenshots";
        private const string OutputFile = "trade.png";
        private const string DataRoot = "Assets/Data";
        private const string CatalogPath = "Assets/Art/SpriteCatalog.asset";
        private const int Width = 1920;
        private const int Height = 1080;
        private const int Columns = 6;

        /// <summary>Пункт меню: снять кадр окна торговли.</summary>
        [MenuItem("Tools/AnomalyPath/UI/Скриншот окна торговли")]
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

        /// <summary>Снимает кадр окна торговли в <c>Logs/Screenshots/trade.png</c>.</summary>
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
            var actors = services.Resolve<IActorRepository>();
            var catalog = AssetDatabase.LoadAssetAtPath<SpriteCatalog>(CatalogPath);
            var trader = FirstTrader(actors);
            Capture(panel => Fill(panel, texts, services, actors, trader, catalog));
            Debug.Log($"AnomalyPath: кадр окна торговли в {OutputFolder}/{OutputFile}");
        }

        private static void Capture(Action<GameObject> fill)
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

            var path = Path.Combine(OutputFolder, OutputFile);
            File.WriteAllBytes(path, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            target.Release();
            UnityEngine.Object.DestroyImmediate(target);
            UnityEngine.Object.DestroyImmediate(canvasObject);
            UnityEngine.Object.DestroyImmediate(cameraObject);
            Debug.Log($"AnomalyPath: скриншот {path}");
        }

        private static void Fill(
            GameObject panel,
            IGameTexts texts,
            ServiceContainer services,
            IActorRepository actors,
            ActorTemplate trader,
            SpriteCatalog? catalog)
        {
            ApplyLabels(panel, texts);
            SetText(panel, "Title", texts.Get(trader.NameKey));
            SetText(panel, "PlayerMoney", UiText.Format(texts, UiTextKey.Common_Money, 1000));

            var ui = new UiFactory(UiFactory.FindFont());
            var items = services.Resolve<IItemRepository>();
            var inventory = services.Resolve<IInventoryService>();
            var traderItems = BuildTraderItems(trader, texts, items);
            var playerTrader = OtherTrader(actors, trader);
            var playerItems = BuildPlayerItems(playerTrader, texts, items, inventory);
            FillGrid(panel, "TraderPanel", ui, catalog, traderItems, null, "TradeItem", pack: true);
            FillGrid(panel, "PlayerPanel", ui, catalog, playerItems, null, "PlayerItem", pack: false);

            if (traderItems.Count == 0)
            {
                return;
            }

            var selected = traderItems[0];
            var template = items.TryGet(selected.ItemId, out var found) ? found : null;
            var unitPrice = UnitPrice(trader, template);
            SetText(panel, "SelectedName", selected.Label);
            SetText(panel, "SelectedPrice", UiText.Format(texts, UiTextKey.Trade_Unit_Price, unitPrice));
            SetText(panel, "Quantity", "2");
            SetText(panel, "TradeAction/Label", UiText.Format(texts, UiTextKey.Trade_Buy_Total, unitPrice * 2L));
            SetIcon(panel, "SelectedIcon", selected.Icon, catalog);
        }

        private static List<TradeGridItem> BuildTraderItems(
            ActorTemplate trader,
            IGameTexts texts,
            IItemRepository items)
        {
            var list = new List<TradeGridItem>();
            foreach (var offer in trader.Trader!.Assortment)
            {
                if (!items.TryGet(offer.ItemId, out var template))
                {
                    continue;
                }

                list.Add(new TradeGridItem(
                    offer.ItemId,
                    offer.ItemId,
                    template.FootprintWidth,
                    template.FootprintHeight,
                    texts.Get(template.NameKey),
                    template.Icon,
                    offer.Stock));
            }

            return list;
        }

        private static List<TradeGridItem> BuildPlayerItems(
            ActorTemplate trader,
            IGameTexts texts,
            IItemRepository items,
            IInventoryService inventory)
        {
            var list = new List<TradeGridItem>();
            var grid = new GridContainer("preview", 8, 10);
            var index = 0;
            foreach (var offer in trader.Trader!.Assortment)
            {
                if (index >= 5)
                {
                    break;
                }

                if (!items.TryGet(offer.ItemId, out var template))
                {
                    continue;
                }

                inventory.TryAdd(grid, offer.ItemId, 1, out _);
                index++;
            }

            foreach (var item in grid.Items)
            {
                if (!items.TryGet(item.ItemId, out var template))
                {
                    continue;
                }

                list.Add(new TradeGridItem(
                    item.InstanceId,
                    item.ItemId,
                    item.PlacedWidth,
                    item.PlacedHeight,
                    texts.Get(template.NameKey),
                    template.Icon,
                    item.Stack,
                    item.X,
                    item.Y));
            }

            return list;
        }

        private static void FillGrid(
            GameObject panel,
            string panelName,
            UiFactory ui,
            SpriteCatalog? catalog,
            IReadOnlyList<TradeGridItem> items,
            string? selectedKey,
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

            var content = ReadContent(scroll);
            var viewport = ReadViewport(scroll);
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
                selectedKey,
                null,
                iconId => catalog != null && catalog.TryGet(iconId, out var sprite) ? sprite : null,
                namePrefix,
                pack);
            content.sizeDelta = new Vector2(0f, height);
        }

        private static void SetIcon(GameObject panel, string name, string? iconId, SpriteCatalog? catalog)
        {
            var transform = FindDeep(panel.transform, name);
            var image = transform?.GetComponent<Image>();
            if (image == null)
            {
                return;
            }

            var sprite = iconId != null && catalog != null && catalog.TryGet(iconId, out var found) ? found : null;
            image.sprite = sprite;
            image.enabled = sprite != null;
        }

        private static void SetText(GameObject panel, string path, string text)
        {
            var parts = path.Split('/');
            var current = FindDeep(panel.transform, parts[0]);
            for (var index = 1; index < parts.Length && current != null; index++)
            {
                current = FindDeep(current, parts[index]);
            }

            var label = current != null ? current.GetComponent<Text>() : null;
            if (label != null)
            {
                label.text = text;
            }
        }

        private static int UnitPrice(ActorTemplate trader, ItemTemplate? template)
        {
            if (!ItemPrices.TryGetBasePrice(template, true, out var basePrice))
            {
                return 0;
            }

            return (int)Math.Round(basePrice * trader.Trader!.PlayerBuyModifier, MidpointRounding.AwayFromZero);
        }

        private static ActorTemplate FirstTrader(IActorRepository actors)
        {
            foreach (var actor in actors.All)
            {
                if (actor.Trader != null && actor.Trader.Assortment.Count > 0)
                {
                    return actor;
                }
            }

            throw new InvalidOperationException("В данных нет торговца с ассортиментом");
        }

        private static ActorTemplate OtherTrader(IActorRepository actors, ActorTemplate trader)
        {
            foreach (var actor in actors.All)
            {
                if (!string.Equals(actor.Id, trader.Id, StringComparison.Ordinal) &&
                    actor.Trader != null &&
                    actor.Trader.Assortment.Count > 0)
                {
                    return actor;
                }
            }

            return trader;
        }

        private static RectTransform? ReadContent(ScrollArea scroll) =>
            ReadField(scroll, "prefabContent") as RectTransform;

        private static RectTransform? ReadViewport(ScrollArea scroll) =>
            ReadField(scroll, "prefabViewport") as RectTransform;

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
    }
}
