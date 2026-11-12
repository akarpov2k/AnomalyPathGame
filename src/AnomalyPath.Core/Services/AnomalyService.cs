#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Core.Anomalies;
using AnomalyPath.Core.Data;
using AnomalyPath.Core.Inventory;
using AnomalyPath.Core.Minigames;
using AnomalyPath.Core.Session;
using AnomalyPath.Core.Spawns;
using AnomalyPath.Core.World;

namespace AnomalyPath.Core.Services
{
    /// <summary>
    /// Радар аномалии: содержимое раскладывается по клеткам предварительными бросками
    /// на каждую попытку, скан показывает силу сигнала, обыск клетки наносит урон и
    /// отдаёт артефакт при совпадении. Броски детерминированы seed-ом локации, id объекта
    /// и номером попытки.
    /// </summary>
    public sealed class AnomalyService : IAnomalyService
    {
        private readonly IAnomalyRepository _anomalies;
        private readonly ILootService _loot;
        private readonly IPlayerDamageService _damage;
        private readonly IGameRulesRepository? _rules;

        /// <summary>Создаёт сервис радара аномалий.</summary>
        /// <param name="anomalies">Аномалии: урон, попытки, шанс и пул артефактов.</param>
        /// <param name="loot">Бросок артефактов из пула аномалии.</param>
        /// <param name="damage">Урон игроку вне боя и последствия смерти.</param>
        /// <param name="rules">Правила прохождения: размеры поля и число сканов; <c>null</c> — без радара.</param>
        /// <exception cref="ArgumentNullException">Зависимость не задана.</exception>
        public AnomalyService(
            IAnomalyRepository anomalies,
            ILootService loot,
            IPlayerDamageService damage,
            IGameRulesRepository? rules = null)
        {
            _anomalies = anomalies ?? throw new ArgumentNullException(nameof(anomalies));
            _loot = loot ?? throw new ArgumentNullException(nameof(loot));
            _damage = damage ?? throw new ArgumentNullException(nameof(damage));
            _rules = rules;
        }

        /// <inheritdoc />
        public RadarState? OpenRadar(GameSession session, AnomalyLocationObject anomaly)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            if (anomaly == null)
            {
                throw new ArgumentNullException(nameof(anomaly));
            }

            var location = session.State.World.CurrentLocation;
            if (!BelongsTo(location, anomaly) ||
                !_anomalies.TryGet(anomaly.AnomalyId, out var template) ||
                template == null)
            {
                return null;
            }

            var field = _rules?.Rules?.AnomalyField;
            if (field == null)
            {
                return null;
            }

            var tier = field.ForTier(location.Tier);
            if (!anomaly.RadarPlaced)
            {
                var successes = new List<int>();
                for (var attempt = 0; attempt < template.Attempts; attempt++)
                {
                    var seed = Seed(location, anomaly, attempt);
                    if (new SeededWorldRandom(seed).NextDouble() < template.ArtifactChance)
                    {
                        successes.Add(attempt);
                    }
                }

                var placementSeed = StableHash.Combine(
                    location.Seed,
                    "anomaly_radar:" + anomaly.SpawnId);
                var cells = RadarField.Place(placementSeed, field.Width, field.Height, successes.Count);
                anomaly.PlaceRadar(cells, successes);
            }

            return new RadarState(
                field.Width,
                field.Height,
                tier.Scans,
                template.Attempts,
                anomaly.AttemptsUsed,
                anomaly.ScannedCells,
                anomaly.SearchedCells,
                tier.Bands);
        }

