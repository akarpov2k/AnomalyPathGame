#nullable enable
using System.Collections.Generic;
using AnomalyPath.Core.Dialog;

namespace AnomalyPath.Core.Abstractions
{
    /// <summary>Доступ к диалогам: единственный путь к данным диалогов из игрового кода.</summary>
    public interface IDialogRepository
    {
        /// <summary>Все диалоги, загруженные при старте.</summary>
        IReadOnlyList<DialogTemplate> All { get; }

        /// <summary>Ищет диалог по id.</summary>
        /// <param name="id">Идентификатор диалога.</param>
        /// <param name="dialog">Найденный диалог или <c>null</c>.</param>
        /// <returns><c>true</c>, если диалог найден.</returns>
        bool TryGet(string id, out DialogTemplate dialog);

        /// <summary>Проверяет, существует ли диалог.</summary>
        /// <param name="id">Идентификатор диалога.</param>
        /// <returns><c>true</c>, если диалог существует.</returns>
        bool Contains(string id);

        /// <summary>Возвращает диалог по id.</summary>
        /// <param name="id">Идентификатор диалога.</param>
        /// <returns>Диалог.</returns>
        /// <exception cref="Data.DataException">Диалог не найден.</exception>
        DialogTemplate Get(string id);
    }
}
