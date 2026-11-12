#nullable enable
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Runtime.Ui.Core;
using UnityEngine;
using UnityEngine.UI;

namespace AnomalyPath.Editor.Ui
{
    /// <summary>Часть класса <see cref="MenuPrefabsScreenshot"/>: кадр меню игры (Esc).</summary>
    public static partial class MenuPrefabsScreenshot
    {
        /// <summary>
        /// Заполняет сводку состояния и счётчик смертей образцом: в игре их пишет
        /// <c>GameMenuScreen.Refresh</c> из сессии, в кадре сессии нет.
        /// </summary>
        /// <param name="panel">Экземпляр префаба меню игры.</param>
        /// <param name="texts">Каталог текстов игры.</param>
        /// <param name="pageSize">Размер страницы сохранений; меню его не использует.</param>
        private static void FillGameMenu(GameObject panel, IGameTexts texts, int pageSize)
        {
            var status = FindDeep(panel.transform, "Status");
            var statusLabel = status != null ? status.GetComponent<Text>() : null;
            if (statusLabel != null)
            {
                statusLabel.text = UiText.Format(texts, UiTextKey.Game_Status, "Кордон", 18, 20, 1, "06:00", 87);
            }

            var deaths = FindDeep(panel.transform, "GameMenuDeaths");
            var deathsLabel = deaths != null ? deaths.GetComponent<Text>() : null;
            if (deathsLabel != null)
            {
                deathsLabel.text = UiText.Format(texts, UiTextKey.GameMenu_Deaths, 2);
            }
        }
    }
}