        /// <inheritdoc />
        public RadarScanResult Scan(GameSession session, AnomalyLocationObject anomaly, int cell)
        {
            var state = OpenRadar(session, anomaly);
            if (state == null)
            {
                return RadarScanResult.Failed(AnomalySearchStatus.UnknownAnomaly);
            }

            if (cell < 0 || cell >= state.CellCount)
            {
                return RadarScanResult.Failed(AnomalySearchStatus.WrongCell);
            }

            if (anomaly.IsScanned(cell))
            {
                return RadarScanResult.Success(Field(state, anomaly).Signal(cell), state.ScansLeft);
            }

            var scansLeft = state.ScansLeft;
            if (scansLeft <= 0)
            {
                return RadarScanResult.Failed(AnomalySearchStatus.NoScans);
            }

            anomaly.MarkScanned(cell);
            var signal = Field(state, anomaly).Signal(cell);
            return RadarScanResult.Success(signal, scansLeft - 1);
        }

        /// <inheritdoc />
        public AnomalySearchResult SearchCell(
            GameSession session,
            AnomalyLocationObject anomaly,
            int cell)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            if (anomaly == null)
            {
                throw new ArgumentNullException(nameof(anomaly));
            }

            if (session.InCombat)
            {
                return AnomalySearchResult.Failed(AnomalySearchStatus.InCombat);
            }

            if (session.State.Player == null)
            {
                return AnomalySearchResult.Failed(AnomalySearchStatus.NoPlayer);
            }

            var location = session.State.World.CurrentLocation;
            if (!BelongsTo(location, anomaly))
            {
                return AnomalySearchResult.Failed(AnomalySearchStatus.WrongLocation);
            }

            if (!_anomalies.TryGet(anomaly.AnomalyId, out var template) || template == null)
            {
                return AnomalySearchResult.Failed(AnomalySearchStatus.UnknownAnomaly);
            }

            var state = OpenRadar(session, anomaly);
            if (state == null)
            {
                return AnomalySearchResult.Failed(AnomalySearchStatus.UnknownAnomaly);
            }

            if (cell < 0 || cell >= state.CellCount)
            {
                return AnomalySearchResult.Failed(AnomalySearchStatus.WrongCell);
            }

            if (anomaly.IsSearched(cell))
            {
                return AnomalySearchResult.Failed(AnomalySearchStatus.AlreadySearched);
            }

            if (!anomaly.CanSearch(template.Attempts))
            {
                return AnomalySearchResult.Failed(AnomalySearchStatus.NoAttempts);
            }

            anomaly.RegisterAttempt();
            var damage = _damage.Apply(session.State, template.Damage, template.DamageTypeId);
            var left = anomaly.RemainingAttempts(template.Attempts);
            anomaly.MarkSearched(cell);
            if (damage.Died)
            {
                return new AnomalySearchResult(AnomalySearchStatus.Success, damage, null, left);
            }

            if (!anomaly.TryTakeArtifact(cell, out var attempt))
            {
                return new AnomalySearchResult(AnomalySearchStatus.Success, damage, null, left);
            }

            var seed = Seed(location, anomaly, attempt);
            var rolled = _loot.RollTable(
                template.ArtifactTableId,
                location.Tier,
                StableHash.Combine(seed, "artifact"),
                ignoreTierBand: true);
            var found = new List<ItemInstance>(rolled);
            anomaly.AddLoot(found);
            return new AnomalySearchResult(AnomalySearchStatus.Success, damage, found, left);
        }

        /// <summary>Seed попытки: локация, объект и номер броска.</summary>
        private static int Seed(LocationInstance location, AnomalyLocationObject anomaly, int attempt) =>
            StableHash.Combine(
                location.Seed,
                "anomaly:" + anomaly.SpawnId + ":" + attempt.ToString(CultureInfo.InvariantCulture));

        private static RadarField Field(RadarState state, AnomalyLocationObject anomaly)
        {
            return new RadarField(state.Width, state.Height, anomaly.ArtifactCells, state.Bands);
        }

        private static bool BelongsTo(LocationInstance location, AnomalyLocationObject anomaly)
        {
            foreach (var item in location.Objects)
            {
                if (ReferenceEquals(item, anomaly))
                {
                    return true;
                }
            }

            return false;
        }
    }
}