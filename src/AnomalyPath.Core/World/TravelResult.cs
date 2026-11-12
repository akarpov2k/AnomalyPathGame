#nullable enable
using AnomalyPath.Core.Player;

namespace AnomalyPath.Core.World
{
    /// <summary>Результат перехода: статус, ребро и урон жажды, если он случился.</summary>
    public sealed class TravelResult
    {
        /// <summary>Создаёт результат.</summary>
        /// <param name="status">Статус перехода.</param>
        /// <param name="edge">Ребро или <c>null</c>.</param>
        /// <param name="damage">Урон жажды за переход; <c>null</c> — воды хватило.</param>
        public TravelResult(TravelStatus status, WorldEdge? edge = null, PlayerDamageResult? damage = null)
        {
            Status = status;
            Edge = edge;
            Damage = damage;
        }

        /// <summary>Статус перехода.</summary>
        public TravelStatus Status { get; }

        /// <summary>Ребро, если переход удался.</summary>
        public WorldEdge? Edge { get; }

        /// <summary>Урон жажды за переход; <c>null</c> — воды хватило.</summary>
        public PlayerDamageResult? Damage { get; }

        /// <summary>Переход выполнен.</summary>
        public bool Succeeded => Status == TravelStatus.Success;

        /// <summary><c>true</c>, если переход закончился смертью от жажды.</summary>
        public bool Died => Damage?.Died == true;
    }
}
