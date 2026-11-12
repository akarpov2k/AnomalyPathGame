#nullable enable
using UnityEngine;
using UnityEngine.UI;

namespace AnomalyPath.Runtime.Screens.Game
{
    /// <summary>Ответвление от дороги: уходящая в глубину тропа, объект-цель и подпись.</summary>
    public sealed class WalkBranchView : MonoBehaviour
    {
        [Tooltip("Тропа в глубину сцены")]
        [SerializeField]
        private Image? path = null;

        [Tooltip("Объект перехода — вырезка локации")]
        [SerializeField]
        private Image? objectImage = null;

        [Tooltip("Подпись зоны")]
        [SerializeField]
        private Text? label = null;

        [Tooltip("Стрелка-заглушка, когда картинки объекта нет")]
        [SerializeField]
        private Text? arrow = null;

        /// <summary>Корень указателя.</summary>
        public RectTransform Rect => (RectTransform)transform;

        /// <summary>Показывает ответвление.</summary>
        /// <param name="objectSprite">Вырезка локации-цели; <c>null</c> — только тропа и подпись.</param>
        /// <param name="pathSprite">Тропа в глубину; <c>null</c> — цветная плашка.</param>
        /// <param name="title">Название зоны.</param>
        public void Show(Sprite? objectSprite, Sprite? pathSprite, string title)
        {
            if (path != null)
            {
                path.sprite = pathSprite;
                path.color = Color.white;
                path.gameObject.SetActive(pathSprite != null);
            }

            if (objectImage != null)
            {
                var rect = objectImage.rectTransform;
                rect.anchorMin = new Vector2(0.5f, 0f);
                rect.anchorMax = new Vector2(0.5f, 0f);
                rect.pivot = new Vector2(0.5f, 0f);
                rect.anchoredPosition = Vector2.zero;
                rect.sizeDelta = FitObject(objectSprite);
                objectImage.gameObject.SetActive(objectSprite != null);
                objectImage.sprite = objectSprite;
                objectImage.color = Color.white;
            }

            if (label != null)
            {
                label.text = title;
                label.gameObject.SetActive(!string.IsNullOrEmpty(title));
            }

            if (arrow != null)
            {
                arrow.gameObject.SetActive(objectSprite == null && pathSprite == null);
            }

            gameObject.SetActive(true);
        }

        /// <summary>Скрывает ответвление.</summary>
        public void Hide() => gameObject.SetActive(false);

        /// <summary>
        /// Вписывает вырезку перехода в квадрат указателя с сохранением пропорций:
        /// низ картинки стоит в точке ответвления, поэтому переход не висит над дорогой.
        /// </summary>
        /// <param name="sprite">Вырезка локации-цели; <c>null</c> — пустой квадрат.</param>
        /// <returns>Размер картинки.</returns>
        private static Vector2 FitObject(Sprite? sprite)
        {
            var sourceWidth = sprite != null ? sprite.rect.width : 300f;
            var sourceHeight = sprite != null ? sprite.rect.height : 300f;
            sourceWidth = Mathf.Max(1f, sourceWidth);
            sourceHeight = Mathf.Max(1f, sourceHeight);
            var fit = Mathf.Min(300f / sourceWidth, 300f / sourceHeight);
            return new Vector2(sourceWidth * fit, sourceHeight * fit);
        }
    }
}
