#nullable enable
using System;
using System.Collections.Generic;
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Core.Anomalies;
using AnomalyPath.Core.Data;
using AnomalyPath.Core.Inventory;
using AnomalyPath.Core.Loot;
using AnomalyPath.Core.Minigames;
using AnomalyPath.Core.Services;
using AnomalyPath.Core.Save;
using AnomalyPath.Core.Session;
using AnomalyPath.Core.Spawns;
using AnomalyPath.Core.World;

namespace AnomalyPath.Tests
{
    /// <summary>Мини-игры: код замка, радар, их сервисы и сериализация состояния.</summary>
    public sealed class MinigameTests : DataTestBase
    {
        /// <summary>Создаёт категорию тестов мини-игр.</summary>
        /// <param name="dataRoot">Корень данных проекта.</param>
        public MinigameTests(string dataRoot)
            : base(dataRoot)
        {
        }

        /// <inheritdoc />
        public override void Run()
        {
            var services = BuildServices();
            var rules = services.Resolve<IGameRulesRepository>();
            var generator = services.Resolve<IWorldGenerator>();
            var damage = services.Resolve<IPlayerDamageService>();
            var loot = services.Resolve<ILootService>();

            Check("замок: код детерминирован seed-ом и без повторов", () =>
            {
                var first = new LockPuzzle(12345, 3, 5);
                var second = new LockPuzzle(12345, 3, 5);
                Equal(first.CodeText(), second.CodeText(), "один seed — один код");
                True(first.CodeText() != new LockPuzzle(54321, 3, 5).CodeText(), "другой seed");
                var code = first.CodeText();
                Equal(3, first.CodeLength, "три цифры");
                True(code[0] != code[1] && code[1] != code[2] && code[0] != code[2], "без повторов");
                foreach (var symbol in code)
                {
                    True(
                        symbol >= '1' && symbol <= (char)('0' + first.AlphabetSize),
                        "цифра в диапазоне 1..5");
                }
            });

            Check("замок: подсказки на месте, не там и отсутствует", () =>
            {
                var puzzle = new LockPuzzle(7, 3, 5);
                var code = puzzle.CodeText();
                var solved = puzzle.Evaluate(code);
                True(solved != null && solved.Solved, "код угадан");
                Equal(3, solved!.Exact, "все на месте");
                Equal(0, solved.Partial, "не там нет");
                True(
                    solved.Marks[0] == LockDigitMark.CorrectPlace &&
                    solved.Marks[1] == LockDigitMark.CorrectPlace &&
                    solved.Marks[2] == LockDigitMark.CorrectPlace,
                    "все цифры зелёные");

                var swapped = code[1] + code.Substring(0, 1) + code.Substring(2);
                var partial = puzzle.Evaluate(swapped);
                True(partial != null, "вариант принят");
                Equal(1, partial!.Exact, "одна на месте");
                Equal(2, partial.Partial, "две не там: " + code + " -> " + swapped);
                Equal(LockDigitMark.WrongPlace, partial.Marks[0], "первая жёлтая");
                Equal(LockDigitMark.WrongPlace, partial.Marks[1], "вторая жёлтая");
                Equal(LockDigitMark.CorrectPlace, partial.Marks[2], "третья зелёная");

                var absent = '0';
                for (var digit = 1; digit <= puzzle.AlphabetSize; digit++)
                {
                    var symbol = (char)('0' + digit);
                    if (code.IndexOf(symbol) < 0)
                    {
                        absent = symbol;
                        break;
                    }
                }

                True(absent != '0', "в алфавите 1..5 есть цифра вне кода");
                var withAbsent = puzzle.Evaluate(absent.ToString() + code[0] + code[1]);
                True(withAbsent != null, "вариант с чужой цифрой принят");
                Equal(LockDigitMark.None, withAbsent!.Marks[0], "чужой цифры нет в коде");
                True(withAbsent.Exact < 3, "код не угадан");
            });

            Check("замок: неполный, чужой или повторяющийся код не принимается", () =>
            {
                var puzzle = new LockPuzzle(7, 3, 5);
                True(puzzle.Evaluate("12") == null, "короткий");
                True(puzzle.Evaluate("126") == null, "чужой символ");
                True(puzzle.Evaluate("122") == null, "повтор");
                True(puzzle.Evaluate(null) == null, "пустой");
                True(LockPuzzle.IsValidGuess("123", 3, 5), "допустимый");
                True(!LockPuzzle.IsValidGuess("122", 3, 5), "повтор недопустим");
                True(LockPuzzle.IsValidGuess("345", 3, 5), "цифра 5 при алфавите 5 допустима");
            });

            Check("радар: сигнал по расстоянию и отсутствие артефактов", () =>
            {
                var field = new RadarField(5, 5, new[] { 12 }, new[] { 1, 2, 3 });
                Equal(4, field.Signal(12), "в клетке артефакта");
                Equal(4, field.Signal(13), "соседняя клетка");
                Equal(3, field.Signal(14), "два шага");
                Equal(2, field.Signal(15), "три шага");
                Equal(1, field.Signal(0), "далеко");
                Equal(0, new RadarField(5, 5, Array.Empty<int>(), new[] { 1, 2, 3 }).Signal(0), "пусто");
            });

            Check("радар: поле 3×2 считает клетки построчно", () =>
            {
                var field = new RadarField(3, 2, new[] { 0 }, new[] { 1, 2, 3 });
                Equal(6, field.CellCount, "шесть клеток");
                Equal(4, field.Signal(1), "сосед справа");
                Equal(4, field.Signal(3), "сосед снизу");
                Equal(3, field.Signal(4), "диагональ");
                Equal(2, field.Signal(5), "дальний угол");
            });

            Check("радар: раскладка без повторов", () =>
            {
                var cells = RadarField.Place(99, 3, 2, 4);
                Equal(4, cells.Count, "четыре клетки");
                var unique = new HashSet<int>(cells);
                Equal(4, unique.Count, "без повторов");
                var inField = true;
                foreach (var cell in cells)
                {
                    inField &= cell >= 0 && cell < 6;
                }

                True(inField, "клетки в поле 3×2");
            });

            Check("настройки мини-игр читаются из данных", () =>
            {
                True(rules.Rules != null, "правила");
                True(rules.Rules!.StashLock != null, "взлом");
                Equal(7, rules.Rules.StashLock!.Attempts, "семь попыток");
                Equal(3, rules.Rules.StashLock.ForTier(1).CodeLength, "код из 3 цифр на тире 1");
                Equal(5, rules.Rules.StashLock.ForTier(1).AlphabetSize, "цифры 1..5");
                True(rules.Rules.AnomalyField != null, "радар");
                Equal(3, rules.Rules.AnomalyField!.Width, "поле 3 клетки в ширину");
                Equal(2, rules.Rules.AnomalyField.Height, "поле 2 клетки в высоту");
                Equal(5, rules.Rules.AnomalyField.ForTier(1).Scans, "пять сканов");
            });

            Check("замок тайника: неверные попытки и потеря тайника", () =>
            {
                var world = WorldState.Start(generator.Generate(42));
                var location = world.CurrentLocation;
                location.MarkStashRolled();
                location.MarkStashPresent();
                var stashes = new StashService(
                    services.Resolve<ILootRepository>(),
                    loot,
                    rules);

                Equal(StashOpenStatus.Locked, stashes.Open(world, location).Status, "без кода закрыт");
                var state = stashes.LockState(world, location);
                True(state != null, "состояние");
                var puzzle = new LockPuzzle(
                    StableHash.Combine(location.Seed, "stash_lock:" + location.InstanceId),
                    state!.CodeLength,
                    state.AlphabetSize);
                var wrong = puzzle.CodeText() == "123" ? "124" : "123";
                for (var attempt = 0; attempt < 7; attempt++)
                {
                    state = stashes.Guess(world, location, wrong);
                }

                True(state!.Failed, "попытки кончились");
                Equal(StashOpenStatus.LockFailed, stashes.Open(world, location).Status, "потерян");
            });

            Check("замок тайника: верный код открывает", () =>
            {
                var world = WorldState.Start(generator.Generate(42));
                var location = world.CurrentLocation;
                location.MarkStashRolled();
                location.MarkStashPresent();
                var stashes = new StashService(
                    services.Resolve<ILootRepository>(),
                    loot,
                    rules);
                var state = stashes.LockState(world, location)!;
                var puzzle = new LockPuzzle(
                    StableHash.Combine(location.Seed, "stash_lock:" + location.InstanceId),
                    state.CodeLength,
                    state.AlphabetSize);
                var solved = stashes.Guess(world, location, puzzle.CodeText());

                True(solved!.Solved, "код подошёл");
                Equal(StashOpenStatus.Opened, stashes.Open(world, location).Status, "тайник вскрыт");
            });

            Check("радар: сканы бесплатны и ограничены", () =>
            {
                var session = NewSession(generator, damage);
                var anomaly = AddAnomaly(session, damage, 2, 1);
                var service = new AnomalyService(
                    new FakeAnomalies(Template(2, 1)),
                    loot,
                    damage,
                    rules);
                var state = service.OpenRadar(session, anomaly)!;
                Equal(2, anomaly.ArtifactCells.Count, "два артефакта");
                var first = service.Scan(session, anomaly, 0);
                Equal(AnomalySearchStatus.Success, first.Status, "скан");
                Equal(
                    state.ScansTotal - 1,
                    first.ScansLeft,
                    "один скан потрачен: total=" + state.ScansTotal + ", left=" + first.ScansLeft);
                var again = service.Scan(session, anomaly, 0);
                Equal(first.ScansLeft, again.ScansLeft, "повторный скан бесплатен");

                for (var cell = 1; cell < state.ScansTotal; cell++)
                {
                    service.Scan(session, anomaly, cell);
                }

                Equal(AnomalySearchStatus.NoScans, service.Scan(session, anomaly, 5).Status, "сканы кончились");
            });

            Check("радар: обыск тратит попытку и отдаёт артефакт", () =>
            {
                var session = NewSession(generator, damage);
                var anomaly = AddAnomaly(session, damage, 2, 1);
                var service = new AnomalyService(
                    new FakeAnomalies(Template(2, 1)),
                    loot,
                    damage,
                    rules);
                service.OpenRadar(session, anomaly);
                var cell = anomaly.ArtifactCells[0];

                var result = service.SearchCell(session, anomaly, cell);

                True(result.Found, "найден");
                Equal(1, anomaly.Loot.Count, "лежит в аномалии");
                Equal(1, result.AttemptsLeft, "попытка потрачена");
                Equal(
                    AnomalySearchStatus.AlreadySearched,
                    service.SearchCell(session, anomaly, cell).Status,
                    "повторный обыск");
            });

            Check("радар: смерть от урона не отдаёт артефакт", () =>
            {
                var session = NewSession(generator, damage, health: 1);
                var anomaly = AddAnomaly(session, damage, 1, 1);
                var service = new AnomalyService(
                    new FakeAnomalies(Template(1, 1)),
                    loot,
                    damage,
                    rules);
                service.OpenRadar(session, anomaly);

                var result = service.SearchCell(session, anomaly, anomaly.ArtifactCells[0]);

                True(result.Died, "смерть");
                False(result.Found, "находки нет");
                Equal(0, anomaly.Loot.Count, "лута нет");
            });

            Check("сейв: состояние замка и радара переживает запись", () =>
            {
                var locationData = new AnomalyPath.Core.Save.LocationSaveData(
                    "loc_1",
                    "tpl",
                    42,
                    "bg",
                    LocationSubtype.Hub,
                    1,
                    0.5,
                    true,
                    false,
                    false,
                    true,
                    true,
                    false,
                    false,
                    null,
                    0,
                    0,
                    2,
                    false,
                    false,
                    new[] { "123", "234" });
                var locationJson = locationData.ToJson();
                var locationBack = AnomalyPath.Core.Save.LocationSaveData.FromJson(locationJson, "test");
                Equal(2, locationBack.StashLockAttempts, "попытки замка");
                Equal(2, locationBack.StashLockGuesses.Count, "история кодов");
                Equal("234", locationBack.StashLockGuesses[1], "последний код");

                var objectData = new AnomalyPath.Core.Save.LocationObjectSaveData(
                    "spawn_test",
                    false,
                    null,
                    1,
                    true,
                    new[] { 3, 7 },
                    new[] { 0, 1 },
                    new[] { 3 },
                    new[] { 7 });
                var objectJson = objectData.ToJson();
                var objectBack = AnomalyPath.Core.Save.LocationObjectSaveData.FromJson(objectJson, "test");
                Equal(2, objectBack.ArtifactCells.Count, "клетки артефактов");
                Equal(1, objectBack.ScannedCells.Count, "сканы");
                Equal(1, objectBack.SearchedCells.Count, "обыски");
            });
        }

