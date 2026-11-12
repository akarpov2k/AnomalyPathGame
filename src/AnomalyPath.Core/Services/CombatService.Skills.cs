#nullable enable
using System;
using System.Collections.Generic;
using AnomalyPath.Core.Combat;
using AnomalyPath.Core.Inventory;
using AnomalyPath.Core.Templates;

namespace AnomalyPath.Core.Services
{
    /// <summary>Часть класса <see cref="CombatService"/>: действия из скиллов оружия и скиллов без оружия.</summary>
    public sealed partial class CombatService
    {
        /// <inheritdoc />
        public IReadOnlyList<CombatAction> GetActions(CombatState state, ItemInstance? weapon)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            var actions = new List<CombatAction>();

            var skillIds = SkillIds(state.Current, weapon);
            if (skillIds == null)
            {
                return actions;
            }

            foreach (var skillId in skillIds)
            {
                if (!_skills.TryGet(skillId, out var skill) || skill == null)
                {
                    actions.Add(new CombatAction(
                        CombatActionKind.Skill,
                        skillId,
                        0,
                        0,
                        CombatActionAvailability.MissingSkill));
                    continue;
                }

                actions.Add(new CombatAction(
                    CombatActionKind.Skill,
                    skill.Id,
                    ActionApCost(state, state.Current, skill),
                    skill.AmmoCost,
                    SkillAvailability(state, weapon, skill)));
            }

            return actions;
        }

        /// <inheritdoc />
        public bool TryUseSkill(CombatState state, ItemInstance? weapon, string skillId, out CombatSkillResult result)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            result = CombatSkillResult.Failed(CombatActionAvailability.MissingSkill);
            if (state.IsFinished)
            {
                result = CombatSkillResult.Failed(CombatActionAvailability.Finished);
                return false;
            }

            WeaponBlock? weaponBlock = null;
            if (weapon != null)
            {
                var template = _items.Get(weapon.ItemId);
                weaponBlock = template.Weapon;
                if (weaponBlock == null)
                {
                    result = CombatSkillResult.Failed(CombatActionAvailability.NoWeapon);
                    return false;
                }

                if (weapon.IsBroken)
                {
                    result = CombatSkillResult.Failed(CombatActionAvailability.BrokenWeapon);
                    return false;
                }
            }

            if (!_skills.TryGet(skillId, out var skill) || skill == null)
            {
                result = CombatSkillResult.Failed(CombatActionAvailability.MissingSkill);
                return false;
            }

            if (!IsOwnedByAttacker(state.Current, weapon, weaponBlock, skillId))
            {
                result = CombatSkillResult.Failed(CombatActionAvailability.MissingSkill);
                return false;
            }

            var availability = SkillAvailability(state, weapon, skill);
            if (availability != CombatActionAvailability.Available)
            {
                result = CombatSkillResult.Failed(availability);
                return false;
            }

