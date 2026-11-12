#nullable enable
using System.Collections.Generic;

namespace AnomalyPath.Core.Quests.Story
{
    /// <summary>
    /// Текущий рейд: всё между выходом из хаба и входом в хаб. Хранит зачищенные
    /// локации, чтобы прогресс цели не зачитывал один экземпляр дважды.
    /// </summary>
    public sealed class RaidState
    {
        private readonly List<string> _cleared;

        /// <summary>Создаёт состояние рейда.</summary>
        /// <param name="active">Идёт ли рейд.</param>
        /// <param name="clearedLocationIds">Зачищенные за рейд локации; пусто — ни одной.</param>
        public RaidState(bool active = false, IReadOnlyList<string>? clearedLocationIds = null)
        {
            Active = active;
            _cleared = clearedLocationIds == null
                ? new List<string>()
                : new List<string>(clearedLocationIds);
        }

        /// <summary>Идёт ли рейд.</summary>
        public bool Active { get; private set; }

        /// <summary>Зачищенные за рейд локации.</summary>
        public IReadOnlyList<string> ClearedLocationIds => _cleared;

        /// <summary>Проверяет, зачитывалась ли локация в этом рейде.</summary>
        /// <param name="instanceId">Экземпляр локации.</param>
        /// <returns><c>true</c>, если локация уже учтена.</returns>
        public bool Contains(string instanceId) =>
            !string.IsNullOrWhiteSpace(instanceId) && _cleared.Contains(instanceId);

        /// <summary>Зачитывает зачищенную локацию.</summary>
        /// <param name="instanceId">Экземпляр локации.</param>
        public void MarkCleared(string instanceId)
        {
            if (!string.IsNullOrWhiteSpace(instanceId) && !_cleared.Contains(instanceId))
            {
                _cleared.Add(instanceId);
            }
        }

        /// <summary>Начинает новый рейд, очищая список зачищенных локаций.</summary>
        public void Begin()
        {
            Active = true;
            _cleared.Clear();
        }

        /// <summary>Завершает рейд: список очищается, рейд считается не начатым.</summary>
        public void Reset()
        {
            Active = false;
            _cleared.Clear();
        }
    }
}
