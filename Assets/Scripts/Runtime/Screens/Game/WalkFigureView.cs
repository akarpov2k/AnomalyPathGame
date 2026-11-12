#nullable enable
using UnityEngine;
using UnityEngine.UI;

namespace AnomalyPath.Runtime.Screens.Game
{
    /// <summary>Фигура на дороге режима похода: картинка, подпись и состояние «осмотрена».</summary>
    public sealed class WalkFigureView : MonoBehaviour
    {
        [Tooltip("Картинка объекта")]
        [SerializeField]
        private Image? icon = null;

        [Tooltip("Подпись объекта")]
        [SerializeField]
        private Text? label = null;

        /// <summary>Корень фигуры.</summary>
        public RectTransform Rect => (RectTransform)transform;

        /// <summary>Показывает фигуру заданной высоты.</summary>
        /// <param name="sprite">Картинка объекта; <c>null</c> — цветная плашка.</param>
        /// <param name="title">Подпись.</param>
        /// <param name="height">Высота картинки в единицах канваса.</param>
        public void Show(Sprite? sprite, string title, float height)
        {
            var maxHeight = Mathf.Clamp(height, 120f, 360f);
            var iconSize = FitIcon(sprite, maxHeight);
            if (icon != null)
            {
                var rect = icon.rectTransform;
                rect.anchorMin = new Vector2(0.5f, 0f);
                rect.anchorMax = new Vector2(0.5f, 0f);
                rect.pivot = new Vector2(0.5f, 0f);
                rect.anchoredPosition = Vector2.zero;
                rect.sizeDelta = iconSize;
                icon.sprite = sprite;
                icon.color = sprite != null ? Color.white : new Color(0.8f, 0.8f, 0.8f, 1f);
            }

            if (label != null)
            {
                label.text = title;
                label.color = Color.white;
            }

            Rect.sizeDelta = new Vector2(WalkMetrics.FigureWidth, iconSize.y);
            gameObject.SetActive(true);
        }

        /// <summary>
        /// Вписывает картинку в ширину фигуры с сохранением пропорций и ставит её
        /// низ на линию земли: широкий объект (машина) больше не висит над дорогой,
        /// потому что раньше картинка центрировалась в высоком прямоугольнике.
        /// </summary>
        /// <param name="sprite">Картинка объекта; <c>null</c> — прямоугольник фигуры.</param>
        /// <param name="maxHeight">Предельная высота картинки в единицах канваса.</param>
        /// <returns>Размер картинки.</returns>
        private static Vector2 FitIcon(Sprite? sprite, float maxHeight)
        {
            var sourceWidth = sprite != null ? sprite.rect.width : WalkMetrics.FigureWidth;
            var sourceHeight = sprite != null ? sprite.rect.height : maxHeight;
            sourceWidth = Mathf.Max(1f, sourceWidth);
            sourceHeight = Mathf.Max(1f, sourceHeight);
            var fit = Mathf.Min(WalkMetrics.FigureWidth / sourceWidth, maxHeight / sourceHeight);
            return new Vector2(sourceWidth * fit, sourceHeight * fit);
        }

        /// <summary>Ставит фигуру на дорогу.</summary>
        /// <param name="position">Позиция в мире.</param>
        public void SetPosition(Vector2 position) => Rect.anchoredPosition = position;

        /// <summary>Помечает фигуру осмотренной или возвращает обычный вид.</summary>
        /// <param name="used">Осмотрена ли фигура.</param>
        public void SetUsed(bool used)
        {
            var alpha = used ? 0.35f : 1f;
            if (icon != null)
            {
                var color = icon.color;
                color.a = alpha;
                icon.color = color;
            }

            if (label != null)
            {
                var color = label.color;
                color.a = alpha;
                label.color = color;
            }
        }

        /// <summary>Скрывает фигуру.</summary>
        public void Hide() => gameObject.SetActive(false);
    }
}
