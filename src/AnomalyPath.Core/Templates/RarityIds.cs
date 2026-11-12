#nullable enable
using System;
using AnomalyPath.Core.Data;

namespace AnomalyPath.Core.Templates
{
    /// <summary>Кодек редкости: id сущности <c>rar_*</c> и enum <see cref="Rarity"/>.</summary>
    public static class RarityIds
    {
        /// <summary>Префикс id редкости в данных.</summary>
        public const string Prefix = "rar_";

        /// <summary>Сколько ступеней редкости в игре: столько же, сколько членов <see cref="Rarity"/>.</summary>
        public static int Count => (int)Rarity.Unique;

        /// <summary>Разбирает id редкости в перечисление.</summary>
        /// <param name="id">Id из данных вида <c>rar_rare</c>.</param>
        /// <param name="rarity">Разобранная ступень.</param>
        /// <returns><c>true</c>, если id известен.</returns>
        public static bool TryParse(string id, out Rarity rarity)
        {
            rarity = default;
            if (string.IsNullOrEmpty(id) || !id.StartsWith(Prefix, StringComparison.Ordinal))
            {
                return false;
            }

            return SchemaEnum.TryParse(id.Substring(Prefix.Length), out rarity);
        }

        /// <summary>Возвращает id редкости для данных.</summary>
        /// <param name="rarity">Ступень.</param>
        /// <returns>Id вида <c>rar_rare</c>.</returns>
        public static string Id(Rarity rarity) => Prefix + SchemaEnum.ToCode(rarity);
    }
}
