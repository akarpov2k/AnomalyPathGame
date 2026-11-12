#nullable enable
namespace AnomalyPath.Core.Inventory
{
    /// <summary>Что за характеристика предмета показывает подсказка.</summary>
    public enum ItemFactKind
    {
        /// <summary>Вес одной единицы предмета.</summary>
        Weight,

        /// <summary>Базовый урон оружия.</summary>
        Damage,

        /// <summary>Вместимость магазина оружия.</summary>
        Magazine,

        /// <summary>Калибр оружия или патронов; значение лежит в <see cref="ItemFact.Code"/>.</summary>
        Caliber,

        /// <summary>Бронебойность патрона, 0..1.</summary>
        Penetration,

        /// <summary>Текущая и максимальная прочность.</summary>
        Durability,

        /// <summary>Надбавка к защите от типа урона; тип лежит в <see cref="ItemFact.Code"/>.</summary>
        Protection,

        /// <summary>Прибавка к лимиту веса рюкзака.</summary>
        ExtraWeight,

        /// <summary>Число слотов артефактов брони или пояса.</summary>
        ArtifactSlots,

        /// <summary>Прибавка к максимуму здоровья от артефакта.</summary>
        HealthModifier,

        /// <summary>Прибавка к меткости от артефакта или расходника.</summary>
        AccuracyModifier,

        /// <summary>Прибавка к максимуму выносливости от артефакта.</summary>
        StaminaModifier,

        /// <summary>Прибавка к защите от типа урона; тип лежит в <see cref="ItemFact.Code"/>.</summary>
        ProtectionModifier,

        /// <summary>Прибавка к цене действия в ОД от расходника; может быть отрицательной.</summary>
        ActionApCostModifier,

        /// <summary>Сколько здоровья лечит расходник за применение.</summary>
        Heal,

        /// <summary>Сколько очков действия восстанавливает расходник за применение.</summary>
        ApRestore,

        /// <summary>Сколько еды восполняет расходник за применение.</summary>
        Food,

        /// <summary>Сколько воды восполняет расходник за применение.</summary>
        Water,

        /// <summary>Остаток применений расходника.</summary>
        Charges,

        /// <summary>Сколько травм снимает расходник.</summary>
        HealsInjuries
    }
}
