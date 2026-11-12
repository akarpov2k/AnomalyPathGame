#nullable enable
using System;
using AnomalyPath.Core.Localization;
using AnomalyPath.Runtime.Ui.Core;
using UnityEngine;
using UnityEngine.UI;

namespace AnomalyPath.Runtime.Screens.Game
{
    /// <summary>
    /// Фигура на сцене локации из префаба. Два вида показа: прозрачная зона нажатия с названием
    /// и подсказкой, и сцена — вырезка объекта, стоящая низом на точке земли, с контактной
    /// тенью и свечением при наведении. Сцена берёт размер от высоты в метрах, поэтому
    /// одна картинка подходит любой локации; подписей у сцены нет.
    /// </summary>
    public sealed class PlayObject
    {
        private readonly GameObject _root;
        private readonly RectTransform _rect;
        private readonly Button _button;
        private readonly Image? _plate;
        private readonly Image? _shadow;
        private readonly Image? _glow;
        private readonly Image? _icon;
        private readonly Text? _title;
        private readonly Text? _hint;
        private readonly HoverHighlight _highlight;
        private readonly string _slotName;
        private readonly Vector2 _plateAnchorMin;
        private readonly Vector2 _plateAnchorMax;
        private readonly Vector2 _plateOffsetMin;
        private readonly Vector2 _plateOffsetMax;
        private readonly Vector2 _iconAnchorMin;
        private readonly Vector2 _iconAnchorMax;
        private readonly Vector2 _iconOffsetMin;
        private readonly Vector2 _iconOffsetMax;
        private Action? _click;

        /// <summary>Связывает фигуру с готовым объектом префаба.</summary>
        /// <param name="root">Корень фигуры с Image и Button.</param>
        /// <param name="slotName">Имя места в префабе: к нему возвращается скрытая фигура.</param>
        /// <exception cref="ArgumentNullException">Корень не задан.</exception>
        /// <exception cref="InvalidOperationException">У корня нет Image или Button.</exception>
        public PlayObject(GameObject root, string slotName)
        {
            _root = root != null ? root : throw new ArgumentNullException(nameof(root));
            _slotName = slotName;
            _rect = root.GetComponent<RectTransform>() ??
                    throw new InvalidOperationException(CoreText.Format(CoreErrors.UiButtonMissing, root.name));
            _button = root.GetComponent<Button>() ??
                      throw new InvalidOperationException(CoreText.Format(CoreErrors.UiButtonMissing, root.name));
            _plate = root.GetComponent<Image>();
            if (_plate != null)
            {
                // Корень фигуры — только зона нажатия: прямоугольная плашка вокруг объекта
                // читалась как рамка на сцене и мешала картинке локации.
                _plate.sprite = null;
                _plate.color = new Color(1f, 1f, 1f, 0f);
                _plate.type = Image.Type.Simple;
                _plate.preserveAspect = false;
                _plate.raycastTarget = true;
            }

            _shadow = Find<Image>("Shadow") ?? _plate;
            _glow = Find<Image>("Glow");
            _icon = Find<Image>("Icon");
            _title = Find<Text>("Title");
            _hint = Find<Text>("Hint");
            _highlight = root.GetComponent<HoverHighlight>() ?? root.AddComponent<HoverHighlight>();
            _highlight.enabled = false;

            _plateAnchorMin = _rect.anchorMin;
            _plateAnchorMax = _rect.anchorMax;
            _plateOffsetMin = _rect.offsetMin;
            _plateOffsetMax = _rect.offsetMax;
            if (_icon != null)
            {
                var iconRect = _icon.rectTransform;
                _iconAnchorMin = iconRect.anchorMin;
                _iconAnchorMax = iconRect.anchorMax;
                _iconOffsetMin = iconRect.offsetMin;
                _iconOffsetMax = iconRect.offsetMax;
            }

            if (_glow != null)
            {
                _glow.sprite = PlayArt.Glow();
                _glow.color = PlayPalette.SceneGlow;
                _glow.preserveAspect = false;
                _glow.raycastTarget = false;
                _glow.gameObject.SetActive(false);
            }

            if (_shadow != null && _shadow != _plate)
            {
                _shadow.sprite = PlayArt.Glow();
                _shadow.color = PlayPalette.SceneShadow;
                _shadow.preserveAspect = false;
                _shadow.raycastTarget = false;
                _shadow.gameObject.SetActive(false);
            }

            _button.onClick.AddListener(Click);
        }

