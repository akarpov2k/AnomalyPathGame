#nullable enable
using System;
using System.Collections.Generic;
using AnomalyPath.Core.Artifacts;
using AnomalyPath.Core.Combat;
using AnomalyPath.Core.Data;
using AnomalyPath.Core.Equipment;
using AnomalyPath.Core.Json;
using AnomalyPath.Core.Localization;

namespace AnomalyPath.Core.Templates
{
    /// <summary>Описание предмета: только данные, без поведения.</summary>
    public sealed class ItemTemplate
    {
        private ItemTemplate(
            string id,
            string nameKey,
            string? descriptionKey,
            ItemCategory category,
            int footprintWidth,
            int footprintHeight,
            bool rotatable,
            int maxStack,
            double weight,
            int tier,
            Rarity rarity,
            string icon,
            WeaponBlock? weapon,
            ConsumableBlock? consumable,
            PriceBlock? price,
            System.Collections.Generic.IReadOnlyList<string> equipSlots,
            double extraWeight,
            int artifactSlots,
            System.Collections.Generic.IReadOnlyDictionary<string, int> protection,
            string? artifactType,
            int armorDurabilityMax,
            ArmorMaterial? armorMaterial,
            double durabilityFactor,
            double quality,
            string? caliber,
            bool unlosable,
            IReadOnlyList<ArtifactModifier> modifiers,
            IReadOnlyList<ArtifactModifier> extraProperties,
            double penetration = 0)
        {
            Id = id;
            NameKey = nameKey;
            DescriptionKey = descriptionKey;
            Category = category;
            FootprintWidth = footprintWidth;
            FootprintHeight = footprintHeight;
            Rotatable = rotatable;
            MaxStack = maxStack;
            Weight = weight;
            Tier = tier;
            Rarity = rarity;
            Icon = icon;
            Weapon = weapon;
            Consumable = consumable;
            Price = price;
            EquipSlots = equipSlots;
            ExtraWeight = extraWeight;
            ArtifactSlots = artifactSlots;
            Protection = protection;
            ArtifactType = artifactType;
            ArmorDurabilityMax = armorDurabilityMax;
            ArmorMaterial = armorMaterial;
            DurabilityFactor = durabilityFactor;
            Quality = quality;
            Caliber = caliber;
            Unlosable = unlosable;
            Modifiers = modifiers;
            ExtraProperties = extraProperties;
            Penetration = penetration;
        }

        /// <summary>Идентификатор предмета.</summary>
        public string Id { get; }

        /// <summary>Ключ локализации названия.</summary>
        public string NameKey { get; }

        /// <summary>Ключ локализации описания.</summary>
        public string? DescriptionKey { get; }

        /// <summary>Категория.</summary>
        public ItemCategory Category { get; }

        /// <summary>Ширина в клетках.</summary>
        public int FootprintWidth { get; }

        /// <summary>Высота в клетках.</summary>
        public int FootprintHeight { get; }

        /// <summary>Разрешён ли поворот.</summary>
        public bool Rotatable { get; }

        /// <summary>Предел стопки.</summary>
        public int MaxStack { get; }

        /// <summary>Вес одной единицы предмета в килограммах; больше нуля.</summary>
        public double Weight { get; }

        /// <summary>Тир предмета.</summary>
        public int Tier { get; }

        /// <summary>Базовая редкость шаблона; по умолчанию обычная.</summary>
        public Rarity Rarity { get; }

        /// <summary>Путь к иконке.</summary>
        public string Icon { get; }

        /// <summary>Блок оружия, если категория <c>weapon</c>.</summary>
        public WeaponBlock? Weapon { get; }

        /// <summary>Блок расходника, если категория <c>consumable</c>.</summary>
        public ConsumableBlock? Consumable { get; }

        /// <summary>Блок цены.</summary>
        public PriceBlock? Price { get; }

        /// <summary>Слоты экипировки, куда предмет можно надеть.</summary>
        public System.Collections.Generic.IReadOnlyList<string> EquipSlots { get; }

        /// <summary>Прибавка к лимиту веса (для категорий <c>backpack</c> и <c>armor</c>), килограммы.</summary>
        public double ExtraWeight { get; }

        /// <summary>Сколько слотов артефактов даёт предмет (броня и пояс).</summary>
        public int ArtifactSlots { get; }