            result = ExecuteSkill(state, weapon, weaponBlock, skill);
            return true;
        }

        private CombatSkillResult ExecuteSkill(
            CombatState state,
            ItemInstance? weapon,
            WeaponBlock? weaponBlock,
            SkillTemplate skill)
        {
            var attacker = state.Current;
            var defender = state.Opponent;
            var attack = attacker.Template.Combat?.Attack;
            var apCost = ActionApCost(state, attacker, skill);
            var hits = new List<CombatAttackResult>();
            var injuries = new List<string>();
            var totalDamage = 0;
            var killed = false;

            var weaponMultiplier = WeaponStats.Multiplier(
                weapon,
                weapon == null ? null : _items.Get(weapon.ItemId),
                _rarities);
            var baseDamage = (weaponBlock?.Damage ?? attack?.Damage ?? 0) * weaponMultiplier;
            if (baseDamage <= 0)
            {
                return CombatSkillResult.Failed(CombatActionAvailability.NoWeapon);
            }

            var penetration = AmmoPenetration(weapon);
            var fallbackDamageType = weaponBlock?.DamageTypeId ?? attack?.DamageTypeId;

            attacker.Ap -= apCost;
            if (weapon != null)
            {
                weapon.Magazine = Math.Max(0, weapon.MagazineRounds - skill.AmmoCost);
            }

            foreach (var effect in skill.Effects)
            {
                if (effect.Type == SkillEffectType.Damage)
                {
                    var damageType = effect.DamageTypeId ?? fallbackDamageType ?? string.Empty;
                    for (var i = 0; i < effect.Hits; i++)
                    {
                        ResolveShot(
                            state,
                            weapon,
                            weaponBlock,
                            skill,
                            attacker,
                            defender,
                            baseDamage,
                            damageType,
                            effect.Multiplier,
                            penetration,
                            out var damage,
                            out var shot);
                        totalDamage += damage;
                        hits.Add(shot);
                        if (shot.Hit && !defender.IsDead)
                        {
                            var injuryId = TryInflictInjury(defender, skill);
                            if (injuryId != null)
                            {
                                injuries.Add(injuryId);
                            }
                        }

                        if (defender.IsDead)
                        {
                            killed = true;
                            break;
                        }
                    }

                    if (killed)
                    {
                        break;
                    }

                    continue;
                }

                if (effect.Type == SkillEffectType.Status)
                {
                    ApplyStatusEffect(effect, attacker, defender);
                    continue;
                }

                if (effect.Type == SkillEffectType.RemoveStatus && effect.StatusId != null)
                {
                    var statusTarget = effect.Target == SkillEffectTarget.Self ? attacker : defender;
                    statusTarget.RemoveStatus(effect.StatusId);
                }
            }

            return new CombatSkillResult(
                true,
                CombatActionAvailability.Available,
                hits,
                totalDamage,
                killed,
                apCost,
                skill.AmmoCost,
                injuries);
        }

        /// <summary>
        /// Бросок травмы за попадание: сначала равномерно выбирается одна травма из списка скилла,
        /// затем бросается её шанс. За одно попадание — не больше одной травмы.
        /// </summary>
        private string? TryInflictInjury(Combatant defender, SkillTemplate skill)
        {
            if (skill.Injuries.Count == 0)
            {
                return null;
            }

            var index = (int)(_random.NextDouble() * skill.Injuries.Count);
            if (index < 0 || index >= skill.Injuries.Count)
            {
                index = skill.Injuries.Count - 1;
            }

            var template = _injuries.Get(skill.Injuries[index]);
            if (_random.NextDouble() >= template.Chance)
            {
                return null;
            }

            defender.ApplyStatus(new CombatStatus(template, template.DurationAmount));
            return template.Id;
        }

        private static void ApplyStatusEffect(SkillEffect effect, Combatant attacker, Combatant defender)
        {
            if (effect.StatusId == null)
            {
                return;
            }

            var target = effect.Target == SkillEffectTarget.Self ? attacker : defender;
            target.ApplyStatus(new CombatStatus(
                effect.StatusId,
                null,
                new CombatModifiers(
                    effect.AccuracyBonus,
                    effect.ActionApCostBonus,
                    null,
                    effect.DamageMultiplier),
                InjuryDurationKind.Turns,
                effect.Turns));
        }

        private void ResolveShot(
            CombatState state,
            ItemInstance? weapon,
            WeaponBlock? weaponBlock,
            SkillTemplate skill,
            Combatant attacker,
            Combatant defender,
            double baseDamage,
            string damageType,
            double multiplier,
            double penetration,
            out int damage,
            out CombatAttackResult shot)
        {
            var chance = HitChance(state, weapon, skill);
            var weaponMultiplier = WeaponStats.Multiplier(
                weapon,
                weapon == null ? null : _items.Get(weapon.ItemId),
                _rarities);
            var roll = _random.NextDouble();
            var hit = roll < chance;
            damage = 0;
            if (hit)
            {
                attacker.PityBonus = 0;
                damage = EvaluateDamage(
                    defender,
                    damageType,
                    baseDamage * multiplier * (1 + attacker.Modifiers.DamageMultiplier),
                    penetration,
                    weapon?.ItemId ?? attacker.Template.Id);
            }
            else
            {
                attacker.PityBonus += BaseChance(state, attacker, weaponBlock, skill, weaponMultiplier) * 0.05;
            }

            defender.Health = Math.Max(0, defender.Health - damage);
            ApplyWear(weapon, defender, damage);
            shot = new CombatAttackResult(hit, damage, defender.IsDead, roll, chance);
        }

        private int ActionApCost(CombatState state, Combatant actor, SkillTemplate skill) =>
            Math.Max(0, skill.ApCost + actor.Modifiers.ActionApCostBonus + PlayerApPenalty(state, actor));

        /// <summary>Скиллы действующего: из оружия, а без оружия — из самого актёра.</summary>
        private IReadOnlyList<string>? SkillIds(Combatant actor, ItemInstance? weapon)
        {
            if (weapon != null)
            {
                var template = _items.Get(weapon.ItemId);
                return template.Weapon?.Skills;
            }

            return actor.Template.Combat?.Skills;
        }

        private static bool IsOwnedByAttacker(
            Combatant actor,
            ItemInstance? weapon,
            WeaponBlock? weaponBlock,
            string skillId)
        {
            var skills = weapon != null ? weaponBlock?.Skills : actor.Template.Combat?.Skills;
            if (skills == null)
            {
                return false;
            }

            return IsListed(skills, skillId);
        }

        private CombatActionAvailability SkillAvailability(
            CombatState state,
            ItemInstance? weapon,
            SkillTemplate skill)
        {
            if (state.IsFinished)
            {
                return CombatActionAvailability.Finished;
            }

            var attacker = state.Current;
            if (weapon != null)
            {
                var weaponBlock = _items.Get(weapon.ItemId).Weapon;
                if (weaponBlock == null)
                {
                    return CombatActionAvailability.NoWeapon;
                }

                if (weapon.IsBroken)
                {
                    return CombatActionAvailability.BrokenWeapon;
                }

                if (weapon.MagazineRounds < skill.AmmoCost)
                {
                    return CombatActionAvailability.NoAmmo;
                }
            }

            return attacker.Ap >= ActionApCost(state, attacker, skill)
                ? CombatActionAvailability.Available
                : CombatActionAvailability.NoAp;
        }

        private static bool IsListed(IReadOnlyList<string> skills, string skillId)
        {
            for (var i = 0; i < skills.Count; i++)
            {
                if (string.Equals(skills[i], skillId, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
