#nullable enable
using System;
using System.Collections.Generic;
using AnomalyPath.Core.Localization;

namespace AnomalyPath.Core.Quests
{
    /// <summary>
    /// Взятый квест: конкретная цель, прогресс и состояние. Цель фиксируется при взятии,
    /// поэтому квест можно сохранить и продолжить после загрузки.
    /// </summary>
    public sealed class QuestState
    {
        private readonly List<string> _actorIds;
        private readonly List<QuestItemTarget> _items;

        /// <summary>Создаёт состояние квеста.</summary>
        /// <param name="questId">Идентификатор квеста.</param>
        /// <param name="boardLocationId">Экземпляр хаба, на доске которого взят квест.</param>
        /// <param name="status">Состояние.</param>
        /// <param name="acceptedAt">Когда взят, в абсолютных часах мира.</param>
        /// <param name="cooldownUntil">До какого времени откат, в абсолютных часах.</param>
        /// <param name="completedAt">Когда сдан, в абсолютных часах.</param>
        /// <param name="locationTemplateId">Шаблон целевой локации.</param>
        /// <param name="locationInstanceId">Экземпляр целевой локации.</param>
        /// <param name="actorIds">Целевые мутанты или NPC.</param>
        /// <param name="items">Целевые предметы.</param>
        /// <param name="killCount">Сколько целей убито.</param>
        /// <param name="cleared">Зачищена ли локация.</param>
        /// <param name="requiredCount">Сколько целей нужно всего.</param>
        /// <param name="slot">Номер слота доски, на котором взят квест.</param>
        /// <exception cref="ArgumentException">Идентификаторы пусты.</exception>
        public QuestState(
            string questId,
            string boardLocationId,
            QuestStatus status,
            double acceptedAt,
            double cooldownUntil,
            double completedAt,
            string? locationTemplateId = null,
            string? locationInstanceId = null,
            IReadOnlyList<string>? actorIds = null,
            IReadOnlyList<QuestItemTarget>? items = null,
            int killCount = 0,
            bool cleared = false,
            int requiredCount = 1,
            int slot = 0)
        {
            if (string.IsNullOrWhiteSpace(questId))
            {
                throw new ArgumentException(
                    CoreText.Format(CoreErrors.RequiredField, nameof(questId)),
                    nameof(questId));
            }

            if (string.IsNullOrWhiteSpace(boardLocationId))
            {
                throw new ArgumentException(
                    CoreText.Format(CoreErrors.RequiredField, nameof(boardLocationId)),
                    nameof(boardLocationId));
            }

            if (killCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(killCount));
            }

            if (requiredCount < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(requiredCount));
            }

