#nullable enable
namespace AnomalyPath.Core.Combat
{
    /// <summary>Почему действие доступно или недоступно.</summary>
    public enum CombatActionAvailability
    {
        /// <summary>Действие доступно.</summary>
        Available,

        /// <summary>Не хватает очков действия.</summary>
        NoAp,

        /// <summary>Не хватает патронов в магазине.</summary>
        NoAmmo,

        /// <summary>Оружие сломано.</summary>
        BrokenWeapon,

        /// <summary>Оружия нет.</summary>
        NoWeapon,

        /// <summary>Бой уже завершён.</summary>
        Finished,

        /// <summary>Скилл не найден в данных.</summary>
        MissingSkill
    }
}
