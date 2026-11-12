#nullable enable
using UnityEngine;
using UnityEngine.UI;

namespace AnomalyPath.Runtime.Ui.Core
{
    /// <summary>
    /// Разметка кнопки-действия на общем кадре <c>ui_btn_mp_ind_empty/green</c>: где на кадре
    /// лежит металлическая табличка и как вписать в неё подпись. Доли сняты с самой текстуры
    /// (512×299): табличка занимает нижнюю часть кадра, выше проходят трубы, справа горит лампа.
    /// Поэтому центр кадра для подписи не годится — она ставится по центру таблички.
    /// </summary>
    public static class ActionButtonMetrics
    {
        /// <summary>Левый край светлого поля таблички: доля ширины кадра.</summary>
        public const float PlateLeft = 0.205f;

        /// <summary>
        /// Правый край светлого поля таблички: доля ширины кадра. Правее 0.80 табличка уходит
        /// в тень (яркость падает с 0.48 до 0.28), и подпись там не читается.
        /// </summary>
        public const float PlateRight = 0.82f;

        /// <summary>Верхний край металлической таблички: доля высоты кадра от верха.</summary>
        public const float PlateTop = 0.5f;

        /// <summary>Нижний край металлической таблички: доля высоты кадра от низа.</summary>
        public const float PlateBottom = 0.105f;

        /// <summary>Поле между подписью и краем таблички.</summary>
        public const float LabelPadding = 2f;

        /// <summary>На сколько прозрачная область клика выступает за прямоугольник кнопки.</summary>
        public const float HitAreaPadding = 16f;

        /// <summary>Кегль подписи кнопки-действия, пока она помещается в табличку.</summary>
        public const int LabelFontSize = 22;

        /// <summary>Предел уменьшения подписи: длинная надпись сжимается, но не пропадает.</summary>
        public const int LabelMinFontSize = 10;

        /// <summary>Какую долю ширины таблички занимает подпись: остальное — поле у кромки.</summary>
        public const float LabelFill = 0.86f;

        /// <summary>Прямоугольник металлической таблички внутри кнопки заданного размера.</summary>
        /// <param name="width">Ширина кнопки.</param>
        /// <param name="height">Высота кнопки.</param>
        /// <param name="padding">Отступ от края таблички внутрь; 0 — сама табличка.</param>
        /// <returns>Прямоугольник таблички в координатах кнопки.</returns>
        public static Rect PlateRect(float width, float height, float padding) => Rect.MinMaxRect(
            (PlateLeft * width) + padding,
            (PlateBottom * height) + padding,
            (PlateRight * width) - padding,
            ((1f - PlateTop) * height) - padding);

        /// <summary>
        /// Красит подпись как подпись кнопки-действия: песочный цвет и тёмная обводка.
        /// Нужна и текстам рядом с табличкой (цена перехода), чтобы они читались на кадре.
        /// </summary>
        /// <param name="label">Подпись.</param>
        public static void ApplyStyle(Text label)
        {
            label.color = ActionButtonPalette.Label;
            var outline = label.GetComponent<Outline>() ?? label.gameObject.AddComponent<Outline>();
            outline.effectColor = ActionButtonPalette.LabelOutline;
            outline.effectDistance = new Vector2(1f, -1f);
            outline.useGraphicAlpha = false;
        }

        /// <summary>
        /// Ставит подпись по центру металлической таблички кадра и вписывает её в табличку:
        /// якоря держат доли кадра, поэтому размер кнопки знать не нужно, а кегль подбирает
        /// <see cref="ActionButtonLabel"/> по ширине таблички.
        /// </summary>
        /// <param name="label">Подпись кнопки.</param>
        /// <param name="fontSize">Кегль, с которого начинается вписывание.</param>
        public static void PlaceLabel(Text label, int fontSize)
        {
            var rect = label.rectTransform;
            rect.anchorMin = new Vector2(PlateLeft, PlateBottom);
            rect.anchorMax = new Vector2(PlateRight, 1f - PlateTop);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(LabelPadding, LabelPadding);
            rect.offsetMax = new Vector2(-LabelPadding, -LabelPadding);
            label.fontSize = fontSize;
            label.alignment = TextAnchor.MiddleCenter;
            ApplyStyle(label);
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.resizeTextForBestFit = false;
            var fitter = label.GetComponent<ActionButtonLabel>() ??
                         label.gameObject.AddComponent<ActionButtonLabel>();
            fitter.Configure(fontSize, LabelMinFontSize);
        }
    }
}