            if (slot < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(slot));
            }

            QuestId = questId;
            BoardLocationId = boardLocationId;
            Status = status;
            AcceptedAt = acceptedAt;
            CooldownUntil = cooldownUntil;
            CompletedAt = completedAt;
            LocationTemplateId = locationTemplateId;
            LocationInstanceId = locationInstanceId;
            _actorIds = actorIds == null ? new List<string>() : new List<string>(actorIds);
            _items = items == null ? new List<QuestItemTarget>() : new List<QuestItemTarget>(items);
            KillCount = killCount;
            Cleared = cleared;
            RequiredCount = requiredCount;
            Slot = slot;
        }

        /// <summary>Идентификатор квеста.</summary>
        public string QuestId { get; }

        /// <summary>Экземпляр хаба, на доске которого взят квест.</summary>
        public string BoardLocationId { get; }

        /// <summary>Состояние.</summary>
        public QuestStatus Status { get; internal set; }

        /// <summary>Когда квест взят.</summary>
        public double AcceptedAt { get; internal set; }

        /// <summary>До какого времени длится откат.</summary>
        public double CooldownUntil { get; internal set; }

        /// <summary>Когда квест сдан.</summary>
        public double CompletedAt { get; internal set; }

        /// <summary>Шаблон целевой локации.</summary>
        public string? LocationTemplateId { get; internal set; }

        /// <summary>Экземпляр целевой локации.</summary>
        public string? LocationInstanceId { get; internal set; }

        /// <summary>Целевые мутанты или NPC.</summary>
        public IReadOnlyList<string> ActorIds => _actorIds;

        /// <summary>Целевые предметы.</summary>
        public IReadOnlyList<QuestItemTarget> Items => _items;

        /// <summary>Сколько целей убито.</summary>
        public int KillCount { get; internal set; }

        /// <summary>Зачищена ли целевая локация.</summary>
        public bool Cleared { get; internal set; }

        /// <summary>Сколько целей нужно всего.</summary>
        public int RequiredCount { get; }

        /// <summary>Номер слота доски, на котором взят квест.</summary>
        public int Slot { get; }

        /// <summary>Добавляет целевого мутанта или NPC.</summary>
        /// <param name="actorId">Идентификатор актёра.</param>
        public void AddActor(string actorId)
        {
            if (!string.IsNullOrWhiteSpace(actorId) && !_actorIds.Contains(actorId))
            {
                _actorIds.Add(actorId);
            }
        }

        /// <summary>Добавляет целевой предмет.</summary>
        /// <param name="target">Требование предмета.</param>
        public void AddItem(QuestItemTarget target)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            _items.Add(target);
        }

        /// <summary>Увеличивает счётчик убитых целей.</summary>
        public void AddKill()
        {
            if (KillCount < int.MaxValue)
            {
                KillCount++;
            }
        }

        /// <summary>Отмечает целевую локацию зачищенной.</summary>
        public void MarkCleared() => Cleared = true;

        /// <summary>Проверяет, идёт ли откат после сдачи или провала.</summary>
        /// <param name="now">Текущее время в абсолютных часах.</param>
        /// <returns><c>true</c>, если квест ещё не может появиться снова.</returns>
        public bool IsOnCooldown(double now) =>
            (Status == QuestStatus.Completed || Status == QuestStatus.Failed) && now < CooldownUntil;

        /// <summary>Считает, сколько часов осталось до провала по сроку.</summary>
        /// <param name="now">Текущее время в абсолютных часах.</param>
        /// <param name="timeLimitHours">Срок с момента взятия в часах; 0 — срок не задан.</param>
        /// <returns>Остаток в часах, не меньше нуля; 0, если срок не задан.</returns>
        public double HoursLeft(double now, int timeLimitHours) =>
            timeLimitHours <= 0 ? 0 : Math.Max(0, AcceptedAt + timeLimitHours - now);

        /// <summary>Помечает предложение взятым.</summary>
        /// <param name="now">Текущее время в абсолютных часах.</param>
        public void Activate(double now)
        {
            Status = QuestStatus.Active;
            AcceptedAt = now;
        }

        /// <summary>Помечает квест отменённым.</summary>
        /// <param name="now">Текущее время в абсолютных часах.</param>
        public void MarkCancelled(double now)
        {
            Status = QuestStatus.Cancelled;
            CompletedAt = now;
        }

        /// <summary>Помечает квест сданным и запускает откат.</summary>
        /// <param name="now">Текущее время в абсолютных часах.</param>
        /// <param name="cooldownHours">Длительность отката в часах.</param>
        public void MarkCompleted(double now, int cooldownHours)
        {
            Status = QuestStatus.Completed;
            CompletedAt = now;
            CooldownUntil = now + Math.Max(0, cooldownHours);
        }

        /// <summary>Помечает квест проваленным и запускает откат.</summary>
        /// <param name="now">Текущее время в абсолютных часах.</param>
        /// <param name="cooldownHours">Длительность отката в часах; 0 — без отката.</param>
        public void MarkFailed(double now, int cooldownHours = 0)
        {
            Status = QuestStatus.Failed;
            CompletedAt = now;
            CooldownUntil = now + Math.Max(0, cooldownHours);
        }
    }
}
