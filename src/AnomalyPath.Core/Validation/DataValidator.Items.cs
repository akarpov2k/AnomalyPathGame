#nullable enable
using System.Collections.Generic;
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Core.Combat;
using AnomalyPath.Core.Data;
using AnomalyPath.Core.Localization;
using AnomalyPath.Core.Repositories;
using AnomalyPath.Core.Templates;

namespace AnomalyPath.Core.Validation
{
    /// <summary>Часть класса <see cref="DataValidator"/>: расходники, их ссылки и аптечки врагов.</summary>
    public sealed partial class DataValidator
    {
        private static void ValidateItems(LoadedData data, List<ValidationIssue> issues)
        {
            IItemRepository? items = null;
            try
            {
                items = ItemRepository.FromLoadedData(data);
            }
            catch (DataException error)
            {
                issues.Add(new ValidationIssue(error.Path, error.Code, error.Reason));
            }

            if (items == null)
            {
                return;
            }

            IInjuryRepository? injuries = null;
            try
            {
                injuries = InjuryRepository.FromLoadedData(data);
            }
            catch (DataException)
            {
                // Ошибки травм собраны в ValidateInjuries.
            }

            IDamageTypeRepository? damageTypes = null;
            try
            {
                damageTypes = DamageTypeRepository.FromLoadedData(data);
            }
            catch (DataException)
            {
                // Ошибки типов урона собраны своими проходами.
            }

            foreach (var item in items.All)
            {
                ValidateConsumable(item, injuries, damageTypes, issues);
            }

            ValidateActorMedkits(data, items, issues);
        }

        private static void ValidateConsumable(
            ItemTemplate item,
            IInjuryRepository? injuries,
            IDamageTypeRepository? damageTypes,
            List<ValidationIssue> issues)
        {
            var block = item.Consumable;
            if (block == null)
            {
                return;
            }

            foreach (var injuryId in block.HealsInjuries)
            {
                if (injuries != null && !injuries.Contains(injuryId))
                {
                    issues.Add(new ValidationIssue(
                        item.Id,
                        CoreErrors.InjuryNotFound,
                        CoreText.Format(CoreErrors.InjuryNotFound, injuryId)));
                }
            }

            foreach (var effect in block.Effects)
            {
                if (effect.Type != ConsumableEffectType.Modifier ||
                    effect.Stat != ConsumableStat.Protection ||
                    string.IsNullOrWhiteSpace(effect.DamageTypeId) ||
                    damageTypes == null ||
                    damageTypes.Contains(effect.DamageTypeId))
                {
                    continue;
                }

                issues.Add(new ValidationIssue(
                    item.Id,
                    CoreErrors.UnknownDamageType,
                    CoreText.Format(CoreErrors.UnknownDamageType, effect.DamageTypeId)));
            }
        }

        private static void ValidateActorMedkits(LoadedData data, IItemRepository items, List<ValidationIssue> issues)
        {
            IReadOnlyList<ActorTemplate> actors;
            try
            {
                actors = ActorRepository.FromLoadedData(data).All;
            }
            catch (DataException)
            {
                // Ошибки схемы актёров уже собраны в ValidateEntities.
                return;
            }

            foreach (var actor in actors)
            {
                var medkitItemId = actor.Combat?.MedkitItemId;
                if (medkitItemId == null)
                {
                    continue;
                }

                if (!items.TryGet(medkitItemId, out var medkit))
                {
                    issues.Add(new ValidationIssue(
                        actor.Id,
                        CoreErrors.ItemNotFound,
                        CoreText.Format(CoreErrors.ItemNotFound, medkitItemId)));
                    continue;
                }

                if (medkit.Consumable == null ||
                    !medkit.Consumable.UsableInCombat ||
                    medkit.Consumable.TotalHeal() <= 0)
                {
                    issues.Add(new ValidationIssue(
                        actor.Id,
                        CoreErrors.ActorMedkitItem,
                        CoreText.Format(CoreErrors.ActorMedkitItem, medkitItemId)));
                }
            }
        }
    }
}
