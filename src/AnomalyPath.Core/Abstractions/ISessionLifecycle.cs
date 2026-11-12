#nullable enable
using System;
using AnomalyPath.Core.Session;

namespace AnomalyPath.Core.Abstractions
{
    /// <summary>Жизненный цикл сессии: сохранение, загрузка и новая игра.</summary>
    internal interface ISessionLifecycle
    {
        /// <summary>Сохраняет состояние сессии в слот.</summary>
        /// <param name="session">Игровая сессия.</param>
        /// <param name="slot">Имя слота.</param>
        /// <param name="playtime">Наигранное время в секундах.</param>
        /// <param name="createdAt">Дата создания; по умолчанию текущая.</param>
        /// <param name="title">Название сохранения, заданное игроком.</param>
        void Save(
            GameSession session,
            string slot,
            double playtime = 0,
            DateTimeOffset? createdAt = null,
            string? title = null);

        /// <summary>Загружает сессию из слота; бой не восстанавливается.</summary>
        /// <param name="slot">Имя слота.</param>
        /// <returns>Сессия с загруженным состоянием.</returns>
        GameSession Load(string slot);

        /// <summary>Создаёт новую игру и проверяет тайник стартовой локации.</summary>
        /// <param name="seed">Seed генерации мира.</param>
        /// <param name="loadoutId">Id выбранного набора снаряжения.</param>
        /// <param name="settings">Настройки сложности.</param>
        /// <returns>Новая сессия.</returns>
        GameSession StartNew(int seed, string loadoutId, NewGameSettings settings);
    }
}
