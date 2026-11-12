#nullable enable
using AnomalyPath.Runtime.Ui.Core;
using UnityEngine;
using UnityEngine.UI;

namespace AnomalyPath.Runtime.Screens.Menu
{
    /// <summary>
    /// Высота карточек сохранения: на страницу помещается ровно <c>pageSize</c> карточек,
    /// поэтому список заполняется при любом разрешении, а карточки пропорционально уменьшаются
    /// на узком окне и не выходят за заданные пределы.
    /// </summary>
    public static class SaveRowLayout
    {
        /// <summary>Высота карточки по высоте списка, зазору и числу карточек на страницу.</summary>
        /// <param name="viewportHeight">Высота видимой области списка.</param>
        /// <param name="spacing">Зазор между карточками из группы раскладки.</param>
        /// <param name="pageSize">Сколько карточек помещается на страницу.</param>
        /// <returns>Высота карточки в единицах канваса.</returns>
        public static float Height(float viewportHeight, float spacing, int pageSize)
        {
            var count = Mathf.Max(1, pageSize);
            var available = viewportHeight - (spacing * (count - 1));
            return Mathf.Clamp(
                available / count,
                MenuScreenMetrics.RowMinHeight,
                MenuScreenMetrics.RowMaxHeight);
        }

        /// <summary>Ставит всем видимым карточкам высоту страницы.</summary>
        /// <param name="rows">Область прокрутки со списком сохранений.</param>
        /// <param name="pageSize">Сколько карточек помещается на страницу.</param>
        public static void Apply(ScrollArea rows, int pageSize)
        {
            if (rows == null || rows.Content == null)
            {
                return;
            }

            var layout = rows.Content.GetComponent<VerticalLayoutGroup>();
            var spacing = layout != null ? layout.spacing : 0f;
            var height = Height(rows.ViewportHeight, spacing, pageSize);
            foreach (Transform child in rows.Content)
            {
                if (!child.gameObject.activeSelf)
                {
                    continue;
                }

                var element = child.GetComponent<LayoutElement>();
                if (element == null)
                {
                    continue;
                }

                element.preferredHeight = height;
                element.minHeight = height;
            }
        }
    }
}
