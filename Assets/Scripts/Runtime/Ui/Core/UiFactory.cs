#nullable enable
using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace AnomalyPath.Runtime.Ui.Core
{
    /// <summary>Создаёт элементы uGUI для экранов игры: панели, колонки, тексты, кнопки, числовые поля.</summary>
    public sealed class UiFactory
    {
        /// <summary>Цвет фона панели.</summary>
        public static readonly Color BackgroundColor = new Color(0.06f, 0.08f, 0.09f, 1f);

        /// <summary>Цвет кнопки.</summary>
        public static readonly Color ButtonColor = new Color(0.18f, 0.24f, 0.23f, 1f);

        /// <summary>Основной цвет текста.</summary>
        public static readonly Color TextColor = new Color(0.9f, 0.92f, 0.9f);

        /// <summary>Приглушённый цвет текста.</summary>
        public static readonly Color MutedTextColor = new Color(0.7f, 0.75f, 0.72f);

        /// <summary>Цвет всплывающего сообщения.</summary>
        public static readonly Color ToastColor = new Color(0.95f, 0.85f, 0.55f);

        /// <summary>Шрифт интерфейса в Resources: <c>Assets/Resources/Fonts/Play-Regular.ttf</c>.</summary>
        private const string FontResource = "Fonts/Play-Regular";

        /// <summary>Жирное начертание шрифта интерфейса: <c>Assets/Resources/Fonts/Play-Bold.ttf</c>.</summary>
        private const string BoldFontResource = "Fonts/Play-Bold";

        private readonly Font _font;
        private readonly Sprite? _buttonNormal;
        private readonly Sprite? _buttonHighlighted;

        /// <summary>Создаёт фабрику с готовым шрифтом и кадрами кнопок-действий.</summary>
        /// <param name="font">Шрифт интерфейса.</param>
        /// <param name="buttonNormal">Кадр кнопки без подсветки; <c>null</c> — кнопки-действия строятся плашкой.</param>
        /// <param name="buttonHighlighted">Кадр кнопки с зелёным индикатором при наведении.</param>
        public UiFactory(Font font, Sprite? buttonNormal = null, Sprite? buttonHighlighted = null)
        {
            _font = font;
            _buttonNormal = buttonNormal;
            _buttonHighlighted = buttonHighlighted;
        }

        /// <summary>Шрифт интерфейса.</summary>
        public Font Font => _font;

        /// <summary>Берёт шрифт игры, а если его нет — встроенный, затем системный.</summary>
        /// <returns>Шрифт для текстов интерфейса.</returns>
        public static Font FindFont() => LoadFont(FontResource);

        /// <summary>Берёт жирное начертание шрифта игры: названия игры и заголовки.</summary>
        /// <returns>Жирный шрифт интерфейса.</returns>
        public static Font FindBoldFont() => LoadFont(BoldFontResource);

        private static Font LoadFont(string resource)
        {
            var font = Resources.Load<Font>(resource);
            if (font != null)
            {
                return font;
            }

            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null)
            {
                font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            return font != null ? font : Font.CreateDynamicFontFromOSFont("Arial", 16);
        }

        /// <summary>Создаёт полноэкранную панель с фоном; панель неактивна.</summary>
        /// <param name="name">Имя объекта панели.</param>
        /// <param name="parent">Родитель, обычно канвас.</param>
        /// <returns>Созданная панель.</returns>
        public GameObject CreatePanel(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            go.GetComponent<Image>().color = BackgroundColor;
            go.SetActive(false);
            return go;
        }

        /// <summary>Создаёт вертикальную колонку по центру панели.</summary>
        /// <param name="parent">Родитель.</param>
        /// <param name="width">Ширина колонки.</param>
        /// <returns>Трансформ колонки.</returns>
        public Transform CreateColumn(Transform parent, float width)
        {
            var go = new GameObject("Column", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(width, 0);
            var layout = go.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(20, 20, 20, 20);
            layout.spacing = 10;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;
            go.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return go.transform;
        }

        /// <summary>Создаёт заголовок экрана.</summary>
        /// <param name="parent">Родитель.</param>
        /// <param name="text">Текст заголовка.</param>
        /// <returns>Компонент текста.</returns>
        public Text CreateTitle(Transform parent, string text)
        {
            var label = CreateText(parent, text, 40, TextAnchor.MiddleCenter);
            label.gameObject.AddComponent<LayoutElement>().preferredHeight = 70;
            return label;
        }

        /// <summary>Создаёт текст основным цветом.</summary>
        /// <param name="parent">Родитель.</param>
        /// <param name="text">Текст.</param>
        /// <param name="size">Размер шрифта.</param>
        /// <param name="anchor">Выравнивание.</param>
        /// <returns>Компонент текста.</returns>
        public Text CreateText(Transform parent, string text, int size, TextAnchor anchor) =>
            CreateText(parent, text, size, anchor, TextColor);

        /// <summary>Создаёт текст заданного цвета.</summary>
        /// <param name="parent">Родитель.</param>
        /// <param name="text">Текст.</param>
        /// <param name="size">Размер шрифта.</param>
        /// <param name="anchor">Выравнивание.</param>
        /// <param name="color">Цвет.</param>
        /// <returns>Компонент текста.</returns>
        public Text CreateText(Transform parent, string text, int size, TextAnchor anchor, Color color)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var label = go.GetComponent<Text>();
            label.font = _font;
            label.fontSize = size;
            label.text = text;
            label.alignment = anchor;
            label.color = color;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            go.AddComponent<LayoutElement>().preferredHeight = size + 12;
            return label;
        }

        /// <summary>Создаёт кнопку с подписью; имя объекта — подпись.</summary>
        /// <param name="parent">Родитель.</param>
        /// <param name="text">Подпись кнопки.</param>
        /// <param name="action">Что выполнить по нажатию; <c>null</c> — подписку делает вызывающий.</param>
        /// <returns>Компонент кнопки.</returns>
        public Button CreateButton(Transform parent, string text, Action? action = null)
        {
            var go = new GameObject(text, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = ButtonColor;
            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => action?.Invoke());
            go.AddComponent<LayoutElement>().preferredHeight = 54;

            var label = CreateText(go.transform, text, 22, TextAnchor.MiddleCenter);
            var rect = label.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            AddHitArea(button);
            return button;
        }

        /// <summary>Добавляет прозрачную область на весь прямоугольник кнопки: клик работает не только по плашке.</summary>
        /// <param name="button">Кнопка, которой нужна вся площадь.</param>
        private static void AddHitArea(Button button)
        {
            var hit = new GameObject("HitArea", typeof(RectTransform), typeof(Image));
            hit.transform.SetParent(button.transform, false);
            var rect = (RectTransform)hit.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(-ActionButtonMetrics.HitAreaPadding, -ActionButtonMetrics.HitAreaPadding);
            rect.offsetMax = new Vector2(ActionButtonMetrics.HitAreaPadding, ActionButtonMetrics.HitAreaPadding);
            var image = hit.GetComponent<Image>();
            image.color = Color.clear;
            image.raycastTarget = true;
            hit.transform.SetAsLastSibling();
        }

        /// <summary>
        /// Создаёт кнопку-действие: кадр с пустым индикатором, а при наведении — с зелёным.
        /// Без кадров кнопка строится прежней плашкой.
        /// </summary>
        /// <param name="parent">Родитель.</param>
        /// <param name="text">Подпись кнопки.</param>
        /// <param name="action">Что выполнить по нажатию; <c>null</c> — подписку делает вызывающий.</param>
        /// <returns>Компонент кнопки с подсветкой индикатора.</returns>
        public Button CreateActionButton(Transform parent, string text, Action? action = null)
        {
            var button = CreateButton(parent, text, action);
            if (_buttonNormal == null)
            {
                return button;
            }

            var image = button.GetComponent<Image>();
            image.sprite = _buttonNormal;
            image.color = Color.white;
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
            var art = button.gameObject.AddComponent<ButtonArt>();
            art.Configure(image, _buttonNormal, _buttonHighlighted);
            var label = button.GetComponentInChildren<Text>();
            if (label != null)
            {
                ActionButtonMetrics.PlaceLabel(label, ActionButtonMetrics.LabelFontSize);
            }

            return button;
        }

        /// <summary>Создаёт числовое поле с вводом цифр и кнопками шага.</summary>
        /// <param name="parent">Родитель.</param>
        /// <param name="name">Имя объекта строки; от него же называются кнопки и поле ввода.</param>
        /// <param name="min">Минимум диапазона.</param>
        /// <param name="max">Максимум диапазона.</param>
        /// <param name="value">Начальное значение.</param>
        /// <param name="step">Шаг кнопок.</param>
        /// <returns>Компонент числового поля.</returns>
        public NumberField CreateNumberField(
            Transform parent,
            string name,
            double min,
            double max,
            double value,
            double step)
        {
            var row = new GameObject(name, typeof(RectTransform), typeof(HorizontalLayoutGroup));
            row.transform.SetParent(parent, false);
            var layout = row.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 8;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;
            row.AddComponent<LayoutElement>().preferredHeight = 54;

            var input = CreateInputField(
                row.transform,
                "0.0",
                value.ToString("0.0", CultureInfo.InvariantCulture),
                8,
                name + "Input");
            var inputElement = input.GetComponent<LayoutElement>();
            inputElement.preferredWidth = 120;
            inputElement.flexibleWidth = 1;

            var increase = CreateStepButton(row.transform, name + "Increase", "▲");
            var decrease = CreateStepButton(row.transform, name + "Decrease", "▼");

            var field = row.AddComponent<NumberField>();
            field.Configure(input, increase, decrease, min, max, step, value);
            return field;
        }

        /// <summary>Создаёт квадратную кнопку шага; подписку на нажатие делает числовое поле.</summary>
        /// <param name="parent">Родитель.</param>
        /// <param name="name">Имя объекта в иерархии.</param>
        /// <param name="label">Символ на кнопке.</param>
        /// <returns>Компонент кнопки.</returns>
        private Button CreateStepButton(Transform parent, string name, string label)
        {
            var button = CreateButton(parent, label);
            button.gameObject.name = name;
            var element = button.GetComponent<LayoutElement>();
            element.preferredWidth = 54;
            element.flexibleWidth = 0;
            return button;
        }

        /// <summary>Создаёт поле ввода с подсказкой (на новом Input System).</summary>
        /// <param name="parent">Родитель.</param>
        /// <param name="placeholder">Текст-подсказка, когда поле пустое.</param>
        /// <param name="value">Начальное значение.</param>
        /// <param name="maxLength">Предел длины.</param>
        /// <param name="name">Имя объекта в иерархии.</param>
        /// <returns>Компонент поля ввода.</returns>
        public TextInputField CreateInputField(
            Transform parent,
            string placeholder,
            string value,
            int maxLength = 24,
            string name = "Input")
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(TextInputField));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = new Color(0.16f, 0.18f, 0.18f, 1f);
            go.AddComponent<LayoutElement>().preferredHeight = 54;

            var text = CreateText(go.transform, value, 22, TextAnchor.MiddleLeft);
            StretchInputText(text.rectTransform);
            text.supportRichText = false;
            text.raycastTarget = false;

            var hint = CreateText(go.transform, placeholder, 22, TextAnchor.MiddleLeft, MutedTextColor);
            StretchInputText(hint.rectTransform);
            hint.raycastTarget = false;

            var input = go.GetComponent<TextInputField>();
            input.Configure(text, hint, image, maxLength);
            input.SetText(value);
            return input;
        }

        private static void StretchInputText(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(14f, 6f);
            rect.offsetMax = new Vector2(-14f, -6f);
        }

        /// <summary>Создаёт изображение.</summary>
        /// <param name="name">Имя объекта.</param>
        /// <param name="parent">Родитель.</param>
        /// <param name="color">Цвет.</param>
        /// <returns>Компонент изображения.</returns>
        public Image CreateImage(string name, Transform parent, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = color;
            return image;
        }
    }
}
