#nullable enable
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Core.Artifacts;
using AnomalyPath.Core.Character;
using AnomalyPath.Core.Combat;
using AnomalyPath.Core.Data;
using AnomalyPath.Core.Death;
using AnomalyPath.Core.Di;
using AnomalyPath.Core.Equipment;
using AnomalyPath.Core.Inventory;
using AnomalyPath.Core.Localization;
using AnomalyPath.Core.Nicks;
using AnomalyPath.Core.Progression;
using AnomalyPath.Core.Repositories;
using AnomalyPath.Core.Services;
using AnomalyPath.Core.Validation;

namespace AnomalyPath.Core
{
    /// <summary>
    /// Сборка приложения: проверка данных, репозитории, сервисы, контейнер.
    /// UI и MonoBehaviour сюда не входят: всё это чистый C#.
    /// </summary>
    public static partial class Bootstrap
    {
        /// <summary>Каталог текстов по умолчанию, относительно корня данных.</summary>
        public const string DefaultTextCatalog = "Localization/errors.ru.json";

        /// <summary>Каталог текстов игры на основном языке (русский).</summary>
        public const string DefaultGameTextCatalog = "Localization/strings.ru.json";

        /// <summary>Каталог текстов игры на втором языке: откат, если ключа нет в основном.</summary>
        public const string FallbackGameTextCatalog = "Localization/strings.en.json";

        /// <summary>Собирает контейнер сервисов из папки данных.</summary>
        /// <param name="dataRoot">Корень данных (в редакторе — <c>Assets/Data</c>).</param>
        /// <returns>Контейнер с зарегистрированными репозиториями и сервисами.</returns>
        /// <exception cref="ValidationException">Данные не прошли проверку.</exception>
        public static ServiceContainer Build(string dataRoot)
        {
            var result = Preflight.Run(dataRoot);
            if (!result.CanStart)
            {
                throw new ValidationException(result.Report);
            }

            return result.Services!;
        }

