#nullable enable
using System;
using System.Collections.Generic;
using AnomalyPath.Core.Combat;
using AnomalyPath.Core.Data;
using AnomalyPath.Core.Localization;
using AnomalyPath.Core.Repositories;

namespace AnomalyPath.Core.Validation
{
    /// <summary>Часть класса <see cref="DataValidator"/>: бой.</summary>
    public sealed partial class DataValidator
    {
        private static void AddWorldIssues(LoadedData data, List<ValidationIssue> issues)
        {
            foreach (var issue in WorldValidator.Validate(data))
            {
                issues.Add(issue);
            }
        }

        /// <summary>
        /// Каждое имя в выражении формулы объявлено в <c>variables</c> или есть в <c>Balance.constants</c>
        /// (схемы §19). Иначе ошибка обнаружилась бы только при расчёте, в середине игры.
        /// </summary>
        private static void ValidateFormulas(LoadedData data, List<ValidationIssue> issues)
        {
            try
            {
                var formulas = FormulaRepository.FromLoadedData(data);
                var balanceRecords = data.All("balance");
                var constants = balanceRecords.Count > 0
                    ? BalanceTemplate.FromJson(balanceRecords[0]).Constants
                    : BalanceTemplate.Empty.Constants;

                foreach (var formula in formulas.All)
                {
                    var declared = new HashSet<string>(formula.Variables, StringComparer.Ordinal);
                    foreach (var name in formula.UsedNames)
                    {
                        if (!declared.Contains(name) && !constants.ContainsKey(name))
                        {
                            issues.Add(new ValidationIssue(
                                formula.Id,
                                CoreErrors.FormulaUnknownVariable,
                                CoreText.Format(CoreErrors.FormulaUnknownVariable, name)));
                        }
                    }
                }
            }
            catch (DataException)
            {
                // Ошибки схемы формул уже собраны отдельными проходами.
            }
        }

        /// <summary>
        /// Формулы урона и опыта из <c>combat.json</c> разрешаются в загруженные формулы
        /// (схемы §19); без этого игра падала бы при сборке сервисов, а не в preflight.
        /// </summary>
        private static void ValidateCombatFormulas(LoadedData data, List<ValidationIssue> issues)
        {
            var combat = data.All("combat");
            if (combat.Count > 1)
            {
                issues.Add(new ValidationIssue(
                    "combat",
                    CoreErrors.CombatSettingsSingle,
                    CoreText.Format(CoreErrors.CombatSettingsSingle, combat.Count)));
            }

            if (combat.Count != 1)
            {
                // Отсутствие combat.json проверяет ValidateSettings; здесь нечего разрешать.
                return;
            }

            try
            {
                var formulas = FormulaRepository.FromLoadedData(data);
                var settings = CombatSettingsTemplate.FromJson(combat[0], "combat");
                if (!formulas.Contains(settings.DamageFormulaId))
                {
                    issues.Add(new ValidationIssue(
                        "combat",
                        CoreErrors.FormulaNotFound,
                        CoreText.Format(CoreErrors.FormulaNotFound, settings.DamageFormulaId)));
                }

                if (!formulas.Contains(settings.XpFormulaId))
                {
                    issues.Add(new ValidationIssue(
                        "combat",
                        CoreErrors.FormulaNotFound,
                        CoreText.Format(CoreErrors.FormulaNotFound, settings.XpFormulaId)));
                }
            }
            catch (DataException)
            {
                // Ошибки схемы уже собраны отдельными проходами.
            }
        }

        private static void ValidateActorAttacks(LoadedData data, List<ValidationIssue> issues)
        {
            try
            {
                var damageTypes = DamageTypeRepository.FromLoadedData(data);
                var actors = ActorRepository.FromLoadedData(data);
                var items = ItemRepository.FromLoadedData(data);
                foreach (var actor in actors.All)
                {
                    if (actor.Combat != null)
                    {
                        ValidateActorAttack(actor, actor.Combat.Attack, damageTypes, items, issues);
                    }
                }
            }
            catch (DataException)
            {
                // Ошибки схемы актёров уже собраны в ValidateEntities.
            }
        }

