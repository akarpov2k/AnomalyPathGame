#nullable enable
using System;
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Core.Player;
using AnomalyPath.Core.Rules;
using AnomalyPath.Core.Save;

namespace AnomalyPath.Core.Services
{
    /// <summary>
    /// Потребности игрока: расход еды и воды за часы мира с накоплением дробного остатка,
    /// штрафы голода и урон жажды. Числа берутся из блока <c>needs</c> правил.
    /// </summary>
    public sealed class NeedsService : INeedsService
    {
        private readonly IGameRulesRepository _rules;
        private readonly IPlayerDamageService _damage;
        private readonly IInventoryService? _inventory;

        /// <summary>Создаёт сервис потребностей.</summary>
        /// <param name="rules">Правила прохождения: ставки расхода и штрафы.</param>
        /// <param name="damage">Урон вне боя: потеря здоровья от жажды и смерть.</param>
        /// <param name="inventory">Инвентарь: нужен, чтобы считать перегруз; <c>null</c> — без проверки.</param>
        /// <exception cref="ArgumentNullException">Зависимость не задана.</exception>
        public NeedsService(IGameRulesRepository rules, IPlayerDamageService damage, IInventoryService? inventory = null)
        {
            _rules = rules ?? throw new ArgumentNullException(nameof(rules));
            _damage = damage ?? throw new ArgumentNullException(nameof(damage));
            _inventory = inventory;
        }

        /// <inheritdoc />
        public NeedsTickResult Consume(GameState state, double hours)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            var player = state.Player;
            if (player == null)
            {
                return new NeedsTickResult(0, 0, 0, 0, 0, 0, 0);
            }

            if (hours <= 0 || double.IsNaN(hours) || double.IsInfinity(hours))
            {
                return new NeedsTickResult(
                    0,
                    0,
                    0,
                    player.Needs.Hunger,
                    player.Needs.Thirst,
                    player.Needs.FoodCarry,
                    player.Needs.WaterCarry);
            }

            var overloaded = IsOverloaded(player);
            var rateHours = overloaded ? hours * 2 : hours;
            var settings = Settings();
            var food = player.Needs.FoodCarry + (rateHours * settings.FoodPerHour);
            var water = player.Needs.WaterCarry + (rateHours * settings.WaterPerHour);
            var foodSpent = (int)Math.Floor(food);
            var waterSpent = (int)Math.Floor(water);
            var hungerBefore = player.Needs.Hunger;
            var thirstBefore = player.Needs.Thirst;

            player.Needs.SetHunger(hungerBefore - foodSpent);
            player.Needs.SetThirst(thirstBefore - waterSpent);
            player.Needs.SetFoodCarry(food - foodSpent);
            player.Needs.SetWaterCarry(water - waterSpent);
            return new NeedsTickResult(
                hours,
                hungerBefore - player.Needs.Hunger,
                thirstBefore - player.Needs.Thirst,
                player.Needs.Hunger,
                player.Needs.Thirst,
                player.Needs.FoodCarry,
                player.Needs.WaterCarry);
        }

        /// <inheritdoc />
        public int TravelStaminaCost(GameState state, int baseCost)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            return IsHungry(state) ? Settings().HungryStaminaCost(baseCost) : baseCost;
        }

        /// <inheritdoc />
        public int ApCostBonus(GameState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            return IsHungry(state) ? Settings().HungryApCostBonus : 0;
        }

        /// <inheritdoc />
        public int TurnDamage(GameState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            return IsThirsty(state) ? Settings().ThirstDamage : 0;
        }

        /// <inheritdoc />
        public PlayerDamageResult ApplyThirstDamage(GameState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            var player = state.Player;
            var damage = TurnDamage(state);
            if (player == null || damage <= 0)
            {
                var health = player?.Health ?? 0;
                return new PlayerDamageResult(0, 0, 0, health, health, false);
            }

            return _damage.ApplyDirect(state, damage);
        }

        /// <summary><c>true</c>, если сытость на нуле.</summary>
        /// <param name="state">Состояние прохождения.</param>
        /// <returns><c>true</c>, если еды не осталось.</returns>
        private static bool IsHungry(GameState state) => state.Player != null && state.Player.Needs.Hunger <= 0;

        /// <summary><c>true</c>, если питьё на нуле.</summary>
        /// <param name="state">Состояние прохождения.</param>
        /// <returns><c>true</c>, если воды не осталось.</returns>
        private static bool IsThirsty(GameState state) => state.Player != null && state.Player.Needs.Thirst <= 0;

        /// <summary>Перегруз: разрешённый запас веса уже израсходован, еда и вода тратятся вдвое.</summary>
        private bool IsOverloaded(PlayerState player) =>
            _inventory != null &&
            !double.IsPositiveInfinity(player.Inventory.WeightLimit) &&
            _inventory.TotalWeight(player.Inventory) > player.Inventory.WeightLimit + 1e-9;

        private NeedsSettings Settings() => _rules.Require().Needs;
    }
}
