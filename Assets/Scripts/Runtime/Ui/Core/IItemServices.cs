#nullable enable
namespace AnomalyPath.Runtime.Ui.Core
{
    /// <summary>Сервисы предметов и персонажа для экранов и виджетов: каталоги, инвентарь и правила.</summary>
    public interface IItemServices
    {
        /// <summary>Наборы снаряжения.</summary>
        AnomalyPath.Core.Abstractions.ILoadoutRepository Loadouts { get; }

        /// <summary>Пробная раскладка набора: что игрок получит на старте.</summary>
        AnomalyPath.Core.Abstractions.ILoadoutPreviewService LoadoutPreviews { get; }

        /// <summary>Предметы: названия и категории для отображения.</summary>
        AnomalyPath.Core.Abstractions.IItemRepository Items { get; }

        /// <summary>Артефакты: модификаторы надетого пояса для характеристик персонажа.</summary>
        AnomalyPath.Core.Abstractions.IArtifactService Artifacts { get; }

        /// <summary>Экипировка: число слотов артефактов и лимит веса с рюкзаком.</summary>
        AnomalyPath.Core.Abstractions.IEquipmentService Equipment { get; }

        /// <summary>Инвентарь: суммарный вес содержимого и лимит переноски.</summary>
        AnomalyPath.Core.Abstractions.IInventoryService Inventory { get; }

        /// <summary>Защита игрока от урона: база актёра, броня, шлем и артефакты.</summary>
        AnomalyPath.Core.Abstractions.IPlayerDamageService Damage { get; }

        /// <summary>Типы урона: строки защиты на экране персонажа.</summary>
        AnomalyPath.Core.Abstractions.IDamageTypeRepository DamageTypes { get; }

        /// <summary>Бросок лута по таблице: объекты локаций и тайники.</summary>
        AnomalyPath.Core.Abstractions.ILootService Loot { get; }

        /// <summary>Действия с предметами: надеть, снять, разрядить, разобрать, выбросить.</summary>
        AnomalyPath.Core.Abstractions.ICharacterActions Actions { get; }

        /// <summary>Применение расходников вне боя: медицина, еда и вода.</summary>
        AnomalyPath.Core.Abstractions.IConsumableService Consumables { get; }

        /// <summary>Генератор ников для новой игры.</summary>
        AnomalyPath.Core.Abstractions.INicknameGenerator Nicknames { get; }

        /// <summary>Правила прохождения.</summary>
        AnomalyPath.Core.Abstractions.IGameRulesRepository Rules { get; }
    }
}
