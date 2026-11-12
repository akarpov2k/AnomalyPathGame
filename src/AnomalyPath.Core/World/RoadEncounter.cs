#nullable enable
namespace AnomalyPath.Core.World
{
    /// <summary>Найденный на дороге противник: актёр, тир для лута и ключ броска.</summary>
    internal sealed class RoadEncounter
    {
        /// <summary>Создаёт найденного противника.</summary>
        /// <param name="actorId">Id актёра.</param>
        /// <param name="tier">Тир противника для лута.</param>
        /// <param name="key">Ключ броска, из которого получена встреча.</param>
        internal RoadEncounter(string actorId, int tier, string key)
        {
            ActorId = actorId;
            Tier = tier;
            Key = key;
        }

        /// <summary>Id актёра.</summary>
        internal string ActorId { get; }

        /// <summary>Тир противника для лута.</summary>
        internal int Tier { get; }

        /// <summary>Ключ броска.</summary>
        internal string Key { get; }
    }
}