        /// <summary>Надбавка к защите по типам урона (броня, шлем и артефакт).</summary>
        public System.Collections.Generic.IReadOnlyDictionary<string, int> Protection { get; }

        /// <summary>Тип артефакта: <c>fire</c>, <c>electric</c>, <c>chemical</c>, <c>gravity</c>.</summary>
        public string? ArtifactType { get; }

        /// <summary>Максимум прочности брони или шлема; 0 — не изнашивается.</summary>
        public int ArmorDurabilityMax { get; }

        /// <summary>Материал брони или шлема; <c>null</c> у остальных категорий.</summary>
        public ArmorMaterial? ArmorMaterial { get; }

        /// <summary>Множитель прочности материала в формуле износа.</summary>
        public double DurabilityFactor { get; }

        /// <summary>Качество предмета: множитель износа, по умолчанию 1.</summary>
        public double Quality { get; }

        /// <summary>Калибр патронов; только для категории <c>ammo</c>.</summary>
        public string? Caliber { get; }

        /// <summary>
        /// <c>true</c>, если предмет нельзя потерять при смерти: он остаётся в инвентаре
        /// или в слоте независимо от страховки и режима полной потери. По умолчанию <c>false</c>.
        /// </summary>
        public bool Unlosable { get; }

        /// <summary>Свойства артефакта при надевании: значения со знаком, ноль не допускается.</summary>
        public IReadOnlyList<ArtifactModifier> Modifiers { get; }

        /// <summary>Пул доп. свойств, из которого легендарная и уникальная редкость берут 1–2 записи.</summary>
        public IReadOnlyList<ArtifactModifier> ExtraProperties { get; }

        /// <summary>Бронебойность патрона (0..1); у не-патронов всегда 0.</summary>
        public double Penetration { get; }

