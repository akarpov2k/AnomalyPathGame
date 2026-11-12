#nullable enable
using System;
using System.Collections.Generic;
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Core.Data;
using AnomalyPath.Core.World;

namespace AnomalyPath.Core.Save
{
    /// <summary>Преобразование состояния мира в снимок сохранения и обратно.</summary>
    public static partial class WorldSaveMapper
    {
        /// <summary>Собирает снимок мира, торговцев, квестов и настроек.</summary>
        /// <param name="state">Состояние прохождения.</param>
        /// <returns>Снимок мира для файла сохранения.</returns>
        /// <exception cref="ArgumentNullException">Состояние не задано.</exception>
        public static WorldSaveData ToSaveData(GameState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            var world = state.World;
            return new WorldSaveData(
                world.Map.StartLocationId,
                world.CurrentLocationId,
                world.Stamina,
                world.StaminaMax,
                world.Map.StartingStamina,
                world.Clock.Day,
                world.Clock.Hours,
                world.Clock.DayStartHour,
                world.Clock.NightStartHour,
                ToLocationData(world.Map),
                ToEdgeData(world.Map),
                state.Settings == null
                    ? null
                    : new NewGameSettingsSaveData(
                        state.Settings.InsuranceChance,
                        state.Settings.TradeDifficulty,
                        state.Settings.Permadeath,
                        state.Settings.XpLossChance,
                        state.Settings.NightAccuracyPenalty),
                ToTraderData(state.Traders),
                ToQuestData(state.Quests),
                state.QuestSerial,
                world.LastHubId,
                world.LootSerial,
                world.StashChecks,
                world.StashOpenings,
                world.HourCarry,
                world.RouteEdges,
                world.RouteTailId,
                world.RouteLastBranches,
                world.RouteFrontIds,
                world.Map.RouteSerial,
                ToQuestBoardData(state.QuestBoards),
                new RaidSaveData(state.Raid.Active, state.Raid.ClearedLocationIds));
        }

        /// <summary>Восстанавливает состояние мира из снимка.</summary>
        /// <param name="data">Снимок мира.</param>
        /// <param name="seed">Seed мира из файла сохранения.</param>
        /// <param name="sourcePath">Путь сохранения для сообщения об ошибке.</param>
        /// <param name="spawns">Записи спавна для объектов локаций; <c>null</c> — без объектов.</param>
        /// <returns>Состояние мира.</returns>
        /// <exception cref="ArgumentNullException">Снимок не задан.</exception>
        /// <exception cref="DataException">Неизвестный код погоды.</exception>
        public static WorldState LoadWorld(
            WorldSaveData data,
            int seed,
            string sourcePath,
            ISpawnRepository? spawns)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            var locations = new List<LocationInstance>();
            foreach (var item in data.Locations)
            {
                var location = new LocationInstance(
                    item.InstanceId,
                    item.TemplateId,
                    item.Seed,
                    item.Background,
                    item.Subtype,
                    item.Tier,
                    item.Danger,
                    item.RouteStep,
                    item.RouteEdge);
                if (item.Visited)
                {
                    location.MarkVisited();
                }

                if (item.Cleared)
                {
                    location.MarkCleared();
                }

                if (item.EnemyGone)
                {
                    location.MarkEnemyGone();
                }

                if (item.StashRolled)
                {
                    location.MarkStashRolled();
                }

                if (item.StashPresent)
                {
                    location.MarkStashPresent();
                }

                if (item.StashOpened)
                {
                    location.MarkStashOpened();
                }

                if (item.StashLockAttempts > 0 || item.StashLockSolved || item.StashLockFailed ||
                    item.StashLockGuesses.Count > 0)
                {
                    location.RestoreStashLock(
                        item.StashLockAttempts,
                        item.StashLockSolved,
                        item.StashLockFailed,
                        item.StashLockGuesses);
                }

                if (item.ObjectsSpawned)
                {
                    location.MarkSpawned();
                    foreach (var savedObject in item.Objects)
                    {
                        var restored = RestoreObject(savedObject, spawns);
                        if (restored != null)
                        {
                            location.AddObject(restored);
                        }
                    }
                }

                locations.Add(location);
            }

            var edges = new List<WorldEdge>();
            foreach (var item in data.Edges)
            {
                edges.Add(new WorldEdge(
                    item.EdgeId,
                    item.FromId,
                    item.ToId,
                    item.Via,
                    item.Length,
                    new TravelCost(item.Stamina, item.Hours),
                    item.OneWay,
                    item.Hidden));
            }

            var map = new WorldMap(
                seed,
                locations,
                edges,
                data.StartLocationId,
                data.StartingStamina,
                data.DayStartHour,
                data.NightStartHour,
                data.RouteEdges,
                data.RouteTailId,
                data.RouteLastBranches,
                data.RouteFront,
                data.RouteSerial);
            var clock = new WorldClock(data.Day, data.Hours, data.DayStartHour, data.NightStartHour);
            return new WorldState(
                map,
                data.CurrentLocationId,
                data.Stamina,
                data.StaminaMax,
                clock,
                data.LastHubId,
                data.LootSerial,
                data.StashChecks,
                data.StashOpenings,
                data.HourCarry);
        }
    }
}