        /// <summary>Строит репозитории и сервисы из уже загруженных данных — без повторного чтения файлов.</summary>
        /// <param name="data">Результат загрузки.</param>
        /// <param name="texts">Тексты игры; <c>null</c> — пустой каталог, ключи возвращаются как есть.</param>
        /// <returns>Контейнер с зарегистрированными репозиториями и сервисами.</returns>
        /// <exception cref="DataException">Сущность не соответствует схеме.</exception>
        public static ServiceContainer BuildServices(LoadedData data, IGameTexts? texts = null)
        {
            var items = ItemRepository.FromLoadedData(data);
            var artifactSettings = LoadArtifactSettings(data);
            var artifacts = new ArtifactService(items, artifactSettings);
            var damageTypes = DamageTypeRepository.FromLoadedData(data);
            var skills = SkillRepository.FromLoadedData(data);
            var formulas = FormulaRepository.FromLoadedData(data);
            var actors = ActorRepository.FromLoadedData(data);
            var rarities = RarityRepository.FromLoadedData(data);
            var injuries = InjuryRepository.FromLoadedData(data);
            var locations = LocationRepository.FromLoadedData(data);
            var topology = TopologyRepository.FromLoadedData(data);
            var loadouts = LoadoutRepository.FromLoadedData(data);
            var combatSettings = LoadCombatSettings(data);
            var balance = LoadBalance(data);
            var instanceIds = new SequentialInstanceIdGenerator();
            var equipmentSettings = LoadEquipmentSettings(data);
            var rules = GameRulesRepository.FromLoadedData(data);
            var nicknames = LoadNicknames(data);
            var economySettings = LoadEconomy(data);
            var quests = QuestRepository.FromLoadedData(data);
            var storyQuests = StoryQuestRepository.FromLoadedData(data);
            var dialogs = DialogRepository.FromLoadedData(data);
            var questChains = QuestChainRepository.FromLoadedData(data);
            var talents = TalentRepository.FromLoadedData(data);
            var loot = LootRepository.FromLoadedData(data);
            var progression = LoadProgression(data);
            var spawns = SpawnRepository.FromLoadedData(data);

            var inventory = new InventoryService(items, instanceIds, rules.Rules?.OverloadKg ?? 0);
            var travel = new TravelService();
            var world = new WorldGenerator(locations, topology, questChains, spawns);

            var container = new ServiceContainer();
            container.RegisterSingleton<IItemRepository>(items);
            container.RegisterSingleton<IArtifactService>(artifacts);
            container.RegisterSingleton<IGameTexts>(texts ?? GameTexts.Empty);
            container.RegisterSingleton<IItemCatalogService>(new ItemCatalogService(items));
            container.RegisterSingleton<IDamageTypeRepository>(damageTypes);
            container.RegisterSingleton<ISkillRepository>(skills);
            container.RegisterSingleton<IFormulaRepository>(formulas);
            container.RegisterSingleton<IActorRepository>(actors);
            container.RegisterSingleton<IRarityRepository>(rarities);
            container.RegisterSingleton<IInjuryRepository>(injuries);
            container.RegisterSingleton<ILocationRepository>(locations);
            container.RegisterSingleton<ITopologyRepository>(topology);
            container.RegisterSingleton<ILoadoutRepository>(loadouts);
            container.RegisterSingleton<IQuestRepository>(quests);
            container.RegisterSingleton<IStoryQuestRepository>(storyQuests);
            container.RegisterSingleton<IDialogRepository>(dialogs);
            container.RegisterSingleton<IQuestChainRepository>(questChains);
            container.RegisterSingleton<ITalentRepository>(talents);
            container.RegisterSingleton<ILootRepository>(loot);
            var lootService = new LootService(loot, items, instanceIds, artifacts, rarities);
            container.RegisterSingleton<ILootService>(lootService);
            container.RegisterSingleton<IStashService>(new StashService(loot, lootService, rules));
            container.RegisterSingleton<ISpawnRepository>(spawns);
            var anomalies = AnomalyRepository.FromLoadedData(data);
            container.RegisterSingleton<IAnomalyRepository>(anomalies);
            container.RegisterSingleton<ILocationObjectService>(
                new LocationObjectService(spawns, locations, lootService, anomalies));
            var storyWorld = new StoryWorldService(questChains, locations, spawns, topology);
            container.RegisterSingleton<IStoryWorldService>(storyWorld);
            var talentService = new TalentService(talents);
            container.RegisterSingleton<ITalentService>(talentService);
            var progressionService = new ProgressionService(progression);
            container.RegisterSingleton<IProgressionService>(progressionService);
            container.RegisterSingleton<IWorldGenerator>(world);
            container.RegisterSingleton<ITravelService>(travel);
            container.RegisterSingleton<IInstanceIdGenerator>(instanceIds);
            container.RegisterSingleton<IInventoryService>(inventory);
            container.RegisterSingleton<IEquipmentSettings>(equipmentSettings);
            container.RegisterSingleton<ICombatSettings>(new CombatSettings(combatSettings));
            container.RegisterSingleton<IGameRulesRepository>(rules);
            container.RegisterSingleton<INicknameGenerator>(nicknames);
            container.RegisterSingleton<IEconomySettings>(economySettings);
            var deathRandom = new SeededDeathRandom(1);
            container.RegisterSingleton<IDeathRandom>(deathRandom);
            var deathService = new DeathService(items, deathRandom, progressionService);
            container.RegisterSingleton<IDeathService>(deathService);
            container.RegisterSingleton<ICombatOutcomeService>(new CombatOutcomeService(
                deathService,
                rules,
                formulas,
                combatSettings,
                balance,
                lootService,
                progressionService));
            var equipmentService = new EquipmentService(items, inventory, artifacts);
            container.RegisterSingleton<IEquipmentService>(equipmentService);
            var playerDamage = new PlayerDamageService(
                deathService,
                rules,
                actors,
                equipmentService,
                talentService,
                artifacts);
            container.RegisterSingleton<IPlayerDamageService>(playerDamage);
            container.RegisterSingleton<INeedsService>(new NeedsService(rules, playerDamage, inventory));
            container.RegisterSingleton<IAnomalyService>(
                new AnomalyService(anomalies, lootService, playerDamage, rules));
            container.RegisterSingleton<ICharacterActions>(
                new CharacterActions(
                    items,
                    inventory,
                    equipmentService,
                    rules.Rules?.BaseCarryWeight ?? double.PositiveInfinity));
            container.RegisterSingleton<IConsumableService>(new ConsumableService(items));
            var wear = new ArmorWearService(items);
            container.RegisterSingleton<IArmorWearService>(wear);
            container.RegisterSingleton<ICombatRandom>(new SeededCombatRandom(1));
            var combatService = new CombatService(items, damageTypes, equipmentService, wear, formulas, combatSettings, balance, new SeededCombatRandom(1), inventory, skills, rarities, injuries);
            container.RegisterSingleton<ICombatService>(combatService);
            container.RegisterSingleton<ICombatAi>(new CombatAi(combatService, items, combatSettings));
            var loadoutPreviews = new LoadoutPreviewService(
                rules,
                loadouts,
                items,
                inventory,
                equipmentService,
                equipmentSettings,
                instanceIds);
            container.RegisterSingleton<ILoadoutPreviewService>(loadoutPreviews);
            container.RegisterSingleton<INewGameService>(new NewGameService(
                rules,
                world,
                actors,
                loadoutPreviews,
                nicknames));
            var tradeService = new TradeService(
                actors,
                locations,
                items,
                inventory,
                economySettings,
                rules,
                rarities);
            container.RegisterSingleton<ITradeService>(tradeService);
            container.RegisterSingleton<IRepairService>(
                new RepairService(actors, items, tradeService, economySettings));
            container.RegisterSingleton<IQuestService>(new QuestService(
                quests,
                actors,
                locations,
                topology,
                items,
                rules));
            var storyQuestService = new StoryQuestService(
                storyQuests,
                items,
                inventory,
                questChains,
                storyWorld);
            container.RegisterSingleton<IStoryQuestService>(storyQuestService);
            container.RegisterSingleton<IDialogService>(
                new DialogService(dialogs, storyQuestService, items, inventory));
            return container;
        }