        private static void ValidateActorAttack(
            ActorTemplate actor,
            ActorAttackTemplate? attack,
            DamageTypeRepository damageTypes,
            ItemRepository items,
            List<ValidationIssue> issues)
        {
            if (actor.Type != ActorType.Enemy)
            {
                if (attack != null)
                {
                    issues.Add(new ValidationIssue(
                        actor.Id,
                        CoreErrors.AttackOnNonEnemy,
                        CoreText.Format(CoreErrors.AttackOnNonEnemy, actor.Id)));
                }

                return;
            }

            if (attack == null)
            {
                issues.Add(new ValidationIssue(
                    actor.Id,
                    CoreErrors.ActorWithoutAttack,
                    CoreText.Format(CoreErrors.ActorWithoutAttack, actor.Id)));
                return;
            }

            var isMutant = HasRole(actor.Roles, ActorRoles.Mutant);
            if (isMutant)
            {
                if (attack.WeaponIds.Count > 0)
                {
                    issues.Add(new ValidationIssue(
                        actor.Id,
                        CoreErrors.MutantWithWeapon,
                        CoreText.Format(CoreErrors.MutantWithWeapon, actor.Id)));
                }

                if (attack.DamageTypeId == null || !damageTypes.Contains(attack.DamageTypeId))
                {
                    issues.Add(new ValidationIssue(
                        actor.Id,
                        CoreErrors.UnknownDamageType,
                        CoreText.Format(CoreErrors.UnknownDamageType, attack.DamageTypeId ?? string.Empty)));
                }

                return;
            }

            if (attack.IsNatural)
            {
                issues.Add(new ValidationIssue(
                    actor.Id,
                    CoreErrors.EnemyWithoutWeapon,
                    CoreText.Format(CoreErrors.EnemyWithoutWeapon, actor.Id)));
                return;
            }

            foreach (var weaponId in attack.WeaponIds)
            {
                if (!items.TryGet(weaponId, out var template))
                {
                    issues.Add(new ValidationIssue(
                        actor.Id,
                        CoreErrors.ItemNotFound,
                        CoreText.Format(CoreErrors.ItemNotFound, weaponId)));
                }
                else if (template.Weapon == null)
                {
                    issues.Add(new ValidationIssue(
                        actor.Id,
                        CoreErrors.WeaponWithoutBlock,
                        CoreText.Format(CoreErrors.WeaponWithoutBlock, weaponId)));
                }
            }
        }

        private static void ValidateSkills(LoadedData data, List<ValidationIssue> issues)
        {
            try
            {
                var skills = SkillRepository.FromLoadedData(data);
                var items = ItemRepository.FromLoadedData(data);
                var actors = ActorRepository.FromLoadedData(data);
                var damageTypes = DamageTypeRepository.FromLoadedData(data);

                foreach (var item in items.All)
                {
                    if (item.Weapon == null)
                    {
                        continue;
                    }

                    foreach (var skillId in item.Weapon.Skills)
                    {
                        if (!skills.Contains(skillId))
                        {
                            issues.Add(new ValidationIssue(
                                item.Id,
                                CoreErrors.SkillNotFound,
                                CoreText.Format(CoreErrors.SkillNotFound, skillId)));
                        }
                    }

                    if (skills.All.Count > 0 && !ContainsSkill(item.Weapon.Skills, item.Weapon.DefaultSkillId))
                    {
                        issues.Add(new ValidationIssue(
                            item.Id,
                            CoreErrors.WeaponDefaultSkillMissing,
                            CoreText.Format(
                                CoreErrors.WeaponDefaultSkillMissing,
                                item.Id,
                                item.Weapon.DefaultSkillId)));
                    }
                }

                foreach (var actor in actors.All)
                {
                    var block = actor.Combat;
                    if (block == null)
                    {
                        continue;
                    }

                    if (block.Skills.Count > 0)
                    {
                        foreach (var skillId in block.Skills)
                        {
                            if (!skills.Contains(skillId))
                            {
                                issues.Add(new ValidationIssue(
                                    actor.Id,
                                    CoreErrors.SkillNotFound,
                                    CoreText.Format(CoreErrors.SkillNotFound, skillId)));
                                continue;
                            }

                            if (skills.TryGet(skillId, out var mutantSkill) &&
                                mutantSkill != null &&
                                mutantSkill.AmmoCost > 0)
                            {
                                issues.Add(new ValidationIssue(
                                    actor.Id,
                                    CoreErrors.MutantSkillAmmo,
                                    CoreText.Format(CoreErrors.MutantSkillAmmo, actor.Id, skillId)));
                            }
                        }
                    }

                    if (block.DefaultSkillId == null)
                    {
                        continue;
                    }

                    if (!ContainsSkill(block.Skills, block.DefaultSkillId))
                    {
                        issues.Add(new ValidationIssue(
                            actor.Id,
                            CoreErrors.DefaultSkillNotListed,
                            CoreText.Format(CoreErrors.DefaultSkillNotListed, block.DefaultSkillId)));
                        continue;
                    }

                    if (skills.TryGet(block.DefaultSkillId, out var defaultSkill) &&
                        defaultSkill != null &&
                        defaultSkill.Kind != SkillKind.Attack)
                    {
                        issues.Add(new ValidationIssue(
                            actor.Id,
                            CoreErrors.DefaultSkillNotAttack,
                            CoreText.Format(CoreErrors.DefaultSkillNotAttack, block.DefaultSkillId)));
                    }
                }

                foreach (var skill in skills.All)
                {
                    foreach (var effect in skill.Effects)
                    {
                        if (effect.DamageTypeId != null && !damageTypes.Contains(effect.DamageTypeId))
                        {
                            issues.Add(new ValidationIssue(
                                skill.Id,
                                CoreErrors.UnknownDamageType,
                                CoreText.Format(CoreErrors.UnknownDamageType, effect.DamageTypeId)));
                        }
                    }
                }
            }
            catch (DataException)
            {
                // Ошибки схемы скиллов уже собраны в ValidateEntities.
            }
        }

        private static bool HasRole(IReadOnlyList<string> roles, string role)
        {
            for (var i = 0; i < roles.Count; i++)
            {
                if (string.Equals(roles[i], role, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ContainsSkill(IReadOnlyList<string> values, string value)
        {
            for (var i = 0; i < values.Count; i++)
            {
                if (string.Equals(values[i], value, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
