#nullable enable
namespace AnomalyPath.Runtime.Screens.Character
{
    /// <summary>Какие предметы показывает сетка инвентаря.</summary>
    public enum InventoryFilter
    {
        /// <summary>Все предметы.</summary>
        All,

        /// <summary>Оружие.</summary>
        Weapons,

        /// <summary>Броня, шлемы, рюкзаки и пояса.</summary>
        Armor,

        /// <summary>Расходники.</summary>
        Consumables,

        /// <summary>Патроны, артефакты, трофеи, материалы и квестовые предметы.</summary>
        Other
    }
}
