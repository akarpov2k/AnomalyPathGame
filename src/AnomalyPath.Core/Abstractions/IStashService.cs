#nullable enable
using AnomalyPath.Core.Loot;
using AnomalyPath.Core.Minigames;
using AnomalyPath.Core.World;

namespace AnomalyPath.Core.Abstractions
{
    /// <summary>
    /// Тайники: появление при входе в локацию с растущим шансом, вскрытие
    /// с отдельным пулом лута и шансом джекпота, кодовый замок.
    /// </summary>
    public interface IStashService
    {
        /// <summary>Настроены ли тайники в данных.</summary>
        bool Enabled { get; }

        /// <summary>Настроен ли кодовый замок в правилах.</summary>
        bool LockEnabled { get; }

        /// <summary>
        /// Проверяет появление тайника при входе в локацию. Хаб не проверяется,
        /// одна локация проверяется один раз; при находке счётчик проверок сбрасывается.
        /// </summary>
        /// <param name="world">Состояние мира: счётчики проверок и seed.</param>
        /// <param name="location">Экземпляр локации.</param>
        /// <returns><c>true</c>, если на локации появился тайник.</returns>
        bool RollOnEntry(WorldState world, LocationInstance location);

        /// <summary>Состояние замка текущего тайника; <c>null</c> — замок не настроен.</summary>
        /// <param name="world">Состояние мира.</param>
        /// <param name="location">Экземпляр локации с тайником.</param>
        /// <returns>Снимок состояния замка или <c>null</c>.</returns>
        StashLockState? LockState(WorldState world, LocationInstance location);

        /// <summary>
        /// Проверяет введённый код. Неверная попытка тратит одну из попыток, седьмая
        /// неудача помечает тайник потерянным; верный код отмечает замок вскрытым.
        /// </summary>
        /// <param name="world">Состояние мира.</param>
        /// <param name="location">Экземпляр локации с тайником.</param>
        /// <param name="guess">Код из цифр допустимого алфавита.</param>
        /// <returns>Состояние замка после попытки; <c>null</c> — замок не настроен.</returns>
        StashLockState? Guess(WorldState world, LocationInstance location, string guess);

        /// <summary>Вскрывает тайник локации и бросает джекпот.</summary>
        /// <param name="world">Состояние мира: счётчик вскрытий и seed.</param>
        /// <param name="location">Экземпляр локации с тайником.</param>
        /// <returns>Статус, лут и признак джекпота.</returns>
        StashOpenResult Open(WorldState world, LocationInstance location);
    }
}
