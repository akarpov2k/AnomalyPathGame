#nullable enable
using System;
using System.Collections.Generic;
using AnomalyPath.Core.Data;
using AnomalyPath.Core.Equipment;
using AnomalyPath.Core.Localization;
using AnomalyPath.Core.Repositories;
using AnomalyPath.Core.Rules;
using AnomalyPath.Core.Templates;

namespace AnomalyPath.Core.Validation
{
    /// <summary>Часть класса <see cref="DataValidator"/>: правила, снаряжение и защита.</summary>
    public sealed partial class DataValidator
    {
        private static void ValidateProtectionKeys(LoadedData data, List<ValidationIssue> issues)        {
            DamageTypeRepository? damageTypes = null;
            ItemRepository? items = null;
            ActorRepository? actors = null;
            try
            {
                damageTypes = DamageTypeRepository.FromLoadedData(data);
            }
            catch (DataException)
            {
                // Без типов урона проверять ключи нечем.
            }

            try
            {
                items = ItemRepository.FromLoadedData(data);
            }
            catch (DataException)
            {
                // Ошибки предметов уже собраны отдельным проходом.
            }

            try
            {
                actors = ActorRepository.FromLoadedData(data);
            }
            catch (DataException)
            {
                // Ошибки актёров уже собраны отдельным проходом.
            }

            if (damageTypes == null)
            {
                return;
            }

            if (items != null)
            {
                foreach (var item in items.All)
                {
                    if (item.Protection.Count > 0 && !AllowsProtection(item.Category))
                    {
                        issues.Add(new ValidationIssue(
                            item.Id,
                            CoreErrors.ProtectionOnWrongCategory,
                            CoreText.Format(CoreErrors.ProtectionOnWrongCategory, SchemaEnum.ToCode(item.Category))));
                    }

                    foreach (var pair in item.Protection)
                    {
                        if (pair.Value < 0)
                        {
                            issues.Add(new ValidationIssue(
                                item.Id,
                                CoreErrors.NonNegative,
                                CoreText.Format(CoreErrors.NonNegative, "protection." + pair.Key)));
                        }

                        if (!damageTypes.Contains(pair.Key))
                        {
                            issues.Add(new ValidationIssue(
                                item.Id,
                                CoreErrors.UnknownDamageType,
                                CoreText.Format(CoreErrors.UnknownDamageType, pair.Key)));
                        }
                    }

                    foreach (var modifier in item.Modifiers)
                    {
                        ValidateModifierDamageType(item.Id, modifier, damageTypes, issues);
                    }

                    foreach (var modifier in item.ExtraProperties)
                    {
                        ValidateModifierDamageType(item.Id, modifier, damageTypes, issues);
                    }
                }
            }

            if (actors == null)
            {
                return;
            }

            foreach (var actor in actors.All)
            {
                var block = actor.Combat;
                if (block == null)
                {
                    continue;
                }

                foreach (var pair in block.Protection)
                {
                    if (pair.Value < 0)
                    {
                        issues.Add(new ValidationIssue(
                            actor.Id,
                            CoreErrors.NonNegative,
                            CoreText.Format(CoreErrors.NonNegative, "protection." + pair.Key)));
                    }

                    if (!damageTypes.Contains(pair.Key))
                    {
                        issues.Add(new ValidationIssue(
                            actor.Id,
                            CoreErrors.UnknownDamageType,
                            CoreText.Format(CoreErrors.UnknownDamageType, pair.Key)));
                    }
                }
            }
        }

        private static bool AllowsProtection(ItemCategory category) =>
            category == ItemCategory.Armor ||
            category == ItemCategory.Helmet;

        private static void ValidateModifierDamageType(
            string itemId,
            Artifacts.ArtifactModifier modifier,
            DamageTypeRepository damageTypes,
            List<ValidationIssue> issues)
        {
            if (modifier.Stat != Artifacts.ArtifactStat.Protection)
            {
                return;
            }

            var damageType = modifier.DamageTypeId;
            if (damageType == null || !damageTypes.Contains(damageType))
            {
                issues.Add(new ValidationIssue(
                    itemId,
                    CoreErrors.UnknownDamageType,
                    CoreText.Format(CoreErrors.UnknownDamageType, damageType ?? "<null>")));
            }
        }

        private static void ValidateRules(LoadedData data, List<ValidationIssue> issues)
        {
            var records = data.All("rules");
            if (records.Count > 1)
            {
                issues.Add(new ValidationIssue(
                    "rules",
                    CoreErrors.RulesSingleRecord,
                    CoreText.Format(CoreErrors.RulesSingleRecord, records.Count)));
            }

            if (records.Count != 1)
            {
                return;
            }

            try
            {
                var rules = GameRulesTemplate.FromJson(records[0], "rules");
                var actors = ActorRepository.FromLoadedData(data);
                if (!actors.Contains(rules.PlayerActorId))
                {
                    issues.Add(new ValidationIssue(
                        "rules",
                        CoreErrors.ActorNotFound,
                        CoreText.Format(CoreErrors.ActorNotFound, rules.PlayerActorId)));
                }
            }
            catch (DataException)
            {
                // Ошибки схемы уже собраны в ValidateEntities.
            }
        }

        private static void ValidateLoadouts(LoadedData data, List<ValidationIssue> issues)
        {
            ItemRepository items;
            LoadoutRepository loadouts;
            try
            {
                items = ItemRepository.FromLoadedData(data);
                loadouts = LoadoutRepository.FromLoadedData(data);
            }
            catch (DataException)
            {
                return;
            }

            foreach (var loadout in loadouts.All)
            {
                foreach (var item in loadout.Items)
                {
                    if (!items.TryGet(item.ItemId, out _))
                    {
                        issues.Add(new ValidationIssue(
                            loadout.Id,
                            CoreErrors.LoadoutItemNotFound,
                            CoreText.Format(CoreErrors.LoadoutItemNotFound, item.ItemId)));
                    }
                }

                foreach (var pair in loadout.Equipment)
                {
                    if (!items.TryGet(pair.Value, out var template))
                    {
                        issues.Add(new ValidationIssue(
                            loadout.Id,
                            CoreErrors.LoadoutItemNotFound,
                            CoreText.Format(CoreErrors.LoadoutItemNotFound, pair.Value)));
                        continue;
                    }

                    var slotId = EquipmentSlots.Id(pair.Key);
                    if (!AllowsSlot(template.EquipSlots, slotId))
                    {
                        issues.Add(new ValidationIssue(
                            loadout.Id,
                            CoreErrors.LoadoutSlotMismatch,
                            CoreText.Format(CoreErrors.LoadoutSlotMismatch, pair.Value, slotId)));
                    }
                }
            }
        }

        private static bool AllowsSlot(IReadOnlyList<string> allowed, string slotId)
        {
            foreach (var slot in allowed)
            {
                if (string.Equals(slot, slotId, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
