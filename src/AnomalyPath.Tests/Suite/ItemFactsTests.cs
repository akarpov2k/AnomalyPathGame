#nullable enable
using System;
using System.Collections.Generic;
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Core.Inventory;

namespace AnomalyPath.Tests
{
    /// <summary>
    /// Характеристики предмета для подсказки: вес, оружие, защита, прочность, артефакты,
    /// расходники и патроны. Тексты подставляет интерфейс, здесь проверяются данные.
    /// </summary>
    public sealed class ItemFactsTests : DataTestBase
    {
        /// <summary>Создаёт категорию тестов характеристик предмета.</summary>
        /// <param name="dataRoot">Корень данных проекта.</param>
        public ItemFactsTests(string dataRoot)
            : base(dataRoot)
        {
        }

        /// <inheritdoc />
        public override void Run()
        {
            var services = BuildServices();
            var items = services.Resolve<IItemRepository>();
            var damageTypes = services.Resolve<IDamageTypeRepository>();
            var artifacts = services.Resolve<IArtifactService>();

            Check("характеристики: оружие — урон, магазин, калибр и прочность экземпляра", () =>
            {
                var template = items.Get("wpn_ak74");
                var instance = new ItemInstance("ak", "wpn_ak74", 1, durability: 12, durabilityMax: 60);
                var facts = ItemFacts.Collect(template, instance, damageTypes.All);

                Equal(5, facts.Count, "число характеристик");
                Near(3.7, Fact(facts, ItemFactKind.Weight).Value, "вес");
                Near(8, Fact(facts, ItemFactKind.Damage).Value, "урон");
                Near(30, Fact(facts, ItemFactKind.Magazine).Value, "магазин");
                Equal("5.45x39", Fact(facts, ItemFactKind.Caliber).Code, "калибр");
                Near(12, Fact(facts, ItemFactKind.Durability).Value, "прочность");
                Near(60, Fact(facts, ItemFactKind.Durability).Max!.Value, "максимум прочности");
            });

            Check("характеристики: оружие — урон с прибавкой редкости", () =>
            {
                var template = items.Get("wpn_ak74");
                var facts = ItemFacts.Collect(template, null, damageTypes.All, weaponStatMultiplier: 1.3);

                Near(10.4, Fact(facts, ItemFactKind.Damage).Value, "8 × 1.3");
                Near(30, Fact(facts, ItemFactKind.Magazine).Value, "магазин без прибавки");
            });

            Check("характеристики: броня — защита по порядку типов урона и слоты артефактов", () =>
            {
                var template = items.Get("itm_armor_plate");
                var instance = new ItemInstance("plate", "itm_armor_plate", 1, durability: 90, durabilityMax: 90);
                var facts = ItemFacts.Collect(template, instance, damageTypes.All);

                Equal(damageTypes.All.Count, Count(facts, ItemFactKind.Protection), "строка на тип урона");
                var index = 0;
                for (var damageTypeIndex = 0; damageTypeIndex < damageTypes.All.Count; damageTypeIndex++)
                {
                    var damageType = damageTypes.All[damageTypeIndex];
                    AssertProtection(facts, index++, damageType.Id, template.Protection[damageType.Id]);
                }

                Near(1, Fact(facts, ItemFactKind.ArtifactSlots).Value, "слоты артефактов");
                Near(90, Fact(facts, ItemFactKind.Durability).Value, "прочность");
                Near(90, Fact(facts, ItemFactKind.Durability).Max!.Value, "максимум прочности");
            });

            Check("характеристики: рюкзак — прибавка к лимиту веса без защиты", () =>
            {
                var template = items.Get("itm_backpack_small");
                var facts = ItemFacts.Collect(template, null, damageTypes.All);

                Near(10, Fact(facts, ItemFactKind.ExtraWeight).Value, "лимит веса");
                Equal(0, Count(facts, ItemFactKind.Protection), "защиты у рюкзака нет");
                Equal(0, Count(facts, ItemFactKind.Damage), "урона у рюкзака нет");
            });

            Check("характеристики: пояс — слоты артефактов", () =>
            {
                var template = items.Get("itm_belt_artifact");
                var facts = ItemFacts.Collect(template, null, damageTypes.All);

                Near(2, Fact(facts, ItemFactKind.ArtifactSlots).Value, "слоты артефактов");
                Near(0.8, Fact(facts, ItemFactKind.Weight).Value, "вес");
            });

            Check("характеристики: артефакт — модификаторы с учётом редкости", () =>
            {
                var template = items.Get("art_fire_ember");
                var instance = new ItemInstance("ember", "art_fire_ember", 1, variantKey: "art:legendary");
                var modifiers = artifacts.GetModifiers(instance);
                var facts = ItemFacts.Collect(template, instance, damageTypes.All, modifiers);

                Near(6, Fact(facts, ItemFactKind.HealthModifier).Value, "здоровье 4 × 1.6");
                Near(0.064, Fact(facts, ItemFactKind.AccuracyModifier).Value, "меткость 0.04 × 1.6");
                Near(5, Fact(facts, ItemFactKind.ProtectionModifier, "dmg_fire").Value, "защита от огня");
                Near(-1, Fact(facts, ItemFactKind.ProtectionModifier, "dmg_rupture").Value, "минус к разрыву");
                Equal(2, Count(facts, ItemFactKind.ProtectionModifier), "только ненулевые строки защиты");
            });

            Check("характеристики: расходник — лечение и остаток зарядов", () =>
            {
                var template = items.Get("itm_medkit_normal");
                var instance = new ItemInstance("medkit", "itm_medkit_normal", 1, charges: 2);
                var facts = ItemFacts.Collect(template, instance, damageTypes.All);

                Near(30, Fact(facts, ItemFactKind.Heal).Value, "лечение");
                Near(2, Fact(facts, ItemFactKind.Charges).Value, "заряды");
                Near(0.8, Fact(facts, ItemFactKind.Weight).Value, "вес");
            });

            Check("характеристики: расходник с одним зарядом не показывает счётчик", () =>
            {
                var template = items.Get("itm_medkit_normal");
                var instance = new ItemInstance("medkit", "itm_medkit_normal", 1, charges: 1);
                var facts = ItemFacts.Collect(template, instance, damageTypes.All);

                Equal(0, Count(facts, ItemFactKind.Charges), "счётчик зарядов скрыт");
            });

            Check("характеристики: расходник с травмами — число снимаемых травм", () =>
            {
                var template = items.Get("itm_splint");
                var facts = ItemFacts.Collect(template, null, damageTypes.All);

                Near(2, Fact(facts, ItemFactKind.HealsInjuries).Value, "снимает две травмы");
                Near(10, Fact(facts, ItemFactKind.Heal).Value, "лечение");
            });

            Check("характеристики: расходник с модификатором — временная защита", () =>
            {
                var template = items.Get("itm_stim_guard");
                var facts = ItemFacts.Collect(template, null, damageTypes.All);

                Near(3, Fact(facts, ItemFactKind.ProtectionModifier, "dmg_bullet").Value, "защита от пуль");
                Equal(0, Count(facts, ItemFactKind.Charges), "у одного заряда счётчика нет");
            });

            Check("характеристики: патроны — калибр и бронебойность", () =>
            {
                var template = items.Get("ammo_545_ap");
                var facts = ItemFacts.Collect(template, null, damageTypes.All);

                Equal("5.45x39", Fact(facts, ItemFactKind.Caliber).Code, "калибр");
                Near(0.05, Fact(facts, ItemFactKind.Penetration).Value, "бронебойность");
            });

            Check("характеристики: без экземпляра прочность не показывается", () =>
            {
                var template = items.Get("wpn_ak74");
                var facts = ItemFacts.Collect(template, null, damageTypes.All);

                Equal(0, Count(facts, ItemFactKind.Durability), "прочности нет");
            });

            Check("характеристики: без шаблона — исключение", () =>
            {
                Throws<ArgumentNullException>(
                    () => ItemFacts.Collect(null!, null, damageTypes.All),
                    "пустой шаблон");
            });

            Check("характеристики: без списка типов урона — исключение", () =>
            {
                var template = items.Get("wpn_ak74");
                Throws<ArgumentNullException>(
                    () => ItemFacts.Collect(template, null, null!),
                    "пустой список типов урона");
            });
        }

