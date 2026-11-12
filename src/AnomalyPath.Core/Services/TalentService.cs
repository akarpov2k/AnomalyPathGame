#nullable enable
using System;
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Core.Data;
using AnomalyPath.Core.Localization;
using AnomalyPath.Core.Player;
using AnomalyPath.Core.Talents;

namespace AnomalyPath.Core.Services
{
    /// <summary>
    /// Прокачка через дерево талантов: очки, ранги, пороги открытия и собранные бонусы.
    /// Очки выдаются за уровни персонажа снаружи, сервис их только тратит и начисляет.
    /// </summary>
    public sealed class TalentService : ITalentService
    {
        private readonly ITalentRepository _repository;

        /// <summary>Создаёт сервис прокачки.</summary>
        /// <param name="repository">Репозиторий дерева талантов.</param>
        /// <exception cref="ArgumentNullException">Репозиторий не задан.</exception>
        public TalentService(ITalentRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        /// <inheritdoc />
        public TalentTreeTemplate Tree => _repository.Tree;

        /// <inheritdoc />
        public int SpentPoints(PlayerState player)
        {
            if (player == null)
            {
                throw new ArgumentNullException(nameof(player));
            }

            var total = 0;
            foreach (var node in Tree.Nodes)
            {
                total += RankOf(player, node.Id);
            }

            return total;
        }

        /// <inheritdoc />
        public int AvailablePoints(PlayerState player)
        {
            if (player == null)
            {
                throw new ArgumentNullException(nameof(player));
            }

            return player.TalentPoints;
        }

        /// <inheritdoc />
        public int RankOf(PlayerState player, string talentId)
        {
            if (player == null)
            {
                throw new ArgumentNullException(nameof(player));
            }

            if (string.IsNullOrWhiteSpace(talentId) || !Tree.TryGetNode(talentId, out var node) || node == null)
            {
                return 0;
            }

            return Math.Clamp(player.GetTalentRank(talentId), 0, node.MaxRank);
        }

        /// <inheritdoc />
        public bool IsUnlocked(PlayerState player, TalentNodeTemplate node)
        {
            if (player == null)
            {
                throw new ArgumentNullException(nameof(player));
            }

            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            return SpentPoints(player) >= node.RequiredPoints;
        }

        /// <inheritdoc />
        public TalentResult Learn(PlayerState player, string talentId)
        {
            if (player == null)
            {
                throw new ArgumentNullException(nameof(player));
            }

            if (!Tree.TryGetNode(talentId, out var node) || node == null)
            {
                return TalentResult.Error(TalentResultStatus.UnknownTalent, talentId);
            }

            var rank = RankOf(player, talentId);
            var spent = SpentPoints(player);
            var available = player.TalentPoints;
            if (rank >= node.MaxRank)
            {
                return TalentResult.Error(
                    TalentResultStatus.MaxRank,
                    talentId,
                    rank,
                    spent,
                    available,
                    node.RequiredPoints);
            }

            if (spent < node.RequiredPoints)
            {
                return TalentResult.Error(
                    TalentResultStatus.Locked,
                    talentId,
                    rank,
                    spent,
                    available,
                    node.RequiredPoints);
            }

            if (available <= 0 || !player.TrySpendTalentPoint())
            {
                return TalentResult.Error(
                    TalentResultStatus.NoPoints,
                    talentId,
                    rank,
                    spent,
                    available,
                    node.RequiredPoints);
            }

            player.SetTalentRank(talentId, rank + 1);
            ApplyNeedsMax(player, node);
            return TalentResult.Ok(
                talentId,
                rank + 1,
                SpentPoints(player),
                player.TalentPoints,
                node.RequiredPoints);
        }

        /// <inheritdoc />
        public TalentModifiers GetModifiers(PlayerState player)
        {
            if (player == null)
            {
                throw new ArgumentNullException(nameof(player));
            }

            var apMax = 0;
            var healthMax = 0;
            var accuracy = 0.0;
            var staminaMax = 0;
            var staminaRecovery = 0;
            var maxFood = 0;
            var maxWater = 0;
            foreach (var node in Tree.Nodes)
            {
                var rank = RankOf(player, node.Id);
                if (rank == 0)
                {
                    continue;
                }

                foreach (var effect in node.Effects)
                {
                    var value = effect.PerRank * rank;
                    switch (effect.Stat)
                    {
                        case TalentStat.ApMax:
                            apMax += ToInt(value);
                            break;
                        case TalentStat.HealthMax:
                            healthMax += ToInt(value);
                            break;
                        case TalentStat.Accuracy:
                            accuracy += value;
                            break;
                        case TalentStat.StaminaMax:
                            staminaMax += ToInt(value);
                            break;
                        case TalentStat.StaminaRecovery:
                            staminaRecovery += ToInt(value);
                            break;
                        case TalentStat.MaxFood:
                            maxFood += ToInt(value);
                            break;
                        case TalentStat.MaxWater:
                            maxWater += ToInt(value);
                            break;
                        default:
                            throw new DataException(node.Id, CoreErrors.UnknownTalentStat, effect.Stat);
                    }
                }
            }

            return new TalentModifiers(apMax, healthMax, accuracy, staminaMax, staminaRecovery, maxFood, maxWater);
        }

        /// <inheritdoc />
        public void GrantPoints(PlayerState player, int amount)
        {
            if (player == null)
            {
                throw new ArgumentNullException(nameof(player));
            }

            player.AddTalentPoints(amount);
        }

        private static int ToInt(double value) => (int)Math.Round(value);

        /// <summary>
        /// Повышает максимум еды и воды при изучении таланта: прибавка за новый ранг
        /// прибавляется к текущему максимуму шкалы, значения шкал не меняются.
        /// </summary>
        /// <param name="player">Игрок.</param>
        /// <param name="node">Изученный талант.</param>
        private static void ApplyNeedsMax(PlayerState player, TalentNodeTemplate node)
        {
            var food = 0;
            var water = 0;
            foreach (var effect in node.Effects)
            {
                if (effect.Stat == TalentStat.MaxFood)
                {
                    food += ToInt(effect.PerRank);
                }
                else if (effect.Stat == TalentStat.MaxWater)
                {
                    water += ToInt(effect.PerRank);
                }
            }

            if (food == 0 && water == 0)
            {
                return;
            }

            player.Needs.SetMax(
                player.Needs.FoodMax + food,
                player.Needs.WaterMax + water);
        }
    }
}
