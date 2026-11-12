#nullable enable
using AnomalyPath.Core.Abstractions;

namespace AnomalyPath.Runtime.Ui.Core
{
    /// <summary>
    /// Полный хост интерфейса: общая часть <see cref="IScreenHost"/> и все доменные сервисы.
    /// Экраны его не используют — у каждого свой узкий интерфейс; полный хост нужен только
    /// виджетам, диалогам и хосту виджетов <see cref="IScreenPanel"/>.
    /// </summary>
    public interface IUiHost : IScreenHost
    {
        /// <summary>Наборы снаряжения.</summary>
        ILoadoutRepository Loadouts { get; }

        /// <summary>Пробная раскладка набора: что игрок получит на старте.</summary>
        ILoadoutPreviewService LoadoutPreviews { get; }

        /// <summary>Предметы: названия и категории для отображения.</summary>
        IItemRepository Items { get; }

        /// <summary>Редкости: прибавка к урону и точности оружия по ступени.</summary>
        IRarityRepository Rarities { get; }

        /// <summary>Шаблоны локаций: названия для основного экрана и меню.</summary>
        ILocationRepository Locations { get; }

        /// <summary>Аномалии: число попыток и тиры локаций.</summary>
        IAnomalyRepository Anomalies { get; }

        /// <summary>Травмы: иконки и названия активных травм персонажа.</summary>
        IInjuryRepository Injuries { get; }

        /// <summary>Артефакты: модификаторы надетого пояса для характеристик персонажа.</summary>
        IArtifactService Artifacts { get; }

        /// <summary>Экипировка: число слотов артефактов и лимит веса с рюкзаком.</summary>
        IEquipmentService Equipment { get; }

        /// <summary>Инвентарь: суммарный вес содержимого и лимит переноски.</summary>
        IInventoryService Inventory { get; }

        /// <summary>Защита игрока от урона: база актёра, броня, шлем и артефакты.</summary>
        IPlayerDamageService Damage { get; }

        /// <summary>Типы урона: строки защиты на экране персонажа.</summary>
        IDamageTypeRepository DamageTypes { get; }

        /// <summary>Бросок лута по таблице: объекты локаций и тайники.</summary>
        ILootService Loot { get; }

        /// <summary>Действия с предметами: надеть, снять, разрядить, разобрать, выбросить.</summary>
        ICharacterActions Actions { get; }

        /// <summary>Применение расходников вне боя: медицина, еда и вода.</summary>
        IConsumableService Consumables { get; }

        /// <summary>Торговля: доступность торговцев, ассортимент и сделки.</summary>
        ITradeService Trade { get; }

        /// <summary>Ремонт снаряжения у торговца-механика.</summary>
        IRepairService Repair { get; }

        /// <summary>Повторяемые задания: предложения торговцев, взятие и сдача.</summary>
        IQuestService Quests { get; }

        /// <summary>Сюжетные квесты: прогресс и сдача.</summary>
        IStoryQuestService StoryQuests { get; }

        /// <summary>Диалоги: начало разговора и выбор вариантов.</summary>
        IDialogService Dialogs { get; }

        /// <summary>Прокачка: дерево талантов, очки и ранги.</summary>
        ITalentService Talents { get; }

        /// <summary>Прогрессия уровней: требование следующего уровня для полосы опыта.</summary>
        IProgressionService Progression { get; }

        /// <summary>Цены действий боя из данных: перезарядка и смена оружия в подсказках.</summary>
        ICombatSettings CombatSettings { get; }

        /// <summary>Скиллы из данных: названия действий оружия в бою.</summary>
        ISkillRepository Skills { get; }

        /// <summary>Актёры: максимум здоровья игрока для полосы.</summary>
        IActorRepository Actors { get; }

        /// <summary>Генератор ников для новой игры.</summary>
        INicknameGenerator Nicknames { get; }

        /// <summary>Правила прохождения.</summary>
        IGameRulesRepository Rules { get; }

        /// <summary>Топология: номер звена и цена возврата в хаб в сводке локации.</summary>
        ITopologyRepository Topology { get; }

        /// <summary>Записи спавна: картинка объекта на сцене локации по id спавна.</summary>
        ISpawnRepository Spawns { get; }
    }
}
