#nullable enable
using System;
using System.Collections.Generic;
using AnomalyPath.Core;
using AnomalyPath.Core.Data;
using AnomalyPath.Core.Localization;

namespace AnomalyPath.Tests
{
    /// <summary>Запуск всех категорий тестов на папке данных.</summary>
    public static class TestSuite
    {
        /// <summary>Прогоняет набор и пишет результат в консоль.</summary>
        /// <param name="dataRoot">Корень данных.</param>
        /// <returns>Счётчики и список упавших проверок.</returns>
        public static TestRunResult Run(string dataRoot) => Run(dataRoot, Console.WriteLine);

        /// <summary>Прогоняет набор и пишет результат в переданный лог.</summary>
        /// <param name="dataRoot">Корень данных.</param>
        /// <param name="log">Куда писать строки результата.</param>
        /// <returns>Счётчики и список упавших проверок.</returns>
        public static TestRunResult Run(string dataRoot, Action<string> log)
        {
            TestBase.DataRootOverride = dataRoot;
            var source = new FileSystemDataSource(dataRoot);
            CoreText.Configure(JsonStringCatalog.FromSource(source, Bootstrap.DefaultTextCatalog));

            var suites = new TestBase[]
            {
                new DataLoadingTests(dataRoot),
                new RepositoryTests(dataRoot),
                new RulesTests(dataRoot),
                new SkyTests(dataRoot),
                new LoadoutTests(dataRoot),
                new NicknameTests(),
                new InventoryTests(),
                new ItemFactsTests(dataRoot),
                new EquipmentTests(dataRoot),
                new ArtifactTests(dataRoot),
                new CharacterActionsTests(),
                new FormulaTests(dataRoot),
                new ProgressionTests(dataRoot),
                new LocalizationTests(source),
                new DamageTypeTests(dataRoot),
                new CombatTests(dataRoot),
                new CombatSkillTests(dataRoot),
                new CombatOutcomeTests(dataRoot),
                new FleeTests(dataRoot),
                new RarityTests(dataRoot),
                new WeaponStatsTests(dataRoot),
                new InjuryTests(dataRoot),
                new HealingTests(dataRoot),
                new FoodTests(dataRoot),
                new EnemyAiTests(dataRoot),
                new TraderTests(dataRoot),
                new RepairTests(dataRoot),
                new QuestTests(dataRoot),
                new StoryQuestTests(dataRoot),
                new DialogTests(dataRoot),
                new TalentTests(dataRoot),
                new LootTests(),
                new LootRarityTests(dataRoot),
                new SpawnTests(),
                new LocationObjectTests(),
                new AnomalyTests(dataRoot),
                new MinigameTests(dataRoot),
                new StashTests(dataRoot),
                new JackpotTests(),
                new DeathTests(dataRoot),
                new WorldTests(dataRoot),
                new StageTests(),
                new HubTravelTests(dataRoot),
                new RestTests(dataRoot),
                new NeedsTests(dataRoot),
                new SessionTests(dataRoot),
                new SaveTests(dataRoot),
                new JsonFormatTests(),
                new FileFormatTests(),
                new DataPackTests(dataRoot),
                new GestureTests(),
                new WindowTests(),
                new ValidationErrorTests(dataRoot),
                new PreflightTests(dataRoot),
                new SourceHygieneTests(dataRoot),
            };

            var passed = 0;
            var failed = 0;
            var failures = new List<string>();

            foreach (var suite in suites)
            {
                suite.Log = log;
                suite.Run();
                passed += suite.Passed;
                failed += suite.Failed;
                failures.AddRange(suite.Failures);
            }

            log($"Итого: {passed} прошло, {failed} упало");
            return new TestRunResult(passed, failed, failures);
        }
    }
}
