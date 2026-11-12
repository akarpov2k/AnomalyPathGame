#nullable enable
using System.Collections.Generic;
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Core.Inventory;
using AnomalyPath.Core.Localization;
using AnomalyPath.Core.Save;
using AnomalyPath.Runtime.Screens.Character;
using AnomalyPath.Runtime.Screens.Combat;
using AnomalyPath.Runtime.Screens.Dialogs;
using AnomalyPath.Runtime.Screens.Game;
using AnomalyPath.Runtime.Screens.Game.Hub;
using AnomalyPath.Runtime.Screens.Menu;
using AnomalyPath.Runtime.Screens.Map;
using AnomalyPath.Runtime.Screens.Minigames;
using AnomalyPath.Runtime.Screens.Quests;
using AnomalyPath.Runtime.Screens.Rewards;
using AnomalyPath.Runtime.Screens.Talents;
using AnomalyPath.Runtime.Screens.Trader;
using AnomalyPath.Runtime.Settings;
using AnomalyPath.Runtime.Ui.Art;
using AnomalyPath.Runtime.Ui.Core;
using AnomalyPath.Runtime.Ui.Dialogs;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace AnomalyPath.Runtime
{
    /// <summary>Часть класса <see cref="GameScreen"/>: сборка канваса, экранов и первый показ.</summary>
    public sealed partial class GameScreen
    {

        private void Start()
        {
            _bootstrap = GetComponent<GameBootstrap>();
            Sprites.TryGet("buttons/empty", out var buttonNormal);
            Sprites.TryGet("buttons/green", out var buttonHighlighted);
            Ui = new UiFactory(UiFactory.FindFont(), buttonNormal, buttonHighlighted);
            EnsureEventSystem();
            EnsureCamera();
            var canvas = BuildCanvas();

            if (_bootstrap.Preflight == null || !_bootstrap.Preflight.CanStart)
            {
                BuildDataError(canvas);
                return;
            }

            var services = _bootstrap.Services!;
            Sessions = services.Resolve<IGameSessionService>();
            Saves = services.Resolve<ISaveService>();
            Loadouts = services.Resolve<ILoadoutRepository>();
            LoadoutPreviews = services.Resolve<ILoadoutPreviewService>();
            Items = services.Resolve<IItemRepository>();
            Rarities = services.Resolve<IRarityRepository>();
            Equipment = services.Resolve<IEquipmentService>();
            Locations = services.Resolve<ILocationRepository>();
            Anomalies = services.Resolve<IAnomalyRepository>();
            Injuries = services.Resolve<IInjuryRepository>();
            Artifacts = services.Resolve<IArtifactService>();
            Actions = services.Resolve<ICharacterActions>();
            Consumables = services.Resolve<IConsumableService>();
            Trade = services.Resolve<ITradeService>();
            Repair = services.Resolve<IRepairService>();
            Quests = services.Resolve<IQuestService>();
            StoryQuests = services.Resolve<IStoryQuestService>();
            Dialogs = services.Resolve<IDialogService>();
            Talents = services.Resolve<ITalentService>();
            Progression = services.Resolve<IProgressionService>();
            CombatSettings = services.Resolve<ICombatSettings>();
            Skills = services.Resolve<ISkillRepository>();
            CombatEngine = services.Resolve<ICombatService>();
            Actors = services.Resolve<IActorRepository>();
            Nicknames = services.Resolve<INicknameGenerator>();
            Rules = services.Resolve<IGameRulesRepository>();
            Topology = services.Resolve<ITopologyRepository>();
            Texts = services.Resolve<IGameTexts>();
            Spawns = services.Resolve<ISpawnRepository>();
            _inventory = services.Resolve<IInventoryService>();
            Loot = services.Resolve<ILootService>();
            Damage = services.Resolve<IPlayerDamageService>();
            DamageTypes = services.Resolve<IDamageTypeRepository>();
            EnsureAudio();

            var iconIds = new List<string>(Items.All.Count);
            foreach (var item in Items.All)
            {
                iconIds.Add(item.Icon);
            }

            var missingIcons = SpriteCatalogValidator.FindMissing(iconIds, Sprites);
            if (missingIcons.Count > 0)
            {
                var message = CoreText.Format(CoreErrors.SpriteMissing, string.Join(", ", missingIcons));
                Debug.LogError(message);
                BuildError(canvas, message);
                return;
            }

            Settings = new GameSettingsService(new PlayerPrefsSettingsStorage());
            Settings.Load();
            Settings.Apply();

            _restDialog = CreateDialog<RestDialog>(restDialogPrefab, canvas, "RestDialog");
            _restDialog.Bind(this);

            _mainMenu = CreateScreen<MainMenuScreen>(mainMenuScreenPrefab, canvas, "MainMenu");
            _newGame = CreateScreen<NewGameScreen>(newGameScreenPrefab, canvas, "NewGame");
            _load = CreateScreen<LoadGameScreen>(loadScreenPrefab, canvas, "LoadGame");
            _saveGame = CreateScreen<SaveGameScreen>(saveScreenPrefab, canvas, "SaveGame");
            _settingsScreen = CreateScreen<SettingsScreen>(settingsScreenPrefab, canvas, "Settings");
            _keyBindings = CreateScreen<KeyBindingsScreen>(keyBindingsScreenPrefab, canvas, "KeyBindings");
            _inventoryScreen = CreateScreen<InventoryScreen>(inventoryScreenPrefab, canvas, "Inventory");
            _bigHub = CreateScreen<BigHubScreen>(bigHubScreenPrefab, canvas, "BigHub");
            _hub = CreateScreen<HubScreen>(hubScreenPrefab, canvas, "Hub");
            _map = CreateScreen<MapScreen>(mapScreenPrefab, canvas, "Map");
            _authors = CreateScreen<AuthorsScreen>(authorsScreenPrefab, canvas, "Authors");
            _play = CreateScreen<PlayScreen>(playScreenPrefab, canvas, "InGame");
            _gameMenu = CreateScreen<GameMenuScreen>(gameMenuScreenPrefab, canvas, "GameMenu");
            _npcDialog = new NpcDialogOverlay(this, canvas, OnNpcDialogEnded);
            _questBoard = CreateScreen<QuestBoardScreen>(questBoardScreenPrefab, canvas, "QuestBoardScreen");
            _questJournal = CreateScreen<QuestJournalScreen>(questJournalScreenPrefab, canvas, "QuestJournalScreen");
            _talents = CreateScreen<TalentScreen>(talentScreenPrefab, canvas, "Talents");
            _trade = CreateScreen<TradeScreen>(tradeScreenPrefab, canvas, "Trade");
            _rewards = CreateScreen<RewardScreen>(rewardScreenPrefab, canvas, "Rewards");
            _combat = CreateScreen<CombatScreen>(combatScreenPrefab, canvas, "Combat");
            _lock = CreateScreen<LockMinigameScreen>(lockMinigameScreenPrefab, canvas, "LockMinigame");
            _radar = CreateScreen<RadarMinigameScreen>(radarMinigameScreenPrefab, canvas, "RadarMinigame");
            if (developmentStubScreenPrefab != null)
            {
                _developmentStub = CreateScreen<DevelopmentStubScreen>(
                    developmentStubScreenPrefab,
                    canvas,
                    "DevelopmentStub");
            }

            Router.Add(_mainMenu.Panel);
            Router.Add(_newGame.Panel);
            Router.Add(_load.Panel);
            Router.Add(_saveGame.Panel);
            Router.Add(_settingsScreen.Panel);
            Router.Add(_keyBindings.Panel);
            Router.Add(_authors.Panel);
            Router.Add(_play.Panel);
            Router.Add(_bigHub.Panel);
            Router.Add(_hub.Panel);
            Router.Add(_gameMenu.Panel);
            Router.Add(_map.Panel);
            Router.Add(_inventoryScreen.Panel);
            Router.Add(_questBoard.Panel);
            Router.Add(_questJournal.Panel);
            Router.Add(_talents.Panel);
            Router.Add(_trade.Panel);
            Router.Add(_rewards.Panel);
            Router.Add(_combat.Panel);
            Router.Add(_lock.Panel);
            Router.Add(_radar.Panel);
            if (_developmentStub != null)
            {
                Router.Add(_developmentStub.Panel);
            }

            _screens.Add(_mainMenu);
            _screens.Add(_newGame);
            _screens.Add(_load);
            _screens.Add(_saveGame);
            _screens.Add(_settingsScreen);
            _screens.Add(_keyBindings);
            _screens.Add(_authors);
            _screens.Add(_play);
            _screens.Add(_bigHub);
            _screens.Add(_hub);
            _screens.Add(_gameMenu);
            _screens.Add(_map);
            _screens.Add(_inventoryScreen);
            _screens.Add(_questBoard);
            _screens.Add(_questJournal);
            _screens.Add(_talents);
            _screens.Add(_trade);
            _screens.Add(_rewards);
            _screens.Add(_combat);
            _screens.Add(_lock);
            _screens.Add(_radar);
            if (_developmentStub != null)
            {
                _screens.Add(_developmentStub);
            }

            _exitDialog = CreateDialog<ExitDialog>(exitDialogPrefab, canvas, "ExitDialog");
            _exitDialog.Bind(this);
            ShowMainMenu();
        }

        /// <summary>Esc: диалог выхода закрывается, меню игры возвращает в игру, игровой экран открывает меню, персонаж — обратно в игру.</summary>
        /// <returns><c>true</c>, если Esc обработан.</returns>
    }
}
