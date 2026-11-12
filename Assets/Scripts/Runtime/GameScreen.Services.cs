#nullable enable
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Core.Inventory;
using AnomalyPath.Core.Session;
using AnomalyPath.Runtime.Audio;
using AnomalyPath.Runtime.Screens.Combat;
using AnomalyPath.Runtime.Screens.Rewards;
using AnomalyPath.Runtime.Settings;
using AnomalyPath.Runtime.Ui.Core;
using AnomalyPath.Runtime.Ui.Dialogs;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace AnomalyPath.Runtime
{
    /// <summary>Часть класса <see cref="GameScreen"/>: сервисы, которые хост раздаёт экранам.</summary>
    public sealed partial class GameScreen
    {
        public RestDialog RestDialog => _restDialog;

        /// <inheritdoc />
        public UiFactory Ui { get; private set; } = null!;

        /// <inheritdoc />
        public ScreenRouter Router { get; } = new ScreenRouter();

        /// <inheritdoc />
        public GameSession? Session { get; set; }

        /// <inheritdoc />
        public GameSettingsService Settings { get; private set; } = null!;

        /// <summary>Боевой экран: журнал боя и действия последнего хода противника.</summary>
        public CombatScreen Combat => _combat;

        /// <summary>Экран наград: добыча, трофеи и награды за задания.</summary>
        public RewardScreen Rewards => _rewards;

        /// <inheritdoc />
        public IGameSessionService Sessions { get; private set; } = null!;

        /// <inheritdoc />
        public ISaveService Saves { get; private set; } = null!;

        /// <inheritdoc />
        public ILoadoutRepository Loadouts { get; private set; } = null!;

        /// <inheritdoc />
        public ILoadoutPreviewService LoadoutPreviews { get; private set; } = null!;

        /// <inheritdoc />
        public IItemRepository Items { get; private set; } = null!;

        /// <inheritdoc />
        public IRarityRepository Rarities { get; private set; } = null!;

        /// <inheritdoc />
        public IEquipmentService Equipment { get; private set; } = null!;

        /// <inheritdoc />
        public IInventoryService Inventory => _inventory;

        /// <inheritdoc />
        public IPlayerDamageService Damage { get; private set; } = null!;

        /// <inheritdoc />
        public IDamageTypeRepository DamageTypes { get; private set; } = null!;

        /// <inheritdoc />
        public ILootService Loot { get; private set; } = null!;

        /// <inheritdoc />
        public ILocationRepository Locations { get; private set; } = null!;

        /// <inheritdoc />
        public IAnomalyRepository Anomalies { get; private set; } = null!;

        /// <inheritdoc />
        public IInjuryRepository Injuries { get; private set; } = null!;

        /// <inheritdoc />
        public IArtifactService Artifacts { get; private set; } = null!;

        /// <inheritdoc />
        public ICharacterActions Actions { get; private set; } = null!;

        /// <inheritdoc />
        public IConsumableService Consumables { get; private set; } = null!;

        /// <inheritdoc />
        public ITradeService Trade { get; private set; } = null!;

        /// <inheritdoc />
        public IRepairService Repair { get; private set; } = null!;

        /// <inheritdoc />
        public IQuestService Quests { get; private set; } = null!;

        /// <inheritdoc />
        public IStoryQuestService StoryQuests { get; private set; } = null!;

        /// <inheritdoc />
        public IDialogService Dialogs { get; private set; } = null!;

        /// <inheritdoc />
        public ITalentService Talents { get; private set; } = null!;

        /// <inheritdoc />
        public IProgressionService Progression { get; private set; } = null!;

        /// <inheritdoc />
        public ICombatSettings CombatSettings { get; private set; } = null!;

        /// <inheritdoc />
        public ISkillRepository Skills { get; private set; } = null!;

        /// <inheritdoc />
        public ICombatService CombatEngine { get; private set; } = null!;

        /// <inheritdoc />
        public ISpriteCatalog Sprites => spriteCatalog != null ? spriteCatalog : NullSpriteCatalog.Instance;

        /// <summary>Каталог звуков: эмбиенты локаций и музыка по id из данных.</summary>
        public IAudioCatalog AudioClips => audioCatalog != null ? audioCatalog : NullAudioCatalog.Instance;

        /// <summary>Слой звука: эмбиент текущей локации и музыка экрана.</summary>
        public AudioDirector Audio => _audio;

        /// <inheritdoc />
        public IActorRepository Actors { get; private set; } = null!;

        /// <inheritdoc />
        public INicknameGenerator Nicknames { get; private set; } = null!;

        /// <inheritdoc />
        public IGameRulesRepository Rules { get; private set; } = null!;

        /// <inheritdoc />
        public ITopologyRepository Topology { get; private set; } = null!;

        /// <inheritdoc />
        public IGameTexts Texts { get; private set; } = null!;

        /// <inheritdoc />
        public ISpawnRepository Spawns { get; private set; } = null!;
    }
}