        private static CombatSettingsTemplate LoadCombatSettings(LoadedData data)
        {
            var records = data.All("combat");
            if (records.Count == 0)
            {
                throw new DataException("combat", CoreErrors.FileNotFound);
            }

            return CombatSettingsTemplate.FromJson(records[0], "combat");
        }

        private static BalanceTemplate LoadBalance(LoadedData data)
        {
            var records = data.All("balance");
            return records.Count == 0 ? BalanceTemplate.Empty : BalanceTemplate.FromJson(records[0]);
        }

        private static IEquipmentSettings LoadEquipmentSettings(LoadedData data)
        {
            var records = data.All("equipment");
            if (records.Count == 0)
            {
                throw new DataException("equipment", CoreErrors.FileNotFound);
            }

            return EquipmentSettings.FromJson(records[0], "equipment");
        }

        private static INicknameGenerator LoadNicknames(LoadedData data)
        {
            var records = data.All("nicks");
            if (records.Count == 0)
            {
                return new NicknameGenerator(System.Array.Empty<string>());
            }

            var template = NicknameCatalogTemplate.FromJson(records[0], "nicks");
            return new NicknameGenerator(template.Names);
        }

        private static IEconomySettings LoadEconomy(LoadedData data)
        {
            var records = data.All("economy");
            return records.Count == 0
                ? Rules.EconomySettings.Default
                : Rules.EconomySettings.FromJson(records[0], "economy");
        }

        private static ProgressionSettings LoadProgression(LoadedData data)
        {
            var records = data.All(ProgressionSettings.RecordId);
            if (records.Count == 0)
            {
                throw new DataException(ProgressionSettings.RecordId, CoreErrors.ProgressionMissing);
            }

            return ProgressionSettings.FromJson(records[0], ProgressionSettings.RecordId);
        }

        private static ArtifactSettings LoadArtifactSettings(LoadedData data)
        {
            var records = data.All(ArtifactSettings.RecordId);
            return records.Count == 0
                ? ArtifactSettings.Default
                : ArtifactSettings.FromJson(records[0], ArtifactSettings.RecordId);
        }
    }
}
