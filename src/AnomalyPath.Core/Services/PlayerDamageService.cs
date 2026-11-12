#nullable enable
using System;
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Core.Combat;
using AnomalyPath.Core.Player;
using AnomalyPath.Core.Save;

namespace AnomalyPath.Core.Services
{
    /// <summary>
    /// Урон вне боя: защита считается как база актёра плюс снаряжение, урон уменьшается на неё,
    /// а нулевое здоровье запускает обычную смерть — потерю вещей, долю опыта и возврат
    /// в последний хаб с урезанным здоровьем, кроме режима постоянной смерти.
    /// </summary>
    public sealed class PlayerDamageService : IPlayerDamageService
    {
        private readonly IDeathService _death;
        private readonly IGameRulesRepository _rules;
        private readonly IActorRepository _actors;
        private readonly IEquipmentService _equipment;
        private readonly ITalentService _talents;
        private readonly IArtifactService _artifacts;

        /// <summary>Создаёт сервис урона вне боя.</summary>
        /// <param name="death">Последствия смерти для вещей и опыта.</param>
        /// <param name="rules">Правила прохождения: актёр игрока и доля респавна.</param>
        /// <param name="actors">Актёры: база здоровья и защиты.</param>
        /// <param name="equipment">Снаряжение: защита надетых вещей.</param>
        /// <param name="talents">Таланты: прибавка к максимуму здоровья.</param>
        /// <param name="artifacts">Артефакты: прибавка к здоровью и защите.</param>
        /// <exception cref="ArgumentNullException">Обязательная зависимость не задана.</exception>
        public PlayerDamageService(
            IDeathService death,
            IGameRulesRepository rules,
            IActorRepository actors,
            IEquipmentService equipment,
            ITalentService talents,
            IArtifactService artifacts)
        {
            _death = death ?? throw new ArgumentNullException(nameof(death));
            _rules = rules ?? throw new ArgumentNullException(nameof(rules));
            _actors = actors ?? throw new ArgumentNullException(nameof(actors));
            _equipment = equipment ?? throw new ArgumentNullException(nameof(equipment));
            _talents = talents ?? throw new ArgumentNullException(nameof(talents));
            _artifacts = artifacts ?? throw new ArgumentNullException(nameof(artifacts));
        }

        /// <inheritdoc />
        public int MaxHealth(GameState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            var player = state.Player;
            if (player == null)
            {
                return 0;
            }

            var actor = PlayerActor();
            var health = (actor.Combat?.Health ?? 0) +
                         _talents.GetModifiers(player).HealthMax +
                         player.LevelHealthBonus +
                         _artifacts.GetEquippedModifiers(player.Equipment).HealthMax;
            return Math.Max(0, health);
        }

        /// <inheritdoc />
        public int Protection(GameState state, string damageTypeId)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (string.IsNullOrWhiteSpace(damageTypeId))
            {
                return 0;
            }

            var player = state.Player;
            if (player == null)
            {
                return 0;
            }

            var actor = PlayerActor();
            var baseValue = 0;
            if (actor.Combat != null && actor.Combat.Protection.TryGetValue(damageTypeId, out var value))
            {
                baseValue = value;
            }

            var equipment = _equipment.GetProtection(player.Equipment, damageTypeId);
            return Math.Max(0, baseValue + equipment);
        }

        /// <inheritdoc />
        public PlayerDamageResult Apply(GameState state, int damage, string damageTypeId)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            var player = state.Player;
            if (player == null)
            {
                return new PlayerDamageResult(0, 0, 0, 0, 0, false);
            }

            if (player.Health <= 0)
            {
                return new PlayerDamageResult(0, 0, 0, 0, 0, true);
            }

            var raw = Math.Max(0, damage);
            var protection = Protection(state, damageTypeId);
            var passed = Math.Max(0, raw - protection);
            return ApplyDamage(state, raw, protection, passed);
        }

        /// <inheritdoc />
        public PlayerDamageResult ApplyDirect(GameState state, int damage)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            var player = state.Player;
            if (player == null)
            {
                return new PlayerDamageResult(0, 0, 0, 0, 0, false);
            }

            if (player.Health <= 0)
            {
                return new PlayerDamageResult(0, 0, 0, 0, 0, true);
            }

            var raw = Math.Max(0, damage);
            return ApplyDamage(state, raw, 0, raw);
        }

        private PlayerDamageResult ApplyDamage(GameState state, int raw, int protection, int passed)
        {
            var player = state.Player!;
            var before = player.Health;
            var after = Math.Max(0, before - passed);
            if (after > 0)
            {
                player.SetHealth(after);
                return new PlayerDamageResult(raw, protection, passed, before, after, false);
            }

            // Максимум считаем до смерти: она снимает предметы и меняет бонусы здоровья.
            var maxHealth = MaxHealth(state);
            var death = _death.Apply(state);
            if (death.Permadeath)
            {
                player.SetHealth(0);
                return new PlayerDamageResult(raw, protection, passed, before, 0, true, true, death);
            }

            var rules = _rules.Require();
            var respawn = Math.Max(
                1,
                (int)Math.Round(maxHealth * rules.RespawnPercent, MidpointRounding.AwayFromZero));
            player.SetHealth(respawn);

            var foodCap = Math.Max(
                0,
                (int)Math.Round(player.Needs.FoodMax * rules.RespawnPercent, MidpointRounding.AwayFromZero));
            var waterCap = Math.Max(
                0,
                (int)Math.Round(player.Needs.WaterMax * rules.RespawnPercent, MidpointRounding.AwayFromZero));
            player.Needs.SetHunger(Math.Min(player.Needs.Hunger, foodCap));
            player.Needs.SetThirst(Math.Min(player.Needs.Thirst, waterCap));

            var hubId = state.World.LastHubId;
            state.World.MoveTo(hubId);
            return new PlayerDamageResult(raw, protection, passed, before, player.Health, true, false, death, hubId);
        }

        private ActorTemplate PlayerActor() => _actors.Get(_rules.Require().PlayerActorId);
    }
}
