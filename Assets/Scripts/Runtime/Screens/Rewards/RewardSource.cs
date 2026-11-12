#nullable enable
namespace AnomalyPath.Runtime.Screens.Rewards
{
    /// <summary>Откуда пришла награда: от этого зависит подпись левой колонки экрана наград.</summary>
    public enum RewardSource
    {
        /// <summary>Победа над противником: слева его трофеи.</summary>
        Victory,

        /// <summary>Вскрытый тайник: слева его содержимое.</summary>
        Stash,

        /// <summary>Лут объекта локации или аномалии: слева его содержимое.</summary>
        Loot,

        /// <summary>Сдача задания: слева предметы награды.</summary>
        Quest
    }
}
