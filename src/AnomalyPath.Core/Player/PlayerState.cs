#nullable enable
using System;
using System.Collections.Generic;
using AnomalyPath.Core.Combat;
using AnomalyPath.Core.Inventory;
using AnomalyPath.Core.Localization;
using AnomalyPath.Core.Quests.Story;
using InventoryEquipment = AnomalyPath.Core.Inventory.Equipment;

namespace AnomalyPath.Core.Player
{
    /// <summary>Состояние игрока: здоровье, опыт, инвентарь, экипировка и потребности.</summary>
    public sealed partial class PlayerState
    {
        private readonly List<string> _completedQuests;
        private readonly List<string> _unlocks;
        private readonly Dictionary<string, int> _reputation;
        private readonly Dictionary<string, int> _talentRanks;
        private readonly List<InjuryState> _injuries;
        private readonly List<StoryQuestState> _storyQuests;

        /// <summary>Создаёт состояние игрока.</summary>
        /// <param name="id">Идентификатор игрока.</param>
        /// <param name="level">Уровень.</param>
        /// <param name="xp">Опыт.</param>
        /// <param name="health">Текущее здоровье.</param>
        /// <param name="apMax">Максимум очков действия.</param>
        /// <param name="inventory">Сетка инвентаря.</param>
        /// <param name="equipment">Экипировка.</param>
        /// <param name="skills">Выученные скиллы.</param>
        /// <param name="needs">Потребности.</param>
        /// <param name="name">Ник персонажа; пусто — взять id.</param>
        /// <param name="money">Деньги на персонаже; по умолчанию 0.</param>
        /// <param name="completedQuests">Завершённые квесты.</param>
        /// <param name="unlocks">Полученные открытия.</param>
        /// <param name="reputation">Репутация по фракциям.</param>
        /// <param name="talentPoints">Свободные очки талантов; по умолчанию 0.</param>
        /// <param name="talentRanks">Ранги талантов по id; пусто — ни одного.</param>
        /// <param name="levelHealthBonus">Прибавка к максимуму здоровья за уровни; по умолчанию 0.</param>
        /// <param name="injuries">Активные травмы; пусто — без травм.</param>
        /// <param name="storyQuests">Взятые сюжетные квесты; пусто — ни одного.</param>
        /// <param name="deaths">Сколько раз игрок умирал; по умолчанию 0.</param>
        /// <exception cref="ArgumentException">id пуст или зависимости не заданы.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Числовые значения вне допустимых границ.</exception>
        public PlayerState(
            string id,
            int level,
            int xp,
            int health,
            int apMax,
            GridContainer inventory,
            InventoryEquipment equipment,
            IReadOnlyList<string> skills,
            NeedsState needs,
            string? name = null,
            int money = 0,
            IReadOnlyList<string>? completedQuests = null,
            IReadOnlyList<string>? unlocks = null,
            IReadOnlyDictionary<string, int>? reputation = null,
            int talentPoints = 0,
            IReadOnlyDictionary<string, int>? talentRanks = null,
            int levelHealthBonus = 0,
            IReadOnlyList<InjuryState>? injuries = null,
            IReadOnlyList<StoryQuestState>? storyQuests = null,
            int deaths = 0)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException(
                    CoreText.Format(CoreErrors.RequiredField, nameof(id)),
                    nameof(id));
            }

