#nullable enable
using System;
using System.Collections.Generic;
using AnomalyPath.Core.Localization;

namespace AnomalyPath.Core.Quests.Story
{
    /// <summary>Взятый сюжетный квест: вариант, область прогресса, состояние и счётчики целей.</summary>
    public sealed class StoryQuestState
    {
        private readonly List<QuestObjectiveProgress> _progress;

        /// <summary>Создаёт состояние сюжетного квеста.</summary>
        /// <param name="questId">Идентификатор квеста.</param>
        /// <param name="storyLine">Линия квеста.</param>
        /// <param name="variantId">Выбранный вариант условий.</param>
        /// <param name="scope">Область прогресса.</param>
        /// <param name="progress">Прогресс целей в порядке варианта.</param>
        /// <param name="status">Состояние; по умолчанию взят.</param>
        /// <param name="acceptedAt">Когда взят, в абсолютных часах мира.</param>
        /// <param name="completedAt">Когда сдан, в абсолютных часах мира.</param>
        /// <exception cref="ArgumentException">Идентификатор квеста или линии пуст.</exception>
        /// <exception cref="ArgumentNullException">Прогресс не задан.</exception>
        public StoryQuestState(
            string questId,
            string storyLine,
            string variantId,
            QuestProgressScope scope,
            IReadOnlyList<QuestObjectiveProgress> progress,
            QuestStatus status = QuestStatus.Active,
            double acceptedAt = 0,
            double completedAt = 0)
        {
            if (string.IsNullOrWhiteSpace(questId))
            {
                throw new ArgumentException(
                    CoreText.Format(CoreErrors.RequiredField, nameof(questId)),
                    nameof(questId));
            }

            if (string.IsNullOrWhiteSpace(storyLine))
            {
                throw new ArgumentException(
                    CoreText.Format(CoreErrors.RequiredField, nameof(storyLine)),
                    nameof(storyLine));
            }

            QuestId = questId;
            StoryLine = storyLine;
            VariantId = string.IsNullOrWhiteSpace(variantId) ? StoryQuestTemplate.StandardVariantId : variantId;
            Scope = scope;
            _progress = progress == null
                ? throw new ArgumentNullException(nameof(progress))
                : new List<QuestObjectiveProgress>(progress);
            Status = status;
            AcceptedAt = acceptedAt;
            CompletedAt = completedAt;
        }

        /// <summary>Идентификатор квеста.</summary>
        public string QuestId { get; }

        /// <summary>Линия квеста.</summary>
        public string StoryLine { get; }

        /// <summary>Выбранный вариант условий.</summary>
        public string VariantId { get; }

        /// <summary>Область прогресса.</summary>
        public QuestProgressScope Scope { get; }

        /// <summary>Состояние квеста.</summary>
        public QuestStatus Status { get; private set; }

        /// <summary>Когда квест взят.</summary>
        public double AcceptedAt { get; private set; }

        /// <summary>Когда квест сдан.</summary>
        public double CompletedAt { get; private set; }

        /// <summary>Прогресс целей в порядке варианта.</summary>
        public IReadOnlyList<QuestObjectiveProgress> Progress => _progress;

        /// <summary>Возвращает прогресс цели по номеру.</summary>
        /// <param name="index">Номер цели.</param>
        /// <returns>Прогресс цели.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Номер вне списка.</exception>
        public QuestObjectiveProgress ObjectiveAt(int index) => _progress[index];

        /// <summary>Проверяет, выполнены ли все цели варианта.</summary>
        /// <param name="objectives">Цели варианта.</param>
        /// <returns><c>true</c>, если каждая цель достигла своего счётчика.</returns>
        public bool IsReady(IReadOnlyList<QuestObjective> objectives)
        {
            if (objectives.Count != _progress.Count)
            {
                return false;
            }

            for (var index = 0; index < objectives.Count; index++)
            {
                if (!_progress[index].IsDone(objectives[index].Count))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>Сбрасывает счётчики всех целей при смене рейда или смерти.</summary>
        public void ResetProgress()
        {
            foreach (var objective in _progress)
            {
                objective.Reset();
            }
        }

        /// <summary>Помечает квест сданным.</summary>
        /// <param name="now">Текущее время в абсолютных часах.</param>
        public void MarkCompleted(double now)
        {
            Status = QuestStatus.Completed;
            CompletedAt = now;
        }
    }
}
