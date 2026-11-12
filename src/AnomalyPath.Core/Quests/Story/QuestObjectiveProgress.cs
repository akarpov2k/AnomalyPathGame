#nullable enable
using System;
using System.Collections.Generic;

namespace AnomalyPath.Core.Quests.Story
{
    /// <summary>
    /// Прогресс одной цели сюжетного квеста: счётчик и уже учтённые цели,
    /// чтобы одно убийство или одна локация не считались дважды.
    /// </summary>
    public sealed class QuestObjectiveProgress
    {
        private readonly List<string> _countedIds;

        /// <summary>Создаёт прогресс цели.</summary>
        /// <param name="type">Тип цели.</param>
        /// <param name="current">Текущий счётчик; не меньше нуля.</param>
        /// <param name="countedIds">Учтённые цели; пусто — ни одной.</param>
        /// <exception cref="ArgumentOutOfRangeException">Счётчик отрицателен.</exception>
        public QuestObjectiveProgress(
            QuestObjectiveType type,
            int current = 0,
            IReadOnlyList<string>? countedIds = null)
        {
            if (current < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(current));
            }

            Type = type;
            Current = current;
            _countedIds = countedIds == null ? new List<string>() : new List<string>(countedIds);
        }

        /// <summary>Тип цели.</summary>
        public QuestObjectiveType Type { get; }

        /// <summary>Сколько раз цель уже выполнена.</summary>
        public int Current { get; private set; }

        /// <summary>Учтённые цели: экземпляры локаций, актёры, объекты или тайники.</summary>
        public IReadOnlyList<string> CountedIds => _countedIds;

        /// <summary>Зачитывает цель, если она ещё не учтена и счётчик не достиг предела.</summary>
        /// <param name="targetId">Идентификатор цели; пусто — без дедупликации.</param>
        /// <param name="required">Сколько нужно всего.</param>
        /// <returns><c>true</c>, если счётчик вырос.</returns>
        public bool TryAdvance(string? targetId, int required)
        {
            if (Current >= required)
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(targetId))
            {
                if (_countedIds.Contains(targetId))
                {
                    return false;
                }

                _countedIds.Add(targetId);
            }

            Current++;
            return true;
        }

        /// <summary>Сбрасывает счётчик и список учтённых целей.</summary>
        public void Reset()
        {
            Current = 0;
            _countedIds.Clear();
        }

        /// <summary>Проверяет, выполнена ли цель.</summary>
        /// <param name="required">Сколько нужно всего.</param>
        /// <returns><c>true</c>, если счётчик достиг предела.</returns>
        public bool IsDone(int required) => Current >= required;
    }
}
