#nullable enable
using System;
using System.Collections.Generic;
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Core.Combat;

namespace AnomalyPath.Core.Services
{
    /// <summary>
    /// ИИ противника: ограниченный перебор планов на бюджет ОД по ожиданиям боевого сервиса.
    /// Аптечка стоит перед перебором: план с добиванием важнее, иначе при здоровье ниже
    /// порога <see cref="CombatSettingsTemplate.MedkitHpThreshold"/> ИИ лечится.
    /// Решение детерминировано и не трогает источник случайности: одинаковое состояние даёт один план.
    /// </summary>
    public sealed class CombatAi : ICombatAi
    {
        private readonly ICombatService _combat;
        private readonly IItemRepository _items;
        private readonly CombatSettingsTemplate _settings;

        /// <summary>Создаёт ИИ противника.</summary>
        /// <param name="combat">Боевой сервис: предпросмотр и исполнение действий.</param>
        /// <param name="items">Репозиторий предметов: аптечка противника.</param>
        /// <param name="settings">Настройки боя: глубина плана и цены.</param>
        public CombatAi(ICombatService combat, IItemRepository items, CombatSettingsTemplate settings)
        {
            _combat = combat ?? throw new ArgumentNullException(nameof(combat));
            _items = items ?? throw new ArgumentNullException(nameof(items));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        /// <inheritdoc />
        public IReadOnlyList<EnemyTurnStep> Plan(CombatState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (state.IsFinished || !ReferenceEquals(state.Current, state.Enemy))
            {
                return Array.Empty<EnemyTurnStep>();
            }

            var best = new BestPlan();
            var plan = new List<EnemyTurnStep>();
            Search(
                state.Clone(),
                plan,
                0,
                state.Player.Health,
                state.Enemy.Ap,
                best);

            // Добивание важнее лечения: если план убивает игрока, аптечка не выбирается.
            if (best.Killed)
            {
                return best.Steps;
            }

            var medkit = MedkitStep(state);
            return medkit.HasValue
                ? new[] { medkit.Value }
                : best.Steps;
        }

        /// <inheritdoc />
        public bool TryExecuteNext(CombatState state, out EnemyTurnAction action)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            action = default!;
            var plan = Plan(state);
            if (plan.Count == 0)
            {
                return false;
            }

            return Execute(state, plan[0], out action);
        }

        /// <inheritdoc />
        public EnemyTurnResult TakeTurn(CombatState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            var actions = new List<EnemyTurnAction>();
            while (TryExecuteNext(state, out var action))
            {
                actions.Add(action);
                if (state.IsFinished)
                {
                    break;
                }
            }

            return actions.Count == 0
                ? EnemyTurnResult.Empty
                : new EnemyTurnResult(actions, state.Player.IsDead);
        }

        private bool Execute(CombatState state, EnemyTurnStep step, out EnemyTurnAction action)
        {
            switch (step.Kind)
            {
                case EnemyTurnActionKind.Reload:
                    action = new EnemyTurnAction(step, 0, false, false);
                    return _combat.TryEnemyReload(state);
                case EnemyTurnActionKind.Skill:
                    if (step.SkillId == null ||
                        !_combat.TryUseSkill(state, state.EnemyWeapon, step.SkillId, out var result))
                    {
                        action = new EnemyTurnAction(step, 0, false, false);
                        return false;
                    }

                    action = new EnemyTurnAction(
                        step,
                        result.TotalDamage,
                        result.Killed,
                        AnyHit(result));
                    return true;
                case EnemyTurnActionKind.Medkit:
                    var healed = _combat.TryEnemyUseMedkit(state);
                    if (!healed.Succeeded)
                    {
                        action = new EnemyTurnAction(step, 0, false, false);
                        return false;
                    }

                    action = new EnemyTurnAction(step, 0, false, false, healed.HealthRestored);
                    return true;
                default:
                    action = new EnemyTurnAction(step, 0, false, false);
                    return false;
            }
        }

        private static bool AnyHit(CombatSkillResult result)
        {
            for (var i = 0; i < result.Hits.Count; i++)
            {
                if (result.Hits[i].Hit)
                {
                    return true;
                }
            }

            return false;
        }

