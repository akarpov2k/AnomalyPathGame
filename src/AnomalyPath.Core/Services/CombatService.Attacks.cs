#nullable enable
using System;
using System.Collections.Generic;
using AnomalyPath.Core.Combat;
using AnomalyPath.Core.Data;
using AnomalyPath.Core.Inventory;
using AnomalyPath.Core.Localization;

namespace AnomalyPath.Core.Services
{
    /// <summary>Часть класса <see cref="CombatService"/>: атаки и урон.</summary>
    public sealed partial class CombatService
    {
        /// <inheritdoc />
        public bool TryAttack(CombatState state, ItemInstance weapon, out CombatAttackResult result)
        {
            result = default;
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (state.IsFinished || weapon == null || weapon.IsBroken)
            {
                return false;
            }

            var attacker = state.Current;
            var template = _items.Get(weapon.ItemId);
            if (template.Weapon == null)
            {
                return false;
            }

            if (!_skills.TryGet(template.Weapon.DefaultSkillId, out var skill) || skill == null)
            {
                return false;
            }

            if (attacker.Template.Type == ActorType.Enemy && weapon.MagazineRounds < skill.AmmoCost)
            {
                // У противника запас патронов бесконечный: нехватка заряжается полностью за ОД.
                if (attacker.Ap < _settings.ReloadApCost)
                {
                    return false;
                }

                weapon.Magazine = template.Weapon.Magazine;
                attacker.Ap -= _settings.ReloadApCost;
            }

            if (!TryUseSkill(state, weapon, skill.Id, out var outcome) || outcome.Hits.Count == 0)
            {
                return false;
            }

            result = outcome.Hits[0];
            return true;
        }

        /// <inheritdoc />
        public bool TryEnemyAttack(CombatState state, out CombatAttackResult result)
        {
            result = default;
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (state.IsFinished || !ReferenceEquals(state.Current, state.Enemy))
            {
                return false;
            }

            var attacker = state.Enemy;
            if (state.EnemyWeapon != null)
            {
                return TryAttack(state, state.EnemyWeapon, out result);
            }

            var skillId = attacker.Template.Combat?.DefaultSkillId;
            if (skillId == null)
            {
                return false;
            }

            if (!TryUseSkill(state, null, skillId, out var outcome) || outcome.Hits.Count == 0)
            {
                return false;
            }

            result = outcome.Hits[0];
            return true;
        }

        /// <inheritdoc />
        public bool TryFlee(CombatState state, out FleeResult result)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (state.IsFinished)
            {
                result = FleeResult.Finished();
                return false;
            }

            if (!ReferenceEquals(state.Current, state.Player))
            {
                result = FleeResult.NotPlayerTurn();
                return false;
            }

            EndTurn(state);
            var attacked = TryEnemyAttack(state, out var attack);
            var attackResult = attacked ? attack : (CombatAttackResult?)null;
            result = state.Player.IsDead
                ? FleeResult.DiedTo(attackResult)
                : FleeResult.EscapedWith(ToHealth(state.Player.Health), attackResult);
            return true;
        }

        private static int ToHealth(double health) =>
            health <= 0 ? 0 : (int)Math.Round(health, MidpointRounding.AwayFromZero);

        private int EvaluateDamage(
            Combatant defender,
            string damageType,
            double baseDamage,
            double penetration,
            string sourceId)
        {
            if (!_damageTypes.Contains(damageType))
            {
                throw new DataException(sourceId, CoreErrors.UnknownDamageType, damageType);
            }

            var variables = new Dictionary<string, double>(StringComparer.Ordinal)
            {
                ["damage"] = baseDamage,
                ["penetration"] = penetration,
                ["protection"] = ProtectionOf(defender, damageType)
            };

            var formula = _formulas.Get(_settings.DamageFormulaId);
            var evaluated = formula.Evaluate(variables, _balance.Constants, _settings.ClampMin, _settings.ClampMax);
            return evaluated < 0 ? 0 : (int)evaluated;
        }
    }
}
