#nullable enable
using AnomalyPath.Core.Abstractions;

namespace AnomalyPath.Runtime.Ui.Core
{
    /// <summary>Боевые сервисы: актёры, травмы, прогрессия, скиллы и цены боя.</summary>
    public interface ICombatServices
    {
        /// <summary>Актёры: максимум здоровья игрока для полосы.</summary>
        IActorRepository Actors { get; }

        /// <summary>Травмы: иконки и названия активных травм персонажа.</summary>
        IInjuryRepository Injuries { get; }

        /// <summary>Прогрессия уровней: требование следующего уровня для полосы опыта.</summary>
        IProgressionService Progression { get; }

        /// <summary>Цены действий боя из данных: перезарядка и смена оружия в подсказках.</summary>
        ICombatSettings CombatSettings { get; }

        /// <summary>Скиллы из данных: названия действий оружия в бою.</summary>
        ISkillRepository Skills { get; }

        /// <summary>Правила боя: шанс попадания, предпросмотр скилла и шага без броска.</summary>
        ICombatService CombatEngine { get; }
    }
}