        /// <summary>Показывает плашку с названием, подсказкой и обработчиком.</summary>
        /// <param name="title">Название объекта.</param>
        /// <param name="hint">Подсказка действия.</param>
        /// <param name="click">Что выполнить по нажатию.</param>
        /// <param name="enabled">Кликабелен ли объект.</param>
        /// <param name="objectName">Имя объекта в иерархии по id спавна; <c>null</c> — оставить прежнее.</param>
        /// <param name="icon">Текстура объекта; <c>null</c> — только эллипс-заглушка.</param>
        public void Show(
            string title,
            string hint,
            Action click,
            bool enabled = true,
            string? objectName = null,
            Sprite? icon = null)
        {
            if (!string.IsNullOrEmpty(objectName))
            {
                _root.name = objectName;
            }

            RestorePlate();

            if (_title != null)
            {
                _title.gameObject.SetActive(true);
                _title.text = title;
            }

            if (_hint != null)
            {
                _hint.gameObject.SetActive(true);
                _hint.text = hint;
            }

            if (_icon != null)
            {
                _icon.sprite = icon;
                _icon.gameObject.SetActive(icon != null);
            }

            _click = click;
            _button.interactable = enabled;
            _root.SetActive(true);
        }

        /// <summary>
        /// Показывает вырезку объекта на сцене: низ на точке земли, размер от высоты фигуры,
        /// под фигурой контактная тень, при наведении — свечение и осветление. Подписей нет.
        /// </summary>
        /// <param name="art">Картинка объекта (одна вырезка на сущность).</param>
        /// <param name="ground">Точка земли в единицах канваса сцены.</param>
        /// <param name="height">Высота фигуры в единицах канваса сцены.</param>
        /// <param name="tint">Цвет подгонки фигуры под тон сцены.</param>
        /// <param name="click">Что выполнить по нажатию.</param>
        /// <param name="enabled">Кликабелен ли объект.</param>
        /// <param name="objectName">Имя объекта в иерархии по id спавна; <c>null</c> — оставить прежнее.</param>
        /// <exception cref="ArgumentNullException">Картинка не задана.</exception>
        public void ShowScene(
            Sprite art,
            Vector2 ground,
            float height,
            Color tint,
            Action click,
            bool enabled = true,
            string? objectName = null)
        {
            if (art == null)
            {
                throw new ArgumentNullException(nameof(art));
            }

            if (!string.IsNullOrEmpty(objectName))
            {
                _root.name = objectName;
            }

            if (_title != null)
            {
                _title.text = string.Empty;
                _title.gameObject.SetActive(false);
            }

            if (_hint != null)
            {
                _hint.text = string.Empty;
                _hint.gameObject.SetActive(false);
            }

            var aspect = art.rect.height > 0f ? art.rect.width / art.rect.height : 1f;
            var figure = StagePlacement.Figure(height, aspect, ground);
            SetRect(_rect, figure, new Vector2(0.5f, 0f));

            if (_plate != null)
            {
                if (_shadow == _plate)
                {
                    // Старый префаб без слоя Shadow: плашка становится прозрачной зоной нажатия,
                    // контактной тени нет — она появится после пересборки префаба.
                    _plate.gameObject.SetActive(true);
                    _plate.sprite = null;
                    _plate.color = new Color(1f, 1f, 1f, 0f);
                    _plate.raycastTarget = true;
                }
                else
                {
                    // Плашка — это корневая картинка фигуры: гасим её цвет, но не объект,
                    // иначе вместе с ней скрылись бы вырезка и тень.
                    _plate.sprite = null;
                    _plate.color = new Color(1f, 1f, 1f, 0f);
                    _plate.raycastTarget = false;
                }
            }

            if (_shadow != null && _shadow != _plate)
            {
                var shadowRect = StagePlacement.Shadow(
                    figure,
                    PlayMetrics.SceneShadowWidth,
                    PlayMetrics.SceneShadowHeight,
                    PlayMetrics.SceneShadowSink);
                PlaceChild(_shadow.rectTransform, shadowRect, figure, new Vector2(0.5f, 0f));
                _shadow.gameObject.SetActive(true);
            }

            if (_glow != null)
            {
                var glowRect = StagePlacement.Glow(figure, PlayMetrics.SceneGlowPadding);
                PlaceChild(_glow.rectTransform, glowRect, figure, new Vector2(0.5f, 0.5f));
                _glow.color = PlayPalette.SceneGlow;
                _glow.gameObject.SetActive(false);
            }

            if (_icon != null)
            {
                _icon.sprite = art;
                _icon.color = tint;
                _icon.type = Image.Type.Simple;
                _icon.preserveAspect = false;
                _icon.raycastTarget = true;
                _icon.rectTransform.anchorMin = Vector2.zero;
                _icon.rectTransform.anchorMax = Vector2.one;
                _icon.rectTransform.offsetMin = Vector2.zero;
                _icon.rectTransform.offsetMax = Vector2.zero;
                _icon.gameObject.SetActive(true);
            }

            var graphic = _icon ?? (Graphic?)_plate;
            if (graphic != null)
            {
                _highlight.enabled = true;
                _highlight.Configure(
                    graphic,
                    _glow?.gameObject,
                    tint,
                    PlayPalette.SceneHighlight(tint),
                    PlayMetrics.SceneHoverScale);
                _highlight.SetNormalColor(tint);
            }

            _click = click;
            _button.interactable = enabled;
            _root.SetActive(true);
        }

