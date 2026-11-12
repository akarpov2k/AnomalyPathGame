#nullable enable
using UnityEngine;

namespace AnomalyPath.Runtime.Screens.Quests
{
    /// <summary>Цвета экранов доски объявлений и журнала заданий.</summary>
    public static class QuestPalette
    {
        /// <summary>Фон строки и карточки.</summary>
        public static readonly Color Row = new Color(0.10f, 0.13f, 0.12f, 0.92f);

        /// <summary>Фон строки и карточки готового задания.</summary>
        public static readonly Color ReadyRow = new Color(0.12f, 0.20f, 0.14f, 0.95f);

        /// <summary>Фон выбранной строки журнала.</summary>
        public static readonly Color SelectedRow = new Color(0.16f, 0.24f, 0.18f, 1f);

        /// <summary>Фон окна доски и журнала.</summary>
        public static readonly Color Window = new Color(0.07f, 0.09f, 0.09f, 0.97f);

        /// <summary>Фон панели списка и описания.</summary>
        public static readonly Color Panel = new Color(0.10f, 0.13f, 0.12f, 0.95f);

        /// <summary>Фон невыбранной категории журнала.</summary>
        public static readonly Color Tab = new Color(0.15f, 0.19f, 0.18f, 1f);

        /// <summary>Фон выбранной категории журнала.</summary>
        public static readonly Color TabActive = new Color(0.22f, 0.31f, 0.23f, 1f);

        /// <summary>Подложка картинки квеста, пока спрайта нет.</summary>
        public static readonly Color ArtPlaceholder = new Color(0.14f, 0.18f, 0.17f, 1f);

        /// <summary>Цвет ошибки.</summary>
        public static readonly Color Error = new Color(0.95f, 0.55f, 0.45f, 1f);
    }
}
