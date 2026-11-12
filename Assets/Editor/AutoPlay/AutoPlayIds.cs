#nullable enable
namespace AnomalyPath.Editor.AutoPlay
{
    /// <summary>
    /// Протокол автопрогона: имена объектов интерфейса, идентификаторы сюжетной линии
    /// и места, к которым привязан сценарий главы 1. Это диагностический редакторный
    /// сценарий, а не игровые данные: содержимое повторяет <c>Assets/Data</c>, и при
    /// смене данных сценарий обновляется вместе с тестами PlayMode.
    /// </summary>
    internal static class AutoPlayIds
    {
        /// <summary>Путь игровой сцены.</summary>
        public const string MainScenePath = "Assets/Scenes/Main.unity";

        /// <summary>Сюжетные квесты главы 1 в порядке прохождения.</summary>
        public static readonly string[] StoryQuestOrder =
        {
            "qst_story_01_cordon",
            "qst_story_02_job",
            "qst_story_03_dogs",
            "qst_story_04_atp",
            "qst_story_05_angar"
        };

        /// <summary>Кто выдаёт и принимает сюжет главы 1 (кроме S5).</summary>
        public const string SmugglerActorId = "trd_hub_smuggler";

        /// <summary>Кто принимает S5.</summary>
        public const string GvozdActorId = "npc_angar_gvozd";

        /// <summary>Запись спавна Гвоздя на большом хабе Депо.</summary>
        public const string GvozdSpawnId = "spawn_angar_gvozd";

        /// <summary>Шаблон локации Депо: его экземпляр открывает S5.</summary>
        public const string DepotTemplateId = "loc_hub_depot";

        /// <summary>Цепочка АТП: шаги проходятся по порядку от входа с маршрута.</summary>
        public const string ChainAtpId = "chain_atp";

        /// <summary>Соль seed кодового замка тайника: та же строка в <c>StashService</c>.</summary>
        public const string StashLockSalt = "stash_lock:";

        /// <summary>Панель стартового меню.</summary>
        public const string MainMenuPanel = "MainMenu";

        /// <summary>Панель новой игры.</summary>
        public const string NewGamePanel = "NewGame";

        /// <summary>Кнопка «Новая игра» в стартовом меню.</summary>
        public const string NewGameButton = "Новая игра";

        /// <summary>Кнопка «Начать» на экране новой игры.</summary>
        public const string StartButton = "Начать";

        /// <summary>Большой хаб.</summary>
        public const string BigHubPanel = "BigHub";

        /// <summary>Комната торговца (малый хаб).</summary>
        public const string HubRoomPanel = "Hub";

        /// <summary>Экран локации.</summary>
        public const string PlayPanel = "InGame";

        /// <summary>Боевой экран.</summary>
        public const string CombatPanel = "Combat";

        /// <summary>Экран наград.</summary>
        public const string RewardsPanel = "Rewards";

        /// <summary>Карта мира.</summary>
        public const string MapPanel = "Map";

        /// <summary>Диалог NPC поверх экрана локации.</summary>
        public const string NpcDialogPanel = "NpcDialog";

        /// <summary>Окно взлома тайника.</summary>
        public const string LockPanel = "LockMinigame";

        /// <summary>Модальное подтверждение.</summary>
        public const string ConfirmPanel = "ConfirmDialog";

        /// <summary>Меню игры.</summary>
        public const string GameMenuPanel = "GameMenu";

        /// <summary>Окно инвентаря.</summary>
        public const string InventoryPanel = "Inventory";

        /// <summary>Значок торговцев на большом хабе.</summary>
        public const string TradersButton = "Traders";

        /// <summary>Выпадашка выбора торговца.</summary>
        public const string TraderMenuPanel = "TraderMenu";

        /// <summary>Кнопка карты мира на большом хабе.</summary>
        public const string MapButton = "Map";

        /// <summary>Дверь хаба: в большом хабе — выход на маршрут, в комнате — возврат.</summary>
        public const string LeaveHubButton = "LeaveHub";

        /// <summary>Кнопка возврата в хаб с маршрута.</summary>
        public const string ReturnToHubButton = "ReturnToHub";

        /// <summary>Кнопка тайника на экране локации.</summary>
        public const string StashButton = "Stash";

        /// <summary>Кнопка «Взять всё» на экране наград.</summary>
        public const string RewardTakeAllButton = "RewardTakeAll";

        /// <summary>Кнопка закрытия экрана наград.</summary>
        public const string RewardCloseButton = "RewardClose";

        /// <summary>Полоса «Поговорить» в меню диалога.</summary>
        public const string DialogTalkButton = "DialogTalk";

        /// <summary>Меню разделов диалога.</summary>
        public const string DialogMenuPanel = "DialogMenu";

        /// <summary>Окно разговора диалога.</summary>
        public const string DialogTalkWindow = "DialogTalkWindow";

        /// <summary>Префикс кнопки фразы диалога: дальше идёт id варианта.</summary>
        public const string DialogPhrasePrefix = "DialogPhrase_";

        /// <summary>Кнопка атаки на боевом экране.</summary>
        public const string CombatAttackButton = "CombatAttack";

        /// <summary>Кнопка конца хода.</summary>
        public const string CombatEndTurnButton = "CombatEndTurn";

        /// <summary>Кнопка скиллов и перезарядки.</summary>
        public const string CombatSkillsButton = "CombatSkills";

        /// <summary>Строка перезарядки в выпадашке скиллов.</summary>
        public const string CombatReloadButton = "CombatReload";

        /// <summary>Кнопка возврата после боя.</summary>
        public const string CombatBackButton = "CombatBack";

        /// <summary>Кнопка закрытия инвентаря.</summary>
        public const string InventoryCloseButton = "InventoryClose";

        /// <summary>Кнопка «Продолжить» в меню игры.</summary>
        public const string GameMenuContinueButton = "Продолжить";

        /// <summary>Кнопка закрытия карты мира.</summary>
        public const string MapCloseButton = "Close";

        /// <summary>Кнопка пешего перехода между хабами.</summary>
        public const string MapWalkButton = "Walk";

        /// <summary>Кнопка перехода с проводником.</summary>
        public const string MapGuideButton = "Guide";

        /// <summary>Префикс вырезки хаба на карте мира.</summary>
        public const string MapSpotPrefix = "Spot_";

        /// <summary>Префикс кнопки символа кодового замка.</summary>
        public const string LockSymbolPrefix = "Symbol";

        /// <summary>Кнопка проверки кода замка.</summary>
        public const string LockSubmitButton = "LockSubmit";

        /// <summary>Кнопка закрытия окна замка.</summary>
        public const string LockCloseButton = "LockClose";

        /// <summary>Префикс фигуры объекта локации: дальше идёт id записи спавна.</summary>
        public const string LocationObjectPrefix = "Object_";

        /// <summary>Приставка перехода сцены локации: центр, справа ниже, слева выше.</summary>
        public const string TransitionPrefix = "Transition_";

        /// <summary>Кнопка внутри объекта-перехода.</summary>
        public const string TransitionArt = "TransitionArt";
    }
}
