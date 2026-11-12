#nullable enable
using System;
using UnityEngine;

namespace AnomalyPath.Runtime.Ui.Core
{
    /// <summary>
    /// Держит содержимое экрана в безопасной области устройства: на телефоне вырез камеры,
    /// скругления и панель жестов не должны перекрывать интерфейс. На Windows дополнительно
    /// учитывается панель задач: окно без рамки может заходить под неё низом, и тогда нижний
    /// ряд кнопок и полоса опыта не видны. Родитель — канвас на весь экран, сама область
    /// пересчитывается при смене разрешения, повороте и смещении окна.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaRoot : MonoBehaviour
    {
        private int _width = -1;
        private int _height = -1;
        private Rect _visible;
        private float _nextCheck;

        private void OnEnable() => Apply();

        private void Update()
        {
            if (_width != Screen.width || _height != Screen.height)
            {
                Apply();
                return;
            }

            if (Time.unscaledTime < _nextCheck)
            {
                return;
            }

            _nextCheck = Time.unscaledTime + 0.5f;
            var visible = ScreenVisibleArea.Current;
            if (visible == _visible)
            {
                return;
            }

            _visible = visible;
            Apply();
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
            _visible = ScreenVisibleArea.Current;
            var safe = Screen.safeArea;
            var left = Mathf.Max(safe.xMin, _visible.xMin);
            var bottom = Mathf.Max(safe.yMin, _visible.yMin);
            var right = Mathf.Min(safe.xMax, _visible.xMax);
            var top = Mathf.Min(safe.yMax, _visible.yMax);
            var area = right - left >= 1f && top - bottom >= 1f
                ? new Rect(left, bottom, right - left, top - bottom)
                : safe;
            if (area.width < _width - 0.5f || area.height < _height - 0.5f)
            {
                Debug.Log($"AnomalyPath: интерфейс вписан в {area.width:0}×{area.height:0} при экране {_width}×{_height}");
            }

            ApplyInsets((RectTransform)transform, area, _width, _height);
        }
    }
}
