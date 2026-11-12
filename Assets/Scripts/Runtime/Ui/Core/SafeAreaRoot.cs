#nullable enable
using System;
using UnityEngine;

namespace AnomalyPath.Runtime.Ui.Core
{
    /// <summary>
    /// Держит содержимое экрана в безопасной области устройства: на телефоне вырез камеры,
    /// скругления и панель жестов не должны перекрывать интерфейс. Родитель — канвас на весь экран,
    /// сама область пересчитывается при смене разрешения или повороте.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaRoot : MonoBehaviour
    {
        private int _width = -1;
        private int _height = -1;

        private void OnEnable() => Apply();

        private void Update()
        {
            if (_width != Screen.width || _height != Screen.height)
            {
                Apply();
            }
        }

        /// <summary>Раскладывает прямоугольник по безопасной области экрана.</summary>
        /// <param name="rect">Прямоугольник содержимого на весь экран.</param>
        /// <param name="safeArea">Безопасная область в пикселях экрана.</param>
        /// <param name="width">Ширина экрана в пикселях.</param>
        /// <param name="height">Высота экрана в пикселях.</param>
        /// <exception cref="ArgumentNullException">Прямоугольник не задан.</exception>
        public static void ApplyInsets(RectTransform rect, Rect safeArea, int width, int height)
        {
            if (rect == null)
            {
                throw new ArgumentNullException(nameof(rect));
            }

            if (width <= 0 || height <= 0)
            {
                return;
            }

            rect.anchorMin = new Vector2(safeArea.xMin / width, safeArea.yMin / height);
            rect.anchorMax = new Vector2(safeArea.xMax / width, safeArea.yMax / height);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private void Apply()
        {
            _width = Screen.width;
            _height = Screen.height;
            ApplyInsets((RectTransform)transform, Screen.safeArea, _width, _height);
        }
    }
}
