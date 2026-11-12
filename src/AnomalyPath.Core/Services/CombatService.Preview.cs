#nullable enable
using System;
using System.Collections.Generic;
using AnomalyPath.Core.Combat;
using AnomalyPath.Core.Inventory;
using AnomalyPath.Core.Templates;

namespace AnomalyPath.Core.Services
{
    /// <summary>Часть класса <see cref="CombatService"/>: предпросмотр действий без бросков.</summary>
    public sealed partial class CombatService
    {
        /// <inheritdoc />
        public CombatSkillPreview PreviewSkill(CombatState state, ItemInstance? weapon, string skillId)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (state.IsFinished)
            {
                return CombatSkillPreview.Failed(CombatActionAvailability.Finished);
            }

            if (!_skills.TryGet(skillId, out var skill) || skill == null)
            {
                return CombatSkillPreview.Failed(CombatActionAvailability.MissingSkill);
            }

            WeaponBlock? weaponBlock = null;
            if (weapon != null)
            {
                weaponBlock = _items.Get(weapon.ItemId).Weapon;
                if (weaponBlock == null)
                {
                    return CombatSkillPreview.Failed(CombatActionAvailability.NoWeapon);
                }
            }

            if (!IsOwnedByAttacker(state.Current, weapon, weaponBlock, skillId))
            {
                return CombatSkillPreview.Failed(CombatActionAvailability.MissingSkill);
            }

            var apCost = ActionApCost(state, state.Current, skill);
            var availability = SkillAvailability(state, weapon, skill);
            if (availability != CombatActionAvailability.Available)
            {
                return CombatSkillPreview.Failed(availability, apCost, skill.AmmoCost);
            }

            var attacker = state.Current;
            var defender = state.Opponent;
            var attack = attacker.Template.Combat?.Attack;
            var weaponMultiplier = WeaponStats.Multiplier(
                weapon,
                weapon == null ? null : _items.Get(weapon.ItemId),
                _rarities);
            var baseDamage = (weaponBlock?.Damage ?? attack?.Damage ?? 0) * weaponMultiplier;
            if (baseDamage <= 0)
            {
                return CombatSkillPreview.Failed(CombatActionAvailability.NoWeapon, apCost, skill.AmmoCost);
            }

            var penetration = AmmoPenetration(weapon);
            var fallbackType = weaponBlock?.DamageTypeId ?? attack?.DamageTypeId;
            var sourceId = weapon?.ItemId ?? attacker.Template.Id;
            var pityStep = BaseChance(state, attacker, weaponBlock, skill, weaponMultiplier) * 0.05;
            var initialPitySteps = pityStep > 0 ? (int)Math.Round(attacker.PityBonus / pityStep) : 0;

            var shots = new List<int>();
            foreach (var effect in skill.Effects)
            {
                if (effect.Type != SkillEffectType.Damage)
                {
                    continue;
                }

                var damageType = effect.DamageTypeId ?? fallbackType ?? string.Empty;
                var damage = EvaluateDamage(
                    defender,
                    damageType,
                    baseDamage * effect.Multiplier * (1 + attacker.Modifiers.DamageMultiplier),
                    penetration,
                    sourceId);
                for (var i = 0; i < effect.Hits; i++)
                {
                    shots.Add(damage);
                }
            }

            var expectedDamage = 0.0;
            var killChance = 0.0;
            if (shots.Count > 0)
            {
                EvaluateShots(
                    state,
                    weapon,
                    skill,
                    shots,
                    defender.Health,
                    pityStep,
                    initialPitySteps,
                    out expectedDamage,
                    out killChance);
            }

            return new CombatSkillPreview(
                true,
                CombatActionAvailability.Available,
                apCost,
                skill.AmmoCost,
                shots.Count,
                HitChance(state, weapon, skill),
                expectedDamage,
                killChance);
        }

        /// <summary>
        /// Считает распределение урона по выстрелам с «пожалей»-бонусом: шанс каждого выстрела
        /// зависит от серии промахов до него, поэтому ожидание нельзя свести к «n × p × урон».
        /// </summary>
        private void EvaluateShots(
            CombatState state,
            ItemInstance? weapon,
            SkillTemplate skill,
            IReadOnlyList<int> shots,
            double health,
            double pityStep,
            int initialPitySteps,
            out double expectedDamage,
            out double killChance)
        {
            expectedDamage = 0;
            killChance = 0;
            var limit = (int)Math.Ceiling(health);
            if (limit <= 0)
            {
                return;
            }

            var maxPity = initialPitySteps + shots.Count;
            var current = new double[maxPity + 1, limit + 1];
            current[initialPitySteps, 0] = 1;
            foreach (var damage in shots)
            {
                var next = new double[maxPity + 1, limit + 1];
                for (var k = 0; k <= maxPity; k++)
                {
                    var chance = Chance(state, weapon, skill, includePity: false, extraPity: k * pityStep);
                    for (var d = 0; d <= limit; d++)
                    {
                        var probability = current[k, d];
                        if (probability <= 0)
                        {
                            continue;
                        }

                        next[0, Math.Min(limit, d + damage)] += probability * chance;
                        if (k < maxPity)
                        {
                            next[k + 1, d] += probability * (1 - chance);
                        }
                    }
                }

                current = next;
            }

            for (var k = 0; k <= maxPity; k++)
            {
                for (var d = 0; d <= limit; d++)
                {
                    var probability = current[k, d];
                    if (probability <= 0)
                    {
                        continue;
                    }

                    expectedDamage += probability * d;
                    if (d >= health)
                    {
                        killChance += probability;
                    }
                }
            }
        }
    }
}