        /// <summary>Строгое чтение: любое нарушение схемы — <see cref="DataException"/> с id и полем.</summary>
        /// <param name="value">Загруженная запись.</param>
        /// <param name="sourcePath">Путь для сообщения об ошибке.</param>
        /// <returns>Описание предмета.</returns>
        /// <exception cref="DataException">Поле отсутствует или имеет неверный тип.</exception>
        public static ItemTemplate FromJson(JsonValue value, string sourcePath)
        {
            var id = DataMap.RequireString(value, "id", sourcePath);
            var categoryCode = DataMap.RequireString(value, "category", sourcePath);
            if (!SchemaEnum.TryParse(categoryCode, out ItemCategory category))
            {
                throw new DataException(sourcePath, CoreErrors.UnknownItemCategory, categoryCode);
            }

            var footprint = DataMap.RequireIntPair(value, "footprint", sourcePath);
            var weight = DataMap.RequireDouble(value, "weight", sourcePath);
            if (weight <= 0 || double.IsNaN(weight) || double.IsInfinity(weight))
            {
                throw new DataException(sourcePath, CoreErrors.PositiveValue, "weight");
            }

            var extraWeight = DataMap.OptionalDouble(value, "extraWeight") ?? 0;
            if (extraWeight < 0 || double.IsNaN(extraWeight) || double.IsInfinity(extraWeight))
            {
                throw new DataException(sourcePath, CoreErrors.NonNegative, "extraWeight");
            }

            if (category != ItemCategory.Backpack && category != ItemCategory.Armor && extraWeight != 0)
            {
                throw new DataException(sourcePath, CoreErrors.ExtraWeightOnCategory, categoryCode);
            }

            var weapon = value.TryGet("weapon", out var weaponValue) ? ReadWeapon(weaponValue, id, sourcePath) : null;

            if (category == ItemCategory.Weapon && weapon == null)
            {
                throw new DataException(sourcePath, CoreErrors.WeaponWithoutBlock, id);
            }

            ConsumableBlock? consumable = null;
            if (value.TryGet("consumable", out var consumableValue))
            {
                if (category != ItemCategory.Consumable)
                {
                    throw new DataException(sourcePath, CoreErrors.ConsumableBlockOnOther, id);
                }

                consumable = ConsumableBlock.FromJson(consumableValue, id, sourcePath);
            }

            if (category == ItemCategory.Consumable && consumable == null)
            {
                throw new DataException(sourcePath, CoreErrors.ConsumableWithoutBlock, id);
            }

            var equipSlots = ReadEquipSlots(value, category);
            if (consumable != null && !consumable.UsableInCombat && HasQuickSlot(equipSlots))
            {
                throw new DataException(sourcePath, CoreErrors.QuickSlotOutOfCombat, id);
            }

            var price = value.TryGet("price", out var priceValue) ? ReadPrice(priceValue, sourcePath) : null;
            var artifactType = DataMap.OptionalString(value, "artifactType");

            var caliber = DataMap.OptionalString(value, "caliber");
            if (category == ItemCategory.Ammo && string.IsNullOrWhiteSpace(caliber))
            {
                throw new DataException(sourcePath, CoreErrors.MissingStringField, "caliber");
            }

            var penetration = DataMap.OptionalDouble(value, "penetration") ?? 0;
            if (penetration < 0 || penetration > 1 || double.IsNaN(penetration))
            {
                throw new DataException(sourcePath, CoreErrors.PenetrationRange, penetration);
            }

            if (category != ItemCategory.Ammo && penetration != 0)
            {
                throw new DataException(sourcePath, CoreErrors.PenetrationOnCategory, id);
            }

            if (category == ItemCategory.Artifact && string.IsNullOrWhiteSpace(artifactType))
            {
                throw new DataException(sourcePath, CoreErrors.ArtifactWithoutType, id);
            }

            var modifiers = ReadModifiers(value, "modifiers", sourcePath);
            var extraProperties = ReadModifiers(value, "extraProperties", sourcePath);
            if (category != ItemCategory.Artifact && (modifiers.Count > 0 || extraProperties.Count > 0))
            {
                throw new DataException(sourcePath, CoreErrors.ModifiersOnWrongCategory, categoryCode);
            }

            if (category == ItemCategory.Artifact && value.TryGet("protection", out _))
            {
                throw new DataException(sourcePath, CoreErrors.ArtifactProtectionBlock, id);
            }

            ArmorMaterial? armorMaterial = null;
            double durabilityFactor = 0;
            double quality = 0;
            if (category == ItemCategory.Armor || category == ItemCategory.Helmet)
            {
                var materialCode = DataMap.RequireString(value, "material", sourcePath);
                if (!SchemaEnum.TryParse(materialCode, out ArmorMaterial material))
                {
                    throw new DataException(sourcePath, CoreErrors.UnknownMaterial, materialCode);
                }

                armorMaterial = material;
                durabilityFactor = DataMap.RequireDouble(value, "durabilityFactor", sourcePath);
                quality = DataMap.RequireDouble(value, "quality", sourcePath);
            }

            return new ItemTemplate(
                id,
                DataMap.RequireString(value, "nameKey", sourcePath),
                DataMap.OptionalString(value, "descriptionKey"),
                category,
                footprint.Item1,
                footprint.Item2,
                DataMap.OptionalBool(value, "rotatable", true),
                DataMap.RequireInt(value, "maxStack", sourcePath),
                weight,
                DataMap.RequireInt(value, "tier", sourcePath),
                ReadRarity(value, sourcePath),
                DataMap.RequireString(value, "icon", sourcePath),
                weapon,
                consumable,
                price,
                equipSlots,
                extraWeight,
                DataMap.OptionalInt(value, "artifactSlots", 0),
                ReadProtection(value),
                artifactType,
                DataMap.OptionalInt(value, "durabilityMax", 0),
                armorMaterial,
                durabilityFactor,
                quality,
                caliber,
                DataMap.OptionalBool(value, "unlosable", false),
                modifiers,
                extraProperties,
                penetration);
        }

        /// <summary>Слоты экипировки по категории, если в данных не заданы явно.</summary>
        /// <param name="category">Категория предмета.</param>
        /// <returns>Список id слотов.</returns>
        public static IReadOnlyList<string> DefaultEquipSlots(ItemCategory category)
        {
            switch (category)
            {
                case ItemCategory.Weapon: return new[] { EquipmentSlots.Id(EquipmentSlot.Main) };
                case ItemCategory.Helmet: return new[] { EquipmentSlots.Id(EquipmentSlot.Helmet) };
                case ItemCategory.Armor: return new[] { EquipmentSlots.Id(EquipmentSlot.Armor) };
                case ItemCategory.Backpack: return new[] { EquipmentSlots.Id(EquipmentSlot.Backpack) };
                case ItemCategory.Belt: return new[] { EquipmentSlots.Id(EquipmentSlot.Belt) };
                case ItemCategory.Consumable: return new[] { EquipmentSlots.Quick };
                default: return Array.Empty<string>();
            }
        }

