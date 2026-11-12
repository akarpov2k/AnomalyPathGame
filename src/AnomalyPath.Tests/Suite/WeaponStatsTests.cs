#nullable enable
using System;
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Core.Inventory;
using AnomalyPath.Core.Templates;

namespace AnomalyPath.Tests
{
    /// <summary>Статы оружия от редкости: множитель урона и точности по ступени.</summary>
    public sealed class WeaponStatsTests : DataTestBase
    {
        /// <summary>Создаёт категорию проверок статов оружия.</summary>
        /// <param name="dataRoot">Корень данных проекта.</param>
        public WeaponStatsTests(string dataRoot)
            : base(dataRoot)
        {
        }

        /// <inheritdoc />
        public override void Run()
        {
            var services = BuildServices();
            var items = services.Resolve<IItemRepository>();
            var rarities = services.Resolve<IRarityRepository>();

            Check("статы оружия: обычная ступень без прибавки", () =>
            {
                var template = items.Get("wpn_ak74");
                var weapon = new ItemInstance("w1", "wpn_ak74", 1, variantKey: RarityVariantCodec.Format(Rarity.Common));
                Near(1.0, WeaponStats.Multiplier(weapon, template, rarities), "множитель");
            });

            Check("статы оружия: эпическая ступень даёт +30%", () =>
            {
                var template = items.Get("wpn_ak74");
                var weapon = new ItemInstance("w2", "wpn_ak74", 1, variantKey: RarityVariantCodec.Format(Rarity.Epic));
                Near(1.3, WeaponStats.Multiplier(weapon, template, rarities), "множитель");
            });

            Check("статы оружия: уникальная ступень даёт +50%", () =>
            {
                var template = items.Get("wpn_ak74");
                var weapon = new ItemInstance("w3", "wpn_ak74", 1, variantKey: RarityVariantCodec.Format(Rarity.Unique));
                Near(1.5, WeaponStats.Multiplier(weapon, template, rarities), "множитель");
            });

            Check("статы оружия: без выпавшей ступени — редкость шаблона", () =>
            {
                var template = items.Get("wpn_ak74");
                var weapon = new ItemInstance("w4", "wpn_ak74", 1);
                Near(1.1, WeaponStats.Multiplier(weapon, template, rarities), "АК-74 редкая");
            });

            Check("статы оружия: не оружие — без прибавки", () =>
            {
                var armor = items.Get("itm_armor_plate");
                var instance = new ItemInstance("a1", "itm_armor_plate", 1, variantKey: RarityVariantCodec.Format(Rarity.Epic));
                Near(1.0, WeaponStats.Multiplier(instance, armor, rarities), "броня");
                Near(1.0, WeaponStats.Multiplier(instance, null, rarities), "шаблона нет");
            });

            Check("статы оружия: репозиторий редкостей обязателен", () =>
            {
                Throws<ArgumentNullException>(
                    () => WeaponStats.Multiplier(new ItemInstance("w5", "wpn_ak74", 1), items.Get("wpn_ak74"), null!),
                    "редкости");
            });
        }

        private static void Near(double expected, double actual, string what)
        {
            if (Math.Abs(expected - actual) > 1e-9)
            {
                throw new InvalidOperationException(what + ": ожидалось " + expected + ", получено " + actual);
            }
        }
    }
}
