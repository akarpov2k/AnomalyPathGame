#nullable enable
using System;
using AnomalyPath.Core.Data;
using AnomalyPath.Core.Templates;

namespace AnomalyPath.Core.Inventory
{
    /// <summary>
    /// Код выпавшей редкости оружия, брони и шлема для <c>ItemInstance.variantKey</c>:
    /// <c>rar:&lt;ступень&gt;</c>. Артефакты редкость хранят своим кодеком
    /// (<see cref="AnomalyPath.Core.Artifacts.ArtifactVariantCodec"/>), остальные категории
    /// редкости не имеют.
    /// </summary>
    public static class RarityVariantCodec
    {
        /// <summary>Префикс кода выпавшей редкости.</summary>
        public const string Prefix = "rar:";

        /// <summary>Собирает код варианта.</summary>
        /// <param name="rarity">Выпавшая ступень редкости.</param>
        /// <returns>Код вида <c>rar:epic</c>.</returns>
        public static string Format(Rarity rarity) => Prefix + SchemaEnum.ToCode(rarity);

        /// <summary>Разбирает код выпавшей редкости.</summary>
        /// <param name="variantKey">Код варианта; <c>null</c> — варианта нет.</param>
        /// <param name="rarity">Разобранная ступень.</param>
        /// <returns><c>true</c>, если код начинается с префикса и ступень известна.</returns>
        public static bool TryParse(string? variantKey, out Rarity rarity)
        {
            rarity = default;
            if (string.IsNullOrWhiteSpace(variantKey) ||
                !variantKey!.StartsWith(Prefix, StringComparison.Ordinal))
            {
                return false;
            }

            var code = variantKey.Substring(Prefix.Length);
            return code.Length > 0 && SchemaEnum.TryParse(code, out rarity);
        }
    }
}