        private static ItemFact Fact(IReadOnlyList<ItemFact> facts, ItemFactKind kind)
        {
            foreach (var fact in facts)
            {
                if (fact.Kind == kind)
                {
                    return fact;
                }
            }

            throw new InvalidOperationException("Нет характеристики " + kind);
        }

        private static ItemFact Fact(IReadOnlyList<ItemFact> facts, ItemFactKind kind, string code)
        {
            foreach (var fact in facts)
            {
                if (fact.Kind == kind && string.Equals(fact.Code, code, StringComparison.Ordinal))
                {
                    return fact;
                }
            }

            throw new InvalidOperationException("Нет характеристики " + kind + " для " + code);
        }

        private static int Count(IReadOnlyList<ItemFact> facts, ItemFactKind kind)
        {
            var count = 0;
            foreach (var fact in facts)
            {
                if (fact.Kind == kind)
                {
                    count++;
                }
            }

            return count;
        }

        private void AssertProtection(IReadOnlyList<ItemFact> facts, int index, string damageTypeId, int value)
        {
            var protection = new List<ItemFact>();
            foreach (var fact in facts)
            {
                if (fact.Kind == ItemFactKind.Protection)
                {
                    protection.Add(fact);
                }
            }

            Equal(damageTypeId, protection[index].Code, "порядок типов урона");
            Near(value, protection[index].Value, "защита " + damageTypeId);
        }

        private void Near(double expected, double actual, string what)
        {
            if (Math.Abs(expected - actual) > 1e-6)
            {
                throw new InvalidOperationException($"{what}: ожидалось {expected}, получено {actual}");
            }
        }
    }
}
