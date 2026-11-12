#nullable enable
using AnomalyPath.Core.Templates;
using AnomalyPath.Runtime.Ui.Core;
using UnityEngine;

namespace AnomalyPath.Runtime.Ui.Items
{
    /// <summary>
    /// Окантовка предмета по редкости: четыре тонкие полосы по краям клетки или слота.
    /// Полосы вписаны внутрь прямоугольника, поэтому не задевают соседние клетки и не
    /// перекрывают иконку, которая лежит с отступом от края.
    /// </summary>
    public static class RarityFrame
    {
        /// <summary>Толщина окантовки в пикселях UI.</summary>
        public const float Thickness = 3f;

        /// <summary>
        /// Рисует окантовку по краям прямоугольника. У обычной ступени и при неизвестной
        /// категории рамки нет: метод ничего не создаёт.
        /// </summary>
        /// <param name="ui">Фабрика элементов интерфейса.</param>
        /// <param name="target">Прямоугольник клетки или слота; полосы станут его детьми.</param>
        /// <param name="rarity">Ступень редкости; <c>null</c> — редкости нет.</param>
        public static void Draw(UiFactory ui, RectTransform target, Rarity? rarity)
        {
            if (ui == null || target == null || rarity == null || !RarityPalette.TryGet(rarity.Value, out var color))
            {
                return;
            }

            Edge(ui, target, "RarityBorderTop", new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, -Thickness), Vector2.zero, color);
            Edge(ui, target, "RarityBorderBottom", new Vector2(0f, 0f), new Vector2(1f, 0f),
                Vector2.zero, new Vector2(0f, Thickness), color);
            Edge(ui, target, "RarityBorderLeft", new Vector2(0f, 0f), new Vector2(0f, 1f),
                Vector2.zero, new Vector2(Thickness, 0f), color);
            Edge(ui, target, "RarityBorderRight", new Vector2(1f, 0f), new Vector2(1f, 1f),
                new Vector2(-Thickness, 0f), Vector2.zero, color);
        }

        private static void Edge(
            UiFactory ui,
            RectTransform target,
            string name,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 offsetMin,
            Vector2 offsetMax,
            Color color)
        {
            var image = ui.CreateImage(name, target, color);
            image.raycastTarget = false;
            var rect = image.rectTransform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }
    }
}
