#nullable enable
using System;
using AnomalyPath.Core.Localization;

namespace AnomalyPath.Core.Spawns
{
    /// <summary>Противник на локации: один враг, с которым начинается бой.</summary>
    public sealed class EnemyLocationObject : LocationObject
    {
        /// <summary>Создаёт объект-противника.</summary>
        /// <param name="spawnId">Id записи спавна.</param>
        /// <param name="titleKey">Ключ названия в каталоге строк.</param>
        /// <param name="actorId">Id актёра типа <c>enemy</c>.</param>
        /// <exception cref="ArgumentException">Id актёра пуст.</exception>
        public EnemyLocationObject(string spawnId, string titleKey, string actorId)
            : base(spawnId, titleKey, LocationObjectKind.Enemy)
        {
            if (string.IsNullOrWhiteSpace(actorId))
            {
                throw new ArgumentException(
                    CoreText.Format(CoreErrors.RequiredField, nameof(actorId)),
                    nameof(actorId));
            }

            ActorId = actorId;
        }

        /// <summary>Id актёра типа <c>enemy</c>.</summary>
        public string ActorId { get; }
    }
}
