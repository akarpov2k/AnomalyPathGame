#nullable enable
using System;
using System.Collections.Generic;
using AnomalyPath.Core.Artifacts;
using AnomalyPath.Core.Templates;

namespace AnomalyPath.Core.Inventory
{
    /// <summary>
    /// Собирает характеристики предмета для подсказки: вес, параметры оружия, защиту, прочность,
    /// модификаторы артефакта и эффекты расходника. Тексты сюда не входят — их подставляет интерфейс.
    /// </summary>
    public static class ItemFacts
    {
        /// <summary>Возвращает характеристики предмета в порядке показа.</summary>
        /// <param name="template">Шаблон предмета.</param>
        /// <param name="instance">Экземпляр с прочностью и зарядами; <c>null</c> — только данные шаблона.</param>
        /// <param name="damageTypes">Типы урона в порядке данных: задают порядок строк защиты.</param>
        /// <param name="artifactModifiers">
        /// Действующие модификаторы артефакта с учётом редкости; <c>null</c> — базовые значения шаблона.
        /// </param>
        /// <param name="weaponStatMultiplier">
        /// Множитель урона и точности оружия по редкости; 1 — без прибавки.
        /// </param>
        /// <returns>Список характеристик; пустой список невозможен — вес есть у любого предмета.</returns>
        /// <exception cref="ArgumentNullException">Шаблон или список типов урона не передан.</exception>
        public static IReadOnlyList<ItemFact> Collect(
            ItemTemplate template,
            ItemInstance? instance,
            IReadOnlyList<DamageTypeTemplate> damageTypes,
            ArtifactModifiers? artifactModifiers = null,
            double weaponStatMultiplier = 1)
        {
            if (template == null)
            {
                throw new ArgumentNullException(nameof(template));
            }

            if (damageTypes == null)
            {
                throw new ArgumentNullException(nameof(damageTypes));
            }

            var facts = new List<ItemFact>
            {
                new ItemFact(ItemFactKind.Weight, template.Weight)
            };

            switch (template.Category)
            {
                case ItemCategory.Weapon:
                    AddWeapon(facts, template, instance, weaponStatMultiplier);
                    break;
                case ItemCategory.Armor:
                case ItemCategory.Helmet:
                    AddDurability(facts, instance);
                    AddProtection(facts, template.Protection, damageTypes);
                    AddArtifactSlots(facts, template.ArtifactSlots);
                    break;
                case ItemCategory.Backpack:
                    AddExtraWeight(facts, template.ExtraWeight);
                    break;
                case ItemCategory.Belt:
                    AddArtifactSlots(facts, template.ArtifactSlots);
                    break;
                case ItemCategory.Artifact:
                    AddArtifactModifiers(facts, template, damageTypes, artifactModifiers);
                    break;
                case ItemCategory.Ammo:
                    AddAmmo(facts, template);
                    break;
                case ItemCategory.Consumable:
                    AddConsumable(facts, template, instance);
                    break;
            }

            return facts;
        }

        private static void AddWeapon(
            List<ItemFact> facts,
            ItemTemplate template,
            ItemInstance? instance,
            double weaponStatMultiplier)
        {
            var weapon = template.Weapon;
            if (weapon != null)
            {
                facts.Add(new ItemFact(ItemFactKind.Damage, weapon.Damage * weaponStatMultiplier));
                facts.Add(new ItemFact(ItemFactKind.Magazine, weapon.Magazine));
                AddCaliber(facts, weapon.AmmoCaliber);
            }

            AddDurability(facts, instance);
        }

        private static void AddAmmo(List<ItemFact> facts, ItemTemplate template)
        {
            AddCaliber(facts, template.Caliber);
            if (template.Penetration > 0)
            {
                facts.Add(new ItemFact(ItemFactKind.Penetration, template.Penetration));
            }
        }

        private static void AddCaliber(List<ItemFact> facts, string? caliber)
        {
            if (!string.IsNullOrWhiteSpace(caliber))
            {
                facts.Add(new ItemFact(ItemFactKind.Caliber, 0, code: caliber));
            }
        }

