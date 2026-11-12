#nullable enable
using AnomalyPath.Runtime.Screens.Game.Hub;
using AnomalyPath.Runtime.Ui.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace AnomalyPath.Editor.Ui
{
    /// <summary>
    /// Часть класса <see cref="HubScreenPrefabBuilder"/>: картинка торговца и стойка,
    /// дверь, значки, часы и подсказка.
    /// </summary>
    public static partial class HubScreenPrefabBuilder
    {
        /// <summary>Спрайты экрана: значки из <c>Assets/Art/Icons</c>; <c>null</c> — рисуются в памяти.</summary>
        private sealed class Icons
        {
            /// <summary>Шестерёнка меню.</summary>
            public Sprite? Gear;

            /// <summary>Мешочек торговцев.</summary>
            public Sprite? Bag;

            /// <summary>Полумесяц отдыха.</summary>
            public Sprite? Rest;

            /// <summary>Рюкзак инвентаря.</summary>
            public Sprite? Backpack;

            /// <summary>Гаечный ключ ремонта снаряжения.</summary>
            public Sprite? Wrench;

            /// <summary>Циферблат часов.</summary>
            public Sprite? ClockFace;

            /// <summary>Стрелка часов.</summary>
            public Sprite? ClockHand;
        }

        private static Icons LoadIcons()
        {
            return new Icons
            {
                Gear = LoadIcon("ui_hub_gear"),
                Bag = LoadIcon("ui_hub_bag"),
                Rest = LoadIcon("ui_hub_rest"),
                Backpack = LoadIcon("ui_hub_backpack"),
                Wrench = LoadIcon("ui_hub_wrench"),
                ClockFace = LoadIcon("ui_clock_face"),
                ClockHand = LoadIcon("ui_clock_hand")
            };
        }

        /// <summary>Картинки объекта хаба: обычный вид и тот же объект со свечением.</summary>
        private sealed class ObjectArt
        {
            /// <summary>Создаёт пару картинок объекта.</summary>
            /// <param name="normal">Спрайт без свечения.</param>
            /// <param name="glow">Спрайт того же объекта со свечением.</param>
            public ObjectArt(Sprite? normal, Sprite? glow)
            {
                Normal = normal;
                Glow = glow;
            }

            /// <summary>Спрайт объекта без свечения.</summary>
            public Sprite? Normal { get; }

            /// <summary>Спрайт того же объекта со свечением.</summary>
            public Sprite? Glow { get; }
        }

        /// <summary>Берёт пару картинок объекта из <c>Assets/Art/Hub</c>: <c>&lt;id&gt;.png</c> и <c>&lt;id&gt;_glow.png</c>.</summary>
        private static ObjectArt LoadObjectArt(string id)
        {
            var normal = LoadSpriteAsset(HubArtFolder + "/" + id + ".png");
            var glow = LoadSpriteAsset(HubArtFolder + "/" + id + "_glow.png");
            if (normal == null || glow == null)
            {
                Debug.LogWarning($"AnomalyPath: нет картинок объекта {id} в {HubArtFolder} — объект будет без свечения");
            }

            return new ObjectArt(normal, glow);
        }

        /// <summary>Загружает спрайт после синхронного импорта: в batch-режиме база ещё не готова.</summary>
        private static Sprite? LoadSpriteAsset(string path)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite != null)
            {
                return sprite;
            }

            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                if (asset is Sprite found)
                {
                    return found;
                }
            }

            return null;
        }

        private static Sprite? LoadIcon(string id)
        {
            var path = IconsFolder + "/" + id + ".png";
            var sprite = LoadSpriteAsset(path);
            if (sprite == null)
            {
                Debug.LogWarning($"AnomalyPath: нет значка {path} — экран нарисует его в памяти");
            }

            return sprite;
        }

        private static Sprite? LoadSprite(string path)
        {
            var sprite = LoadSpriteAsset(path);
            if (sprite == null)
            {
                Debug.LogWarning($"AnomalyPath: нет спрайта {path}");
            }

            return sprite;
        }

        /// <summary>
        /// Торговец за стойкой: картинка в полный рост и прозрачная кликабельная зона
        /// от головы до линии стола. Картинку подменяет код по <c>hubPortrait</c> актёра.
        /// </summary>
        private static void BuildMerchant(
            RectTransform stage,
            HubTooltip tooltip,
            out Image body,
            out Button button,
            out HubZone zone)
        {
            body = CreateImage(stage, "Merchant", Color.white);
            var bodyRect = body.rectTransform;
            bodyRect.anchorMin = new Vector2(0.5f, 1f);
            bodyRect.anchorMax = new Vector2(0.5f, 1f);
            bodyRect.pivot = new Vector2(0.5f, 1f);
            bodyRect.anchoredPosition = new Vector2(0f, -MerchantTop);
            bodyRect.sizeDelta = new Vector2(MerchantWidth, MerchantHeight);
            body.sprite = LoadSprite(DefaultMerchantPath);
            body.color = HubPalette.MerchantTint;
            body.preserveAspect = true;

            var go = new GameObject(
                "MerchantHit",
                typeof(RectTransform),
                typeof(Image),
                typeof(Button),
                typeof(HubZone));
            go.transform.SetParent(stage, false);
            var hit = (RectTransform)go.transform;
            hit.anchorMin = new Vector2(0.5f, 1f);
            hit.anchorMax = new Vector2(0.5f, 1f);
            hit.pivot = new Vector2(0.5f, 1f);
            hit.anchoredPosition = new Vector2(0f, -MerchantTop);
            hit.sizeDelta = new Vector2(MerchantHitWidth, MerchantHitHeight);

            var image = go.GetComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0f);
            image.raycastTarget = true;

            button = go.GetComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.None;

            zone = go.GetComponent<HubZone>();
            SetString(zone, "nameKey", string.Empty);
            SetObject(zone, "tooltip", tooltip);
        }

        /// <summary>Полоса переднего стола внизу сцены: закрывает низ фигуры торговца.</summary>
        private static void BuildCounter(RectTransform stage)
        {
            var counter = CreateImage(stage, "Counter", Color.white);
            var rect = counter.rectTransform;
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = new Vector2(0f, CounterHeight);
            counter.sprite = LoadSprite(CounterPath);
            counter.preserveAspect = false;
        }

        /// <summary>
        /// Объект на картинке хаба: прозрачная кнопка на месте предмета, его картинка без свечения
        /// и пара спрайтов для подмены при наведении, плюс подсказка с названием.
        /// </summary>
        private static Button BuildZone(
            RectTransform stage,
            string name,
            string key,
            float left,
            float right,
            float bottom,
            float top,
            ObjectArt objectArt,
            HubTooltip tooltip)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(HubZone));
            go.transform.SetParent(stage, false);
            var rect = (RectTransform)go.transform;
            PlaceFraction(rect, left, bottom, right, top);

            var image = go.GetComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0f);
            image.raycastTarget = true;

            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.None;

            var art = CreateImage(rect, "Object", Color.white);
            Stretch(art.rectTransform);
            art.sprite = objectArt.Normal;
            art.preserveAspect = true;

            var zone = go.GetComponent<HubZone>();
            SetString(zone, "nameKey", key);
            zone.Configure(art, objectArt.Normal, objectArt.Glow);
            SetObject(zone, "tooltip", tooltip);
            return button;
        }

        /// <summary>Кнопка-значок над картинкой: светлый значок с тенью, без квадратной плашки.</summary>
        private static Button BuildIconButton(
            RectTransform stage,
            string name,
            Sprite? icon,
            string key,
            HubTooltip tooltip,
            Vector2 anchor,
            Vector2 pivot,
            Vector2 position)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(HubZone));
            go.transform.SetParent(stage, false);
            var rect = (RectTransform)go.transform;
            Place(rect, anchor, pivot, position, new Vector2(HudSize, HudSize));

            var image = go.GetComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0f);
            image.raycastTarget = true;

            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.None;

            var art = CreateImage(rect, "Icon", Color.white);
            var artRect = art.rectTransform;
            artRect.anchorMin = new Vector2(0.5f, 0.5f);
            artRect.anchorMax = new Vector2(0.5f, 0.5f);
            artRect.pivot = new Vector2(0.5f, 0.5f);
            artRect.anchoredPosition = Vector2.zero;
            artRect.sizeDelta = new Vector2(IconSize, IconSize);
            art.sprite = icon;
            art.preserveAspect = true;
            var shadow = art.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.8f);
            shadow.effectDistance = new Vector2(2f, -2f);

            // Подсветка значка при наведении и выборе: без неё кнопка не отвечала на курсор
            // и выглядела неинтуитивной. Масштаб — у корня кнопки, цвет — у значка.
            var hover = go.GetComponent<HoverHighlight>() ?? go.AddComponent<HoverHighlight>();
            hover.Configure(
                art,
                null,
                Color.white,
                new Color(1f, 0.93f, 0.78f, 1f),
                HubMetrics.IconHoverScale);

            var zone = go.GetComponent<HubZone>();
            SetString(zone, "nameKey", key);
            SetObject(zone, "tooltip", tooltip);
            return button;
        }

        private static void BuildClock(
            RectTransform stage,
            Icons icons,
            out RectTransform hand,
            out Text time)
        {
            var group = CreateRect(stage, "Clock");
            Place(
                group,
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(ClockLeft, -ClockTop),
                new Vector2(ClockSize, ClockSize));

            var face = CreateImage(group, "ClockFace", Color.white);
            Stretch(face.rectTransform);
            face.sprite = icons.ClockFace;
            face.preserveAspect = true;

            var handImage = CreateImage(face.transform, "ClockHand", Color.white);
            hand = handImage.rectTransform;
            hand.anchorMin = new Vector2(0.5f, 0.5f);
            hand.anchorMax = new Vector2(0.5f, 0.5f);
            hand.pivot = new Vector2(0.5f, 0.5f);
            hand.anchoredPosition = Vector2.zero;
            hand.sizeDelta = new Vector2(ClockSize, ClockSize);
            handImage.sprite = icons.ClockHand;
            handImage.preserveAspect = true;

            time = CreateText(stage, "ClockTime", string.Empty, 24, TextAnchor.MiddleLeft, Color.white);
            Place(
                time.rectTransform,
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(ClockLeft + ClockSize + ClockTimeGap, -ClockTop),
                new Vector2(280f, 70f));
        }

        private static HubTooltip BuildTooltip(RectTransform stage)
        {
            var go = new GameObject("Tooltip", typeof(RectTransform), typeof(CanvasGroup));
            go.transform.SetParent(stage, false);
            var rect = (RectTransform)go.transform;
            Place(
                rect,
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -HubMetrics.TooltipTop),
                new Vector2(TooltipWidth, TooltipHeight));

            var group = go.GetComponent<CanvasGroup>();
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;

            var plate = CreateImage(rect, "Backplate", HubPalette.Tooltip);
            Stretch(plate.rectTransform);

            var label = CreateText(
                rect,
                "Label",
                string.Empty,
                HubMetrics.TooltipFontSize,
                TextAnchor.MiddleCenter,
                Color.white);
            Stretch(label.rectTransform);

            var tooltip = go.AddComponent<HubTooltip>();
            SetObject(tooltip, "group", group);
            SetObject(tooltip, "label", label);
            return tooltip;
        }
    }
}
