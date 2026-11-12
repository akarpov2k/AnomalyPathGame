#nullable enable
using System.Collections.Generic;
using AnomalyPath.Core.Artifacts;
using AnomalyPath.Core.Talents;
using EquipmentState = AnomalyPath.Core.Inventory.Equipment;

namespace AnomalyPath.Core.Combat
{
    /// <summary>Участник боя: здоровье, ОД и экипировка.</summary>
    public sealed class Combatant
    {
        private readonly List<CombatStatus> _statuses = new List<CombatStatus>();
        /// <summary>Создаёт участника боя из шаблона.</summary>
        /// <param name="template">Актёр.</param>
        /// <param name="equipment">Экипировка игрока, если есть.</param>
        /// <param name="modifiers">Бонусы талантов игрока; <c>null</c> — без бонусов.</param>
        /// <param name="healthBonus">Прибавка к максимуму здоровья за уровни игрока; 0 — без неё.</param>
        /// <param name="rarityAccuracyBonus">Поправка к меткости за редкость актёра; 0 — без неё.</param>
        /// <param name="artifacts">Бонусы надетых артефактов; <c>null</c> — без бонусов.</param>
        /// <exception cref="System.ArgumentNullException">Шаблон не передан.</exception>
        public Combatant(
            ActorTemplate template,
            EquipmentState? equipment = null,
            TalentModifiers? modifiers = null,
            int healthBonus = 0,
            double rarityAccuracyBonus = 0,
            ArtifactModifiers? artifacts = null)
        {
            Template = template ?? throw new System.ArgumentNullException(nameof(template));
            Equipment = equipment;
            var bonus = modifiers ?? TalentModifiers.None;
            var artifactBonus = artifacts ?? ArtifactModifiers.None;
            MaxHealth = (template.Combat?.Health ?? 0) + bonus.HealthMax + healthBonus + artifactBonus.HealthMax;
            Health = MaxHealth;
            MaxAp = (template.Combat?.Ap ?? 0) + bonus.ApMax;
            Ap = MaxAp;
            AccuracyBonus = bonus.Accuracy + artifactBonus.Accuracy;
            RarityAccuracyBonus = rarityAccuracyBonus;
        }

        private Combatant(Combatant source)
        {
            Template = source.Template;
            Equipment = source.Equipment;
            MaxHealth = source.MaxHealth;
            MaxAp = source.MaxAp;
            AccuracyBonus = source.AccuracyBonus;
            RarityAccuracyBonus = source.RarityAccuracyBonus;
            Health = source.Health;
            Ap = source.Ap;
            PityBonus = source.PityBonus;
            foreach (var status in source._statuses)
            {
                _statuses.Add(status.Clone());
            }
        }

        /// <summary>Копия участника для расчёта плана ИИ: изменения копии не трогают бой.</summary>
        /// <returns>Независимая копия с тем же здоровьем, ОД, статусами и «пожалей».</returns>
        internal Combatant Clone() => new Combatant(this);

        /// <summary>Актёр.</summary>
        public ActorTemplate Template { get; }

        /// <summary>Экипировка; у врагов обычно отсутствует.</summary>
        public EquipmentState? Equipment { get; }

        /// <summary>Статусы участника: травмы и будущие бафы и дебафы.</summary>
        public IReadOnlyList<CombatStatus> Statuses => _statuses;

        /// <summary>Сумма модификаторов от статусов.</summary>
        public CombatModifiers Modifiers => CombatModifiers.FromStatuses(_statuses);

        /// <summary>
        /// Накладывает статус. Повторная травма с тем же id не складывает модификаторы,
        /// а продлевает длительность: продление перечислено в правиле травм.
        /// </summary>
        /// <param name="status">Статус.</param>
        /// <exception cref="System.ArgumentNullException">Статус не передан.</exception>
        public void ApplyStatus(CombatStatus status)
        {
            if (status == null)
            {
                throw new System.ArgumentNullException(nameof(status));
            }

            for (var i = 0; i < _statuses.Count; i++)
            {
                if (string.Equals(_statuses[i].Id, status.Id, System.StringComparison.Ordinal))
                {
                    _statuses[i].Extend(status.Remaining);
                    return;
                }
            }

            _statuses.Add(status);
        }

        /// <summary>Снимает статус по id: лечение или истечение.</summary>
        /// <param name="id">Идентификатор статуса.</param>
        /// <returns><c>true</c>, если статус был снят.</returns>
        public bool RemoveStatus(string id)
        {
            for (var i = 0; i < _statuses.Count; i++)
            {
                if (string.Equals(_statuses[i].Id, id, System.StringComparison.Ordinal))
                {
                    _statuses.RemoveAt(i);
                    return true;
                }
            }

            return false;
        }

        /// <summary>Тратит длительность статусов указанного вида и убирает истёкшие.</summary>
        /// <param name="kind">Вид длительности: часы мира.</param>
        /// <param name="units">Сколько единиц прошло.</param>
        /// <returns>Id статусов, которые истекли и были сняты.</returns>
        public IReadOnlyList<string> TickStatuses(InjuryDurationKind kind, int units)
        {
            var removed = new List<string>();
            for (var i = _statuses.Count - 1; i >= 0; i--)
            {
                var status = _statuses[i];
                if (status.Kind != kind)
                {
                    continue;
                }

                status.Tick(units);
                if (!status.IsExpired)
                {
                    continue;
                }

                removed.Add(status.Id);
                _statuses.RemoveAt(i);
            }

            return removed;
        }

        /// <summary>Текущее здоровье.</summary>
        public double Health { get; internal set; }

        /// <summary>Максимальное здоровье.</summary>
        public int MaxHealth { get; }

        /// <summary>Текущие очки действия.</summary>
        public int Ap { get; internal set; }

        /// <summary>Максимум ОД.</summary>
        public int MaxAp { get; }

        /// <summary>Бонус меткости от талантов.</summary>
        public double AccuracyBonus { get; }

        /// <summary>Поправка к меткости за редкость актёра.</summary>
        public double RarityAccuracyBonus { get; }

        /// <summary>Уровень актёра.</summary>
        public int Level => Template.Combat?.Level ?? 1;

        /// <summary>Накопленный бонус «пожалей»: 5% базового шанса за каждый промах.</summary>
        public double PityBonus { get; internal set; }

        /// <summary><c>true</c>, если здоровье на нуле.</summary>
        public bool IsDead => Health <= 0;

        /// <summary>Базовая защита персонажа по типу урона: характеристика актёра без снаряжения и статусов.</summary>
        /// <param name="damageType">Тип урона.</param>
        /// <returns>Значение защиты; 0, если нет.</returns>
        public int OwnProtection(string damageType)
        {
            var protection = Template.Combat?.Protection;
            if (protection == null)
            {
                return 0;
            }

            return protection.TryGetValue(damageType, out var value) ? value : 0;
        }
    }
}