        private void Search(
            CombatState simulation,
            List<EnemyTurnStep> plan,
            int depth,
            double startHealth,
            int startAp,
            BestPlan best)
        {
            var damage = startHealth - simulation.Player.Health;
            var apSpent = startAp - simulation.Enemy.Ap;
            if (IsBetter(damage, apSpent, plan.Count, simulation.Player.IsDead, best))
            {
                best.Steps.Clear();
                best.Steps.AddRange(plan);
                best.Damage = damage;
                best.Ap = apSpent;
                best.Killed = simulation.Player.IsDead;
            }

            if (depth >= _settings.AiPlanDepth || simulation.IsFinished)
            {
                return;
            }

            foreach (var step in Candidates(simulation))
            {
                var next = simulation.Clone();
                if (!Apply(next, step))
                {
                    continue;
                }

                plan.Add(step);
                Search(
                    next,
                    plan,
                    depth + (step.ApCost > 0 ? 1 : 0),
                    startHealth,
                    startAp,
                    best);
                plan.RemoveAt(plan.Count - 1);
            }
        }

        private List<EnemyTurnStep> Candidates(CombatState simulation)
        {
            var steps = new List<EnemyTurnStep>();
            foreach (var action in _combat.GetActions(simulation, simulation.EnemyWeapon))
            {
                if (action.Kind != CombatActionKind.Skill || action.SkillId == null || !action.IsAvailable)
                {
                    continue;
                }

                var preview = _combat.PreviewSkill(simulation, simulation.EnemyWeapon, action.SkillId);
                if (!preview.Available)
                {
                    continue;
                }

                steps.Add(new EnemyTurnStep(
                    EnemyTurnActionKind.Skill,
                    action.SkillId,
                    preview.ApCost,
                    preview.AmmoCost,
                    preview.HitChance,
                    preview.ExpectedDamage,
                    preview.KillChance));
            }

            if (_combat.CanEnemyReload(simulation))
            {
                steps.Add(new EnemyTurnStep(
                    EnemyTurnActionKind.Reload,
                    null,
                    _settings.ReloadApCost,
                    0,
                    0,
                    0,
                    0));
            }

            return steps;
        }

        /// <summary>
        /// Шаг аптечки: выбирается, только когда здоровье ниже порога, заряд есть,
        /// медицина боевая и ОД хватает; на полном здоровье лечить нечего.
        /// </summary>
        private EnemyTurnStep? MedkitStep(CombatState state)
        {
            var enemy = state.Enemy;
            if (state.EnemyMedkitCharges <= 0 || enemy.MaxHealth <= 0)
            {
                return null;
            }

            var medkitItemId = enemy.Template.Combat?.MedkitItemId;
            if (medkitItemId == null || !_items.TryGet(medkitItemId, out var template))
            {
                return null;
            }

            var block = template.Consumable;
            if (block == null || !block.UsableInCombat || block.TotalHeal() <= 0)
            {
                return null;
            }

            if (enemy.Health >= enemy.MaxHealth || enemy.Ap < block.UseTime)
            {
                return null;
            }

            var healthRatio = enemy.Health / enemy.MaxHealth;
            if (healthRatio >= _settings.MedkitHpThreshold)
            {
                return null;
            }

            return new EnemyTurnStep(EnemyTurnActionKind.Medkit, null, block.UseTime, 0, 0, 0, 0);
        }

        /// <summary>
        /// Применяет шаг к копии боя при переборе планов. Аптечки здесь нет:
        /// она выбирается правилом перед перебором, а не как вариант плана.
        /// </summary>
        private bool Apply(CombatState simulation, EnemyTurnStep step)
        {
            switch (step.Kind)
            {
                case EnemyTurnActionKind.Reload:
                    return _combat.TryEnemyReload(simulation);
                case EnemyTurnActionKind.Skill:
                    simulation.Enemy.Ap -= step.ApCost;
                    if (simulation.EnemyWeapon != null)
                    {
                        simulation.EnemyWeapon.Magazine =
                            Math.Max(0, simulation.EnemyWeapon.MagazineRounds - step.AmmoCost);
                    }

                    simulation.Player.Health = Math.Max(0, simulation.Player.Health - step.ExpectedDamage);
                    return true;
                default:
                    return false;
            }
        }

        private static bool IsBetter(
            double damage,
            int apSpent,
            int steps,
            bool killed,
            BestPlan best)
        {
            if (killed != best.Killed)
            {
                return killed;
            }

            if (killed)
            {
                if (apSpent != best.Ap)
                {
                    return apSpent < best.Ap;
                }

                return steps < best.Steps.Count;
            }

            if (damage > best.Damage + 1e-9)
            {
                return true;
            }

            if (Math.Abs(damage - best.Damage) > 1e-9)
            {
                return false;
            }

            if (apSpent != best.Ap)
            {
                return apSpent < best.Ap;
            }

            return steps < best.Steps.Count;
        }

        private sealed class BestPlan
        {
            public List<EnemyTurnStep> Steps { get; } = new List<EnemyTurnStep>();

            public double Damage { get; set; }

            public int Ap { get; set; }

            public bool Killed { get; set; }
        }
    }
}
