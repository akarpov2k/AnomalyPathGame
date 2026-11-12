#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using AnomalyPath.Core.Data;
using AnomalyPath.Core.Templates;

namespace AnomalyPath.Core.Artifacts
{
    /// <summary>
    /// Код варианта артефакта для <c>ItemInstance.variantKey</c>:
    /// <c>art:&lt;rarity&gt;[:&lt;индексы через запятую&gt;]</c>.
    /// </summary>
    public static class ArtifactVariantCodec
    {
        /// <summary>Префикс кода артефакта.</summary>
        public const string Prefix = "art:";

        /// <summary>Собирает код варианта.</summary>
        /// <param name="rarity">Ступень редкости.</param>
        /// <param name="extraIndexes">Номера доп. свойств; пусто — доп. свойств нет.</param>
        /// <returns>Код варианта.</returns>
        /// <exception cref="ArgumentNullException">Список номеров не задан.</exception>
        public static string Format(Rarity rarity, IReadOnlyList<int> extraIndexes)
        {
            if (extraIndexes == null)
            {
                throw new ArgumentNullException(nameof(extraIndexes));
            }

            var builder = new StringBuilder(Prefix);
            builder.Append(SchemaEnum.ToCode(rarity));
            if (extraIndexes.Count > 0)
            {
                builder.Append(':');
                for (var index = 0; index < extraIndexes.Count; index++)
                {
                    if (index > 0)
                    {
                        builder.Append(',');
                    }

                    builder.Append(extraIndexes[index].ToString(CultureInfo.InvariantCulture));
                }
            }

            return builder.ToString();
        }

        /// <summary>Разбирает код варианта артефакта.</summary>
        /// <param name="variantKey">Код варианта; <c>null</c> — варианта нет.</param>
        /// <param name="variant">Разобранный вариант.</param>
        /// <returns><c>true</c>, если код артефактный и разобран полностью.</returns>
        public static bool TryParse(string? variantKey, out ArtifactVariant? variant)
        {
            variant = null;
            if (string.IsNullOrWhiteSpace(variantKey) ||
                !variantKey!.StartsWith(Prefix, StringComparison.Ordinal))
            {
                return false;
            }

            var rest = variantKey.Substring(Prefix.Length);
            var separator = rest.IndexOf(':');
            var rarityCode = separator < 0 ? rest : rest.Substring(0, separator);
            if (!SchemaEnum.TryParse(rarityCode, out Rarity rarity))
            {
                return false;
            }

            var indexes = new List<int>();
            if (separator >= 0)
            {
                var tail = rest.Substring(separator + 1);
                if (tail.Length == 0)
                {
                    return false;
                }

                foreach (var chunk in tail.Split(','))
                {
                    if (!int.TryParse(chunk, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ||
                        value < 0)
                    {
                        return false;
                    }

                    indexes.Add(value);
                }
            }

            variant = new ArtifactVariant(rarity, indexes);
            return true;
        }
    }
}
