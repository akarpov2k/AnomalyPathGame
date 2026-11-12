#nullable enable
using AnomalyPath.Core.Abstractions;

namespace AnomalyPath.Runtime.Ui.Core
{
    /// <summary>Мир и задания: квесты, локации, аномалии, диалоги и записи спавна.</summary>
    public interface IQuestServices
    {
        /// <summary>Повторяемые задания: предложения торговцев, взятие и сдача.</summary>
        IQuestService Quests { get; }

        /// <summary>Сюжетные квесты: прогресс и сдача.</summary>
        IStoryQuestService StoryQuests { get; }

        /// <summary>Диалоги: начало разговора и выбор вариантов.</summary>
        IDialogService Dialogs { get; }

        /// <summary>Шаблоны локаций: названия для основного экрана и меню.</summary>
        ILocationRepository Locations { get; }

        /// <summary>Аномалии: число попыток и тиры локаций.</summary>
        IAnomalyRepository Anomalies { get; }

        /// <summary>Записи спавна: картинка объекта на сцене локации по id спавна.</summary>
        ISpawnRepository Spawns { get; }

        /// <summary>Топология: номер звена и цена возврата в хаб в сводке локации.</summary>
        ITopologyRepository Topology { get; }
    }
}
