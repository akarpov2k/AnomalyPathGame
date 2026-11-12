#nullable enable
using System;
using System.Collections.Generic;
using AnomalyPath.Core.Localization;
using AnomalyPath.Core.Spawns;

namespace AnomalyPath.Core.World
{
    /// <summary>Один сгенерированный экземпляр локации.</summary>
    public sealed class LocationInstance
    {
        private readonly List<LocationObject> _objects = new List<LocationObject>();
        private readonly List<string> _stashLockGuesses = new List<string>();

        /// <summary>Создаёт экземпляр локации.</summary>
        /// <param name="instanceId">Уникальный id экземпляра.</param>
        /// <param name="templateId">Из какого шаблона создан.</param>
        /// <param name="seed">Seed наполнения.</param>
        /// <param name="background">Выбранный фон.</param>
        /// <param name="subtype">Подтип локации.</param>
        /// <param name="tier">Тир опасности.</param>
        /// <param name="danger">Сложность от 0 до 1.</param>
        /// <param name="routeStep">Сколько шагов от хаба до локации; у хаба 0.</param>
        /// <param name="routeEdge">Номер звена маршрута; у хаба 0.</param>
        /// <exception cref="ArgumentException">Обязательные строки пусты.</exception>
        public LocationInstance(
            string instanceId,
            string templateId,
            int seed,
            string background,
            LocationSubtype subtype,
            int tier,
            double danger,
            int routeStep = 0,
            int routeEdge = 0)
        {
            if (string.IsNullOrWhiteSpace(instanceId))
            {
                throw new ArgumentException(
                    CoreText.Format(CoreErrors.RequiredField, nameof(instanceId)),
                    nameof(instanceId));
            }

            if (string.IsNullOrWhiteSpace(templateId))
            {
                throw new ArgumentException(
                    CoreText.Format(CoreErrors.RequiredField, nameof(templateId)),
                    nameof(templateId));
            }

            InstanceId = instanceId;
            TemplateId = templateId;
            Seed = seed;
            Background = background;
            Subtype = subtype;
            Tier = tier;
            Danger = danger;
            RouteStep = routeStep;
            RouteEdge = routeEdge;
        }

        /// <summary>Уникальный id экземпляра.</summary>
        public string InstanceId { get; }

        /// <summary>Из какого шаблона создан.</summary>
        public string TemplateId { get; }

        /// <summary>Seed наполнения.</summary>
        public int Seed { get; }

        /// <summary>Выбранный фон.</summary>
        public string Background { get; }

        /// <summary>Подтип локации.</summary>
        public LocationSubtype Subtype { get; }

        /// <summary>Сколько шагов от хаба до локации; у хаба 0, у ветки — как у её входа на путь.</summary>
        public int RouteStep { get; }

        /// <summary>Номер звена маршрута; у хаба 0, у ветки — звену, от которого она отходит.</summary>
        public int RouteEdge { get; }

        /// <summary>Тир опасности.</summary>
        public int Tier { get; }

        /// <summary>Сложность от 0 до 1.</summary>
        public double Danger { get; }

        /// <summary>Был ли игрок в локации.</summary>
        public bool Visited { get; private set; }

        /// <summary>Зачищена ли локация.</summary>
        public bool Cleared { get; private set; }

        /// <summary>Противник побеждён или сбежал и больше здесь не появляется.</summary>
        public bool EnemyGone { get; private set; }

        /// <summary>Проверялось ли появление тайника при входе.</summary>
        public bool StashRolled { get; private set; }

        /// <summary>Есть ли на локации невскрытый тайник.</summary>
        public bool StashPresent { get; private set; }

        /// <summary>Вскрыт ли тайник локации.</summary>
        public bool StashOpened { get; private set; }

        /// <summary>Сколько попыток взлома уже потрачено.</summary>
        public int StashLockAttemptsUsed { get; private set; }

        /// <summary>Замок тайника вскрыт верным кодом.</summary>
        public bool StashLockSolved { get; private set; }

        /// <summary>Попытки взлома кончились: тайник потерян навсегда.</summary>
        public bool StashLockFailed { get; private set; }

        /// <summary>Введённые коды в порядке попыток.</summary>
        public IReadOnlyList<string> StashLockGuesses => _stashLockGuesses;

        /// <summary>Объекты, появившиеся на локации при входе.</summary>
        public IReadOnlyList<LocationObject> Objects => _objects;

        /// <summary>Проведён ли бросок спавна локации; повторно объекты не перебрасываются.</summary>
        public bool Spawned { get; private set; }