            if (level < 1 || xp < 0 || health < 0 || apMax < 0 || money < 0 || talentPoints < 0 ||
                levelHealthBonus < 0 || deaths < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(level));
            }

            if (talentRanks != null)
            {
                foreach (var pair in talentRanks)
                {
                    if (string.IsNullOrWhiteSpace(pair.Key) || pair.Value < 0)
                    {
                        throw new ArgumentOutOfRangeException(nameof(talentRanks));
                    }
                }
            }

            Id = id;
            Name = string.IsNullOrWhiteSpace(name) ? id : name.Trim();
            Level = level;
            Xp = xp;
            Health = health;
            ApMax = apMax;
            Money = money;
            TalentPoints = talentPoints;
            LevelHealthBonus = levelHealthBonus;
            Deaths = deaths;
            Inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
            Equipment = equipment ?? throw new ArgumentNullException(nameof(equipment));
            Skills = skills ?? throw new ArgumentNullException(nameof(skills));
            Needs = needs ?? throw new ArgumentNullException(nameof(needs));
            _completedQuests = completedQuests == null ? new List<string>() : new List<string>(completedQuests);
            _unlocks = unlocks == null ? new List<string>() : new List<string>(unlocks);
            _reputation = reputation == null
                ? new Dictionary<string, int>(StringComparer.Ordinal)
                : new Dictionary<string, int>(reputation, StringComparer.Ordinal);
            _talentRanks = talentRanks == null
                ? new Dictionary<string, int>(StringComparer.Ordinal)
                : new Dictionary<string, int>(talentRanks, StringComparer.Ordinal);
            _injuries = injuries == null ? new List<InjuryState>() : new List<InjuryState>(injuries);
            _storyQuests = storyQuests == null
                ? new List<StoryQuestState>()
                : new List<StoryQuestState>(storyQuests);
            CompletedQuests = _completedQuests;
            Unlocks = _unlocks;
            Reputation = _reputation;
            TalentRanks = _talentRanks;
            Injuries = _injuries;
            StoryQuests = _storyQuests;
        }

        /// <summary>Идентификатор игрока.</summary>
        public string Id { get; }

        /// <summary>Ник персонажа.</summary>
        public string Name { get; }

        /// <summary>Уровень.</summary>
        public int Level { get; private set; }

        /// <summary>Опыт, накопленный за всё прохождение.</summary>
        public int Xp { get; private set; }

        /// <summary>
        /// Прибавка к максимуму здоровья, полученная за повышения уровня. Хранится отдельно,
        /// потому что к максимуму добавляются ещё базовое здоровье актёра и бонусы талантов.
        /// </summary>
        public int LevelHealthBonus { get; private set; }

        /// <summary>Текущее здоровье.</summary>
        public int Health { get; private set; }

        /// <summary>Максимум очков действия.</summary>
        public int ApMax { get; }

        /// <summary>Деньги на персонаже.</summary>
        public int Money { get; private set; }

        /// <summary>Свободные очки талантов.</summary>
        public int TalentPoints { get; private set; }

        /// <summary>Сетка инвентаря.</summary>
        public GridContainer Inventory { get; }

        /// <summary>Экипировка.</summary>
        public InventoryEquipment Equipment { get; }

        /// <summary>Выученные скиллы.</summary>
        public IReadOnlyList<string> Skills { get; }

        /// <summary>Потребности.</summary>
        public NeedsState Needs { get; }

        /// <summary>Завершённые квесты.</summary>
        public IReadOnlyList<string> CompletedQuests { get; }

        /// <summary>Полученные открытия.</summary>
        public IReadOnlyList<string> Unlocks { get; }

        /// <summary>Репутация по фракциям.</summary>
        public IReadOnlyDictionary<string, int> Reputation { get; }

        /// <summary>Вложенные ранги талантов по идентификатору.</summary>
        public IReadOnlyDictionary<string, int> TalentRanks { get; }

        /// <summary>Активные травмы: переживают бой, тикают шагами боя или часами мира.</summary>
        public IReadOnlyList<InjuryState> Injuries { get; }

        /// <summary>Взятые сюжетные квесты: активные и сданные, в порядке взятия.</summary>
        public IReadOnlyList<StoryQuestState> StoryQuests { get; }

        /// <summary>Добавляет взятого сюжетного квеста.</summary>
        /// <param name="quest">Состояние квеста.</param>
        /// <exception cref="ArgumentNullException">Состояние не задано.</exception>
        public void AddStoryQuest(StoryQuestState quest)
        {
            if (quest == null)
            {
                throw new ArgumentNullException(nameof(quest));
            }

            _storyQuests.Add(quest);
        }

        /// <summary>Ищет сюжетный квест по идентификатору.</summary>
        /// <param name="questId">Идентификатор квеста.</param>
        /// <param name="quest">Найденное состояние или <c>null</c>.</param>
        /// <returns><c>true</c>, если квест есть в списке.</returns>
        public bool TryGetStoryQuest(string questId, out StoryQuestState? quest)
        {
            foreach (var item in _storyQuests)
            {
                if (string.Equals(item.QuestId, questId, StringComparison.Ordinal))
                {
                    quest = item;
                    return true;
                }
            }

            quest = null;
            return false;
        }

        /// <summary>Заменяет список травм: так бой возвращает травмы, полученные и истёкшие в бою.</summary>
        /// <param name="injuries">Новый список; <c>null</c> — очистить.</param>
        public void SetInjuries(IReadOnlyList<InjuryState>? injuries)
        {
            _injuries.Clear();
            if (injuries != null)
            {
                _injuries.AddRange(injuries);
            }
        }

        /// <summary>Накладывает травму; повторная с тем же id не складывается, а продлевает длительность.</summary>
        /// <param name="injury">Травма; <c>null</c> — ничего не делает.</param>
        public void ApplyInjury(InjuryState? injury)
        {
            if (injury == null)
            {
                return;
            }

            for (var i = 0; i < _injuries.Count; i++)
            {
                if (string.Equals(_injuries[i].Id, injury.Id, StringComparison.Ordinal))
                {
                    _injuries[i].Extend(injury.Remaining);
                    return;
                }
            }

            _injuries.Add(injury);
        }

        /// <summary>Снимает травму по id: лечение.</summary>
        /// <param name="injuryId">Идентификатор травмы.</param>
        /// <returns><c>true</c>, если травма была снята.</returns>
        public bool RemoveInjury(string injuryId)
        {
            for (var i = 0; i < _injuries.Count; i++)
            {
                if (string.Equals(_injuries[i].Id, injuryId, StringComparison.Ordinal))
                {
                    _injuries.RemoveAt(i);
                    return true;
                }
            }

            return false;
        }

        /// <summary>Тратит длительность травм указанного вида и убирает истёкшие.</summary>
        /// <param name="kind">Вид длительности: шаги боя или часы мира.</param>
        /// <param name="units">Сколько единиц прошло.</param>
        /// <returns>Id травм, которые истекли и были сняты.</returns>
        public IReadOnlyList<string> TickInjuries(InjuryDurationKind kind, int units)
        {
            var removed = new List<string>();
            for (var i = _injuries.Count - 1; i >= 0; i--)
            {
                var injury = _injuries[i];
                if (injury.Kind != kind)
                {
                    continue;
                }

                injury.Tick(units);
                if (!injury.IsExpired)
                {
                    continue;
                }

                removed.Add(injury.Id);
                _injuries.RemoveAt(i);
            }

            return removed;
        }

        /// <summary>Добавляет деньги.</summary>
        /// <param name="amount">Сумма; не может быть отрицательной.</param>
        /// <exception cref="ArgumentOutOfRangeException">Сумма отрицательна.</exception>
        public void AddMoney(int amount)
        {
            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount));
            }

            Money += amount;
        }

        /// <summary>Добавляет опыт.</summary>
        /// <param name="amount">Сколько опыта добавить; не меньше нуля.</param>
        /// <exception cref="ArgumentOutOfRangeException">Количество отрицательно.</exception>
        public void AddXp(int amount)
        {
            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount));
            }

            Xp += amount;
        }

        /// <summary>Списывает опыт, но не больше накопленного.</summary>
        /// <param name="amount">Сколько опыта забрать; не меньше нуля.</param>
        /// <returns>Сколько опыта списано на самом деле.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Количество отрицательно.</exception>
        public int RemoveXp(int amount)
        {
            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount));
            }

            var removed = Math.Min(amount, Xp);
            Xp -= removed;
            return removed;
        }

        /// <summary>Записывает уровень; понижать его сервис прогрессии не даёт.</summary>
        /// <param name="level">Новый уровень, не меньше единицы.</param>
        /// <exception cref="ArgumentOutOfRangeException">Уровень меньше единицы.</exception>
        public void SetLevel(int level)
        {
            if (level < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(level),
                    CoreText.Format(CoreErrors.LevelOutOfRange, level));
            }

            Level = level;
        }

        /// <summary>Добавляет прибавку к максимуму здоровья за уровни.</summary>
        /// <param name="amount">Сколько добавить; не меньше нуля.</param>
        /// <exception cref="ArgumentOutOfRangeException">Количество отрицательно.</exception>
        public void AddLevelHealthBonus(int amount)
        {
            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount));
            }

            LevelHealthBonus += amount;
        }

        /// <summary>Устанавливает текущее здоровье.</summary>
        /// <param name="health">Новое значение; не меньше нуля.</param>
        /// <exception cref="ArgumentOutOfRangeException">Здоровье отрицательно.</exception>
        public void SetHealth(int health)
        {
            if (health < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(health));
            }

            Health = health;
        }

        /// <summary>Списывает деньги, если их хватает.</summary>
        /// <param name="amount">Сумма; не может быть отрицательной.</param>
        /// <returns><c>true</c>, если деньги списаны.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Сумма отрицательна.</exception>
        public bool TrySpendMoney(int amount)
        {
            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount));
            }

            if (Money < amount)
            {
                return false;
            }

            Money -= amount;
            return true;
        }

        /// <summary>Проверяет, завершён ли квест.</summary>
        /// <param name="questId">Идентификатор квеста.</param>
        /// <returns><c>true</c>, если квест завершён.</returns>
        public bool HasCompletedQuest(string questId)
        {
            foreach (var quest in CompletedQuests)
            {
                if (string.Equals(quest, questId, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Проверяет, получено ли открытие.</summary>
        /// <param name="unlockId">Идентификатор открытия.</param>
        /// <returns><c>true</c>, если открытие получено.</returns>
        public bool HasUnlock(string unlockId)
        {
            foreach (var unlock in Unlocks)
            {
                if (string.Equals(unlock, unlockId, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Возвращает репутацию у фракции; 0, если фракция неизвестна.</summary>
        /// <param name="factionId">Идентификатор фракции.</param>
        /// <returns>Репутация.</returns>
        public int ReputationWith(string factionId) =>
            Reputation.TryGetValue(factionId, out var value) ? value : 0;

        /// <summary>Добавляет квест в список завершённых.</summary>
        /// <param name="questId">Идентификатор квеста.</param>
        public void CompleteQuest(string questId)
        {
            if (!string.IsNullOrWhiteSpace(questId) && !_completedQuests.Contains(questId))
            {
                _completedQuests.Add(questId);
            }
        }

        /// <summary>Добавляет открытие.</summary>
        /// <param name="unlockId">Идентификатор открытия.</param>
        public void AddUnlock(string unlockId)
        {
            if (!string.IsNullOrWhiteSpace(unlockId) && !_unlocks.Contains(unlockId))
            {
                _unlocks.Add(unlockId);
            }
        }

        /// <summary>Изменяет репутацию фракции.</summary>
        /// <param name="factionId">Идентификатор фракции.</param>
        /// <param name="delta">Изменение; может быть отрицательным.</param>
        public void AddReputation(string factionId, int delta)
        {
            if (string.IsNullOrWhiteSpace(factionId))
            {
                return;
            }

            _reputation[factionId] = ReputationWith(factionId) + delta;
        }
    }
}
