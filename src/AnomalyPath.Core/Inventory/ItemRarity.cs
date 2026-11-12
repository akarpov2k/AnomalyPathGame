#nullable enable
using AnomalyPath.Core.Artifacts;
using AnomalyPath.Core.Templates;

namespace AnomalyPath.Core.Inventory
{
    /// <summary>
    /// Редкость экземпляра предмета: выпавшая ступень из <c>variantKey</c>, ступень артефакта
    /// из его кода, иначе базовая ступень шаблона — но только у категорий, которым редкость
    /// положена (оружие, броня, шлем, артефакт). У расходников, патронов, трофеев, материалов,
    /// рюкзаков, поясов и квестовых предметов редкости нет.
    /// </summary>
    public static class ItemRarity
    {
        /// <summary>Возвращает редкость экземпляра или <c>null</c>, если её нет.</summary>
        /// <param name="item">Экземпляр предмета.</param>
        /// <param name="template">Шаблон предмета; <c>null</c> — неизвестен, берётся только код варианта.</param>
        /// <returns>Ступень редкости или <c>null</c>.</returns>
        public static Rarity? Resolve(ItemInstance? item, ItemTemplate? template)
        {
            if (item == null)
            {
                return null;
            }

            if (RarityVariantCodec.TryParse(item.VariantKey, out var rolled))
            {
                return rolled;
            }

            if (ArtifactVariantCodec.TryParse(item.VariantKey, out var artifact) && artifact != null)
            {
                return artifact.Rarity;
            }

            if (template == null || !HasRarity(template.Category))
            {
                return null;
            }

            return template.Rarity;
        }

        /// <summary>Есть ли у категории редкость: оружие, броня, шлем и артефакт.</summary>
        /// <param name="category">Категория шаблона.</param>
        /// <returns><c>true</c> — у категории есть ступень редкости.</returns>
        private static bool HasRarity(ItemCategory category) =>
            category == ItemCategory.Weapon ||
            category == ItemCategory.Armor ||
            category == ItemCategory.Helmet ||
            category == ItemCategory.Artifact;
    }
}