        private static IReadOnlyList<string> ReadEquipSlots(JsonValue value, ItemCategory category)
        {
            if (!value.TryGet("equipSlots", out var slots) || !slots.IsArray)
            {
                return DefaultEquipSlots(category);
            }

            return DataMap.OptionalStringArray(value, "equipSlots");
        }

        private static bool HasQuickSlot(IReadOnlyList<string> slots)
        {
            foreach (var slot in slots)
            {
                if (string.Equals(slot, EquipmentSlots.Quick, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Читает список модификаторов артефакта; повтор пары «характеристика + тип урона» запрещён.</summary>
        private static IReadOnlyList<ArtifactModifier> ReadModifiers(JsonValue value, string field, string sourcePath)
        {
            var result = new List<ArtifactModifier>();
            if (!value.TryGet(field, out var array))
            {
                return result;
            }

            if (!array.IsArray)
            {
                throw new DataException(sourcePath, CoreErrors.MissingArrayField, field);
            }

            var keys = new HashSet<string>(StringComparer.Ordinal);
            foreach (var item in array.Items)
            {
                var modifier = ArtifactModifier.FromJson(item, sourcePath);
                if (!keys.Add(modifier.Key))
                {
                    throw new DataException(sourcePath, CoreErrors.DuplicateModifier, modifier.Key);
                }

                result.Add(modifier);
            }

            return result;
        }

        private static System.Collections.Generic.IReadOnlyDictionary<string, int> ReadProtection(JsonValue value)
        {
            var protection = new System.Collections.Generic.Dictionary<string, int>(System.StringComparer.Ordinal);
            if (!value.TryGet("protection", out var block) || !block.IsObject)
            {
                return protection;
            }

            foreach (var pair in block.Members)
            {
                if (pair.Value.Kind == JsonKind.Number)
                {
                    protection[pair.Key] = (int)System.Math.Round(pair.Value.Number);
                }
            }

            return protection;
        }

        private static Rarity ReadRarity(JsonValue value, string sourcePath)
        {
            var code = DataMap.OptionalString(value, "rarity");
            if (code == null)
            {
                return global::AnomalyPath.Core.Templates.Rarity.Common;
            }

            if (!SchemaEnum.TryParse(code, out Rarity rarity))
            {
                throw new DataException(sourcePath, CoreErrors.UnknownRarity, code);
            }

            return rarity;
        }

        private static WeaponBlock ReadWeapon(JsonValue value, string itemId, string sourcePath)
        {
            if (!value.IsObject)
            {
                throw new DataException(sourcePath, CoreErrors.WeaponWithoutBlock, itemId);
            }

            var magazine = DataMap.RequireInt(value, "magazine", sourcePath);
            if (magazine < 1)
            {
                throw new DataException(sourcePath, CoreErrors.WeaponMagazineInvalid, itemId);
            }

            return new WeaponBlock(
                DataMap.RequireString(value, "classId", sourcePath),
                DataMap.RequireDouble(value, "damage", sourcePath),
                DataMap.OptionalDouble(value, "accuracyCoefficient") ?? DataMap.RequireDouble(value, "accuracy", sourcePath),
                magazine,
                DataMap.RequireString(value, "ammoCaliber", sourcePath),
                DataMap.RequireDouble(value, "reloadTime", sourcePath),
                DataMap.RequireString(value, "damageTypeId", sourcePath),
                DataMap.RequireInt(value, "durabilityMax", sourcePath),
                DataMap.RequireDouble(value, "durabilityPerShot", sourcePath),
                DataMap.OptionalStringArray(value, "skills"),
                DataMap.RequireString(value, "defaultSkillId", sourcePath),
                DataMap.OptionalBool(value, "reloadPerShell", false));
        }

        private static PriceBlock ReadPrice(JsonValue value, string sourcePath)
        {
            var modeCode = DataMap.RequireString(value, "mode", sourcePath);
            if (!SchemaEnum.TryParse(modeCode, out PriceMode mode))
            {
                throw new DataException(sourcePath, CoreErrors.UnknownPriceMode, modeCode);
            }

            return new PriceBlock(
                mode,
                DataMap.OptionalDouble(value, "buy"),
                DataMap.OptionalDouble(value, "sell"),
                DataMap.OptionalDouble(value, "base"),
                DataMap.OptionalString(value, "factionId"));
        }
    }
}