        /// <summary>Скрывает фигуру: у локации может не быть объекта.</summary>
        public void Hide()
        {
            _click = null;
            _highlight.enabled = false;
            _highlight.SetHighlighted(false);
            _root.name = _slotName;
            _root.SetActive(false);
        }

        private void RestorePlate()
        {
            _rect.anchorMin = _plateAnchorMin;
            _rect.anchorMax = _plateAnchorMax;
            _rect.offsetMin = _plateOffsetMin;
            _rect.offsetMax = _plateOffsetMax;

            _highlight.enabled = false;
            _highlight.SetHighlighted(false);

            if (_plate != null)
            {
                _plate.gameObject.SetActive(true);
                _plate.sprite = null;
                _plate.color = new Color(1f, 1f, 1f, 0f);
                _plate.type = Image.Type.Simple;
                _plate.preserveAspect = false;
                _plate.raycastTarget = true;
            }

            if (_shadow != null && _shadow != _plate)
            {
                _shadow.gameObject.SetActive(false);
            }

            if (_glow != null)
            {
                _glow.gameObject.SetActive(false);
            }

            if (_icon != null)
            {
                var iconRect = _icon.rectTransform;
                iconRect.anchorMin = _iconAnchorMin;
                iconRect.anchorMax = _iconAnchorMax;
                iconRect.offsetMin = _iconOffsetMin;
                iconRect.offsetMax = _iconOffsetMax;
                _icon.preserveAspect = true;
                _icon.raycastTarget = false;
            }
        }

        private static void SetRect(RectTransform rect, Rect area, Vector2 pivot)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.pivot = pivot;
            rect.anchoredPosition = new Vector2(area.x + (area.width * pivot.x), area.y);
            rect.sizeDelta = area.size;
        }

        /// <summary>
        /// Ставит дочерний слой по прямоугольнику сцены. Начало координат родителя —
        /// низ по центру фигуры, поэтому точка опоры слоя переводится в её локальные координаты.
        /// </summary>
        private static void PlaceChild(RectTransform child, Rect area, Rect figure, Vector2 pivot)
        {
            child.anchorMin = Vector2.zero;
            child.anchorMax = Vector2.zero;
            child.pivot = pivot;
            child.anchoredPosition = new Vector2(
                area.x + (area.width * pivot.x) - figure.center.x,
                area.y + (area.height * pivot.y) - figure.y);
            child.sizeDelta = area.size;
        }

        private void Click() => _click?.Invoke();

        private T? Find<T>(string name)
            where T : Component
        {
            var child = _root.transform.Find(name);
            return child == null ? null : child.GetComponent<T>();
        }
    }
}