        private static GameSession NewSession(
            IWorldGenerator generator,
            IPlayerDamageService damage,
            int health = 100)
        {
            var world = WorldState.Start(generator.Generate(42));
            var player = new AnomalyPath.Core.Player.PlayerState(
                "player",
                1,
                0,
                health,
                8,
                new GridContainer("player", 8, 4),
                new Equipment("player", 4),
                Array.Empty<string>(),
                new AnomalyPath.Core.Player.NeedsState(100, 100, 100));
            return new GameSession(new GameState(world, player));
        }

        private static AnomalyLocationObject AddAnomaly(
            GameSession session,
            IPlayerDamageService damage,
            int attempts,
            double chance)
        {
            var anomaly = new AnomalyLocationObject("spawn_test", "spawn.spawn_test.title", "anomaly_test");
            session.State.World.CurrentLocation.AddObject(anomaly);
            return anomaly;
        }

        private static AnomalyTemplate Template(int attempts, double chance) =>
            new AnomalyTemplate(
                "anomaly_test",
                "fire",
                "dmg_fire",
                5,
                attempts,
                chance,
                "loot_artifact_fire",
                new[] { 1 });

        private sealed class FakeAnomalies : IAnomalyRepository
        {
            private readonly AnomalyTemplate _template;

            public FakeAnomalies(AnomalyTemplate template)
            {
                _template = template;
            }

            public IReadOnlyList<AnomalyTemplate> All => new[] { _template };

            public bool TryGet(string id, out AnomalyTemplate? template)
            {
                template = string.Equals(id, _template.Id, StringComparison.Ordinal) ? _template : null;
                return template != null;
            }

            public AnomalyTemplate Get(string id) =>
                TryGet(id, out var template) ? template! : throw new KeyNotFoundException(id);
        }
    }
}
