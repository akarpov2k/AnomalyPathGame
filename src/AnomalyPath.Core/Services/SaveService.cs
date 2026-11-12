#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Core.Data;
using AnomalyPath.Core.Json;
using AnomalyPath.Core.Save;
using AnomalyPath.Core.Session;

namespace AnomalyPath.Core.Services
{
    /// <summary>Сохранение мира и игрока в JSON-снимок и восстановление из него.</summary>
    public sealed class SaveService : ISaveService
    {
        private readonly ISaveStorage _storage;
        private readonly IItemRepository _items;
        private readonly IGameRulesRepository? _rules;
        private readonly ISpawnRepository? _spawns;
        private readonly IEquipmentService? _equipment;

        /// <summary>Создаёт сервис поверх хранилища.</summary>
        /// <param name="storage">Хранилище слотов.</param>
        /// <param name="items">Репозиторий предметов для восстановления инвентаря.</param>
        /// <param name="rules">
        /// Правила прохождения: значение потери опыта для сейвов, записанных до появления
        /// настройки; <c>null</c> — без подстановки, такие сейвы читаются с нулём.
        /// </param>
        /// <param name="spawns">Записи спавна для восстановления объекта локации; <c>null</c> — без объектов.</param>
        /// <param name="equipment">
        /// Экипировка для пересчёта лимита веса после загрузки; <c>null</c> — лимит не задаётся.
        /// </param>
        /// <exception cref="ArgumentNullException">Хранилище или предметы не заданы.</exception>
        public SaveService(
            ISaveStorage storage,
            IItemRepository items,
            IGameRulesRepository? rules = null,
            ISpawnRepository? spawns = null,
            IEquipmentService? equipment = null)
        {
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
            _items = items ?? throw new ArgumentNullException(nameof(items));
            _rules = rules;
            _spawns = spawns;
            _equipment = equipment;
        }

        /// <inheritdoc />
        public void Save(
            GameState state,
            string slot,
            double playtime = 0,
            DateTimeOffset? createdAt = null,
            string? title = null)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            var now = createdAt ?? DateTimeOffset.UtcNow;
            var worldData = WorldSaveMapper.ToSaveData(state);
            var game = new SaveGame(
                SaveGame.CurrentSchemaVersion,
                SaveGame.CurrentSaveVersion,
                now.ToString("O", CultureInfo.InvariantCulture),
                playtime,
                state.World.Map.Seed,
                worldData,
                state.Player == null ? null : PlayerSaveMapper.ToSaveData(state.Player),
                title);
            _storage.Write(slot, JsonWriter.Write(game.ToJson()));
        }

        /// <inheritdoc />
        public GameState Load(string slot)
        {
            var text = _storage.Read(slot);
            var game = SaveGame.FromJson(JsonParser.Parse(text, slot), slot);
            var world = WorldSaveMapper.LoadWorld(game.World, game.Seed, slot, _spawns);
            var player = game.Player == null
                ? null
                : PlayerSaveMapper.FromSaveData(game.Player, _items, slot);
            var loadRules = _rules?.Rules;
            if (player != null && loadRules != null)
            {
                // Сетка растёт под содержимое: при загрузке она только добирает базовую высоту,
                // если сейв её меньше, и не сжимается — иначе вещи нижних рядов ушли бы в переполнение.
                if (player.Inventory.Rows < loadRules.BaseRows)
                {
                    player.Inventory.Resize(loadRules.BaseRows);
                }

                player.Inventory.AllowGrowth(Math.Max(loadRules.MaxRows, player.Inventory.Rows));

                if (_equipment != null)
                {
                    player.Inventory.SetWeightLimit(
                        _equipment.CarryCapacity(player.Equipment, loadRules.BaseCarryWeight));
                }
            }

            var settings = game.World.Settings == null
                ? null
                : new NewGameSettings(
                    game.World.Settings.InsuranceChance,
                    game.World.Settings.TradeDifficulty,
                    permadeath: game.World.Settings.Permadeath,
                    xpLossChance: game.World.Settings.XpLossChance ?? _rules?.Rules?.XpLossChance ?? 0,
                    nightAccuracyPenalty: game.World.Settings.NightAccuracyPenalty);
            return new GameState(
                world,
                player,
                settings,
                WorldSaveMapper.LoadTraders(game.World.Traders),
                WorldSaveMapper.LoadQuests(game.World.Quests),
                game.World.QuestSerial,
                WorldSaveMapper.LoadQuestBoards(game.World.QuestBoards),
                WorldSaveMapper.LoadRaid(game.World.Raid));
        }

        /// <inheritdoc />
        public bool Exists(string slot) => _storage.Exists(slot);

        /// <inheritdoc />
        public IReadOnlyList<SaveSlotInfo> ListSlots()
        {
            var result = new List<SaveSlotInfo>();
            foreach (var slot in _storage.List())
            {
                try
                {
                    var text = _storage.Read(slot);
                    var game = SaveGame.FromJson(JsonParser.Parse(text, slot), slot);
                    result.Add(new SaveSlotInfo(
                        slot,
                        game.CreatedAt,
                        game.Playtime,
                        game.Seed,
                        game.World.CurrentLocationId,
                        game.World.Day,
                        game.World.Hours,
                        true,
                        game.Title,
                        game.Player?.Level ?? 0,
                        game.Player?.Money ?? 0));
                }
                catch (DataException)
                {
                    result.Add(new SaveSlotInfo(slot, null, 0, 0, null, 0, 0, false));
                }
            }

            return result;
        }

        /// <inheritdoc />
        public void Delete(string slot) => _storage.Delete(slot);
    }
}
