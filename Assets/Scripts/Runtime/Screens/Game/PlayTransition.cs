#nullable enable
using System;

namespace AnomalyPath.Runtime.Screens.Game
{
    /// <summary>
    /// Переход с экрана локации: объект-цель на сцене. Названий локаций на экране нет,
    /// поэтому переход несёт только картинку цели (вырезку или общий указатель) и обработчик.
    /// </summary>
    public sealed class PlayTransition
    {
        /// <summary>Создаёт переход.</summary>
        /// <param name="id">Идентификатор перехода, совпадает с id ребра карты.</param>
        /// <param name="artId">Id вырезки объекта в каталоге спрайтов; <c>null</c> — общий указатель.</param>
        /// <param name="click">Что выполнить при нажатии.</param>
        /// <exception cref="ArgumentNullException">Нет id или обработчика.</exception>
        public PlayTransition(string id, string? artId, Action click)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException(null, nameof(id));
            }

            Id = id;
            ArtId = string.IsNullOrWhiteSpace(artId) ? null : artId;
            Click = click ?? throw new ArgumentNullException(nameof(click));
        }

        /// <summary>Идентификатор перехода.</summary>
        public string Id { get; }

        /// <summary>Id вырезки объекта; <c>null</c> — рисовать указатель.</summary>
        public string? ArtId { get; }

        /// <summary>Что выполнить при нажатии.</summary>
        public Action Click { get; }
    }
}