        /// <summary>Добавляет объект, появившийся на локации.</summary>
        /// <param name="locationObject">Объект локации.</param>
        /// <exception cref="ArgumentNullException">Объект не задан.</exception>
        public void AddObject(LocationObject locationObject)
        {
            _objects.Add(locationObject ?? throw new ArgumentNullException(nameof(locationObject)));
        }

        /// <summary>Отмечает, что бросок спавна выполнен (в том числе с пустым результатом).</summary>
        public void MarkSpawned() => Spawned = true;

        /// <summary>Противник на локации; <c>null</c> — противника нет или он уже побеждён.</summary>
        public LocationObject? Enemy
        {
            get
            {
                foreach (var locationObject in _objects)
                {
                    if (locationObject.Kind == LocationObjectKind.Enemy && !locationObject.Looted)
                    {
                        return locationObject;
                    }
                }

                return null;
            }
        }

        /// <summary>Противник с указанным актёром; <c>null</c> — такого объекта нет.</summary>
        /// <param name="actorId">Id актёра типа <c>enemy</c>.</param>
        /// <returns>Объект-противник или <c>null</c>.</returns>
        public LocationObject? FindEnemy(string actorId)
        {
            foreach (var locationObject in _objects)
            {
                if (locationObject is EnemyLocationObject enemy &&
                    !enemy.Looted &&
                    string.Equals(enemy.ActorId, actorId, StringComparison.Ordinal))
                {
                    return enemy;
                }
            }

            return null;
        }

        /// <summary>Отмечает противника побеждённым: объект больше не появляется.</summary>
        /// <param name="actorId">Id актёра типа <c>enemy</c>.</param>
        public void MarkEnemyDefeated(string actorId) => FindEnemy(actorId)?.MarkLooted();

        /// <summary>Отмечает, что игрок был здесь.</summary>
        public void MarkVisited() => Visited = true;

        /// <summary>Отмечает проверку появления тайника при входе.</summary>
        public void MarkStashRolled() => StashRolled = true;

        /// <summary>Отмечает, что на локации появился тайник.</summary>
        public void MarkStashPresent() => StashPresent = true;

        /// <summary>Отмечает, что тайник локации вскрыт.</summary>
        public void MarkStashOpened() => StashOpened = true;

        /// <summary>Записывает неудачную попытку взлома и её код.</summary>
        /// <param name="guess">Введённый код.</param>
        /// <exception cref="ArgumentException">Код пуст.</exception>
        public void RegisterStashLockGuess(string guess)
        {
            if (string.IsNullOrWhiteSpace(guess))
            {
                throw new ArgumentException(
                    CoreText.Format(CoreErrors.RequiredField, nameof(guess)),
                    nameof(guess));
            }

            StashLockAttemptsUsed++;
            _stashLockGuesses.Add(guess);
        }

        /// <summary>Отмечает, что замок вскрыт верным кодом.</summary>
        public void MarkStashLockSolved()
        {
            StashLockSolved = true;
            StashLockFailed = false;
        }

        /// <summary>Отмечает, что попытки кончились и тайник потерян.</summary>
        public void MarkStashLockFailed()
        {
            StashLockFailed = true;
        }

        /// <summary>Восстанавливает состояние замка из сейва.</summary>
        /// <param name="attemptsUsed">Сколько попыток потрачено.</param>
        /// <param name="solved">Замок вскрыт.</param>
        /// <param name="failed">Попытки кончились.</param>
        /// <param name="guesses">Введённые коды; <c>null</c> — пусто.</param>
        /// <exception cref="ArgumentOutOfRangeException">Счётчик попыток отрицателен.</exception>
        /// <exception cref="ArgumentException">Одновременно вскрыт и потерян.</exception>
        public void RestoreStashLock(
            int attemptsUsed,
            bool solved,
            bool failed,
            IReadOnlyList<string>? guesses)
        {
            if (attemptsUsed < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(attemptsUsed));
            }

            if (solved && failed)
            {
                throw new ArgumentException(
                    CoreText.Format(CoreErrors.SaveMissingField, "stashLock"),
                    nameof(solved));
            }

            StashLockAttemptsUsed = attemptsUsed;
            StashLockSolved = solved;
            StashLockFailed = failed;
            _stashLockGuesses.Clear();
            if (guesses == null)
            {
                return;
            }

            foreach (var guess in guesses)
            {
                if (!string.IsNullOrWhiteSpace(guess))
                {
                    _stashLockGuesses.Add(guess);
                }
            }
        }

        /// <summary>Отмечает локацию зачищенной.</summary>
        public void MarkCleared() => Cleared = true;

        /// <summary>Отмечает, что противник побеждён или покинул локацию и больше здесь не появляется.</summary>
        public void MarkEnemyGone() => EnemyGone = true;
    }
}