        private static void AddDurability(List<ItemFact> facts, ItemInstance? instance)
        {
            if (instance?.Durability is int current &&
                instance.DurabilityMax is int max &&
                max > 0)
            {
                facts.Add(new ItemFact(ItemFactKind.Durability, current, max));
            }
        }

        private static void AddProtection(
            List<ItemFact> facts,
            IReadOnlyDictionary<string, int> protection,
            IReadOnlyList<DamageTypeTemplate> damageTypes)
        {
            foreach (var damageType in damageTypes)
            {
                if (protection.TryGetValue(damageType.Id, out var value) && value != 0)
                {
                    facts.Add(new ItemFact(ItemFactKind.Protection, value, code: damageType.Id));
                }
            }
        }

        private static void AddArtifactSlots(List<ItemFact> facts, int slots)
        {
            if (slots > 0)
            {
                facts.Add(new ItemFact(ItemFactKind.ArtifactSlots, slots));
            }
        }

        private static void AddExtraWeight(List<ItemFact> facts, double extraWeight)
        {
            if (extraWeight > 0)
            {
                facts.Add(new ItemFact(ItemFactKind.ExtraWeight, extraWeight));
            }
        }

        private static void AddArtifactModifiers(
            List<ItemFact> facts,
            ItemTemplate template,
            IReadOnlyList<DamageTypeTemplate> damageTypes,
            ArtifactModifiers? artifactModifiers)
        {
            var modifiers = artifactModifiers ?? new ArtifactModifiers(template.Modifiers, 1);
            if (modifiers.HealthMax != 0)
            {
                facts.Add(new ItemFact(ItemFactKind.HealthModifier, modifiers.HealthMax));
            }

            if (Math.Abs(modifiers.Accuracy) > 1e-9)
            {
                facts.Add(new ItemFact(ItemFactKind.AccuracyModifier, modifiers.Accuracy));
            }

            if (modifiers.Stamina != 0)
            {
                facts.Add(new ItemFact(ItemFactKind.StaminaModifier, modifiers.Stamina));
            }

            foreach (var damageType in damageTypes)
            {
                var value = modifiers.ProtectionOf(damageType.Id);
                if (value != 0)
                {
                    facts.Add(new ItemFact(ItemFactKind.ProtectionModifier, value, code: damageType.Id));
                }
            }
        }

        private static void AddConsumable(List<ItemFact> facts, ItemTemplate template, ItemInstance? instance)
        {
            var consumable = template.Consumable;
            if (consumable == null)
            {
                return;
            }

            var charges = instance?.Charges ?? consumable.Charges;
            if (charges > 1)
            {
                facts.Add(new ItemFact(ItemFactKind.Charges, charges));
            }

            var heal = consumable.TotalHeal();
            if (heal > 0)
            {
                facts.Add(new ItemFact(ItemFactKind.Heal, heal));
            }

            var ap = consumable.TotalApRestore();
            if (ap > 0)
            {
                facts.Add(new ItemFact(ItemFactKind.ApRestore, ap));
            }

            var food = consumable.TotalFoodRestore();
            if (food > 0)
            {
                facts.Add(new ItemFact(ItemFactKind.Food, food));
            }

            var water = consumable.TotalWaterRestore();
            if (water > 0)
            {
                facts.Add(new ItemFact(ItemFactKind.Water, water));
            }

            if (consumable.HealsInjuries.Count > 0)
            {
                facts.Add(new ItemFact(ItemFactKind.HealsInjuries, consumable.HealsInjuries.Count));
            }

            foreach (var effect in consumable.Effects)
            {
                if (effect.Type != ConsumableEffectType.Modifier)
                {
                    continue;
                }

                var stat = effect.Stat;
                if (stat == null)
                {
                    continue;
                }

                switch (stat.Value)
                {
                    case ConsumableStat.Accuracy:
                        facts.Add(new ItemFact(ItemFactKind.AccuracyModifier, effect.Value));
                        break;
                    case ConsumableStat.Protection:
                        facts.Add(new ItemFact(ItemFactKind.ProtectionModifier, effect.Value, code: effect.DamageTypeId));
                        break;
                    case ConsumableStat.ActionApCost:
                        facts.Add(new ItemFact(ItemFactKind.ActionApCostModifier, effect.Value));
                        break;
                }
            }
        }
    }
}
