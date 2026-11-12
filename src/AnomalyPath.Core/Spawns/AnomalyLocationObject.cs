#nullable enable
using System;
using System.Collections.Generic;
using AnomalyPath.Core.Inventory;
using AnomalyPath.Core.Localization;

namespace AnomalyPath.Core.Spawns
{
    /// <summary>
    /// Аномалия на локации: помнит, сколько попыток поиска потрачено, и хранит найденные,
    /// но не забранные артефакты до диалога лута.
    /// </summary>
    public sealed class AnomalyLocationObject : LocationObject
    {
        private readonly List<ItemInstance> _loot = new List<ItemInstance>();
        private readonly List<int> _artifactCells = new List<int>();
        private readonly List<int> _artifactAttempts = new List<int>();
        private readonly List<int> _scannedCells = new List<int>();
        private readonly List<int> _searchedCells = new List<int>();

        /// <summary>Создаёт объект-аномалию.</summary>
        /// <param name="spawnId">Id записи спавна.</param>
        /// <param name="titleKey">Ключ названия в каталоге строк.</param>
        /// <param name="anomalyId">Id аномалии из данных.</param>
        /// <exception cref="ArgumentException">Обязательная строка пуста.</exception>
        public AnomalyLocationObject(string spawnId, string titleKey, string anomalyId)
            : base(spawnId, titleKey, LocationObjectKind.Anomaly)
        {
            if (string.IsNullOrWhiteSpace(anomalyId))
            {
                throw new ArgumentException(
                    CoreText.Format(CoreErrors.RequiredField, nameof(anomalyId)),
                    nameof(anomalyId));
            }

            AnomalyId = anomalyId;
        }

        /// <summary>Id аномалии из данных.</summary>
        public string AnomalyId { get; }

        /// <summary>Сколько попыток поиска уже потрачено.</summary>
        public int AttemptsUsed { get; private set; }

        /// <summary>Раскладка артефактов по клеткам уже выполнена.</summary>
        public bool RadarPlaced { get; private set; }

        /// <summary>Клетки, в которых ещё лежат артефакты.</summary>
        public IReadOnlyList<int> ArtifactCells => _artifactCells;

        /// <summary>Номера бросков, которыми получены артефакты: параллельно <see cref="ArtifactCells"/>.</summary>
        public IReadOnlyList<int> ArtifactAttempts => _artifactAttempts;

        /// <summary>Отсканированные клетки.</summary>
        public IReadOnlyList<int> ScannedCells => _scannedCells;

        /// <summary>Обысканные клетки.</summary>
        public IReadOnlyList<int> SearchedCells => _searchedCells;

        /// <summary>Отмечает раскладку артефактов при открытии радара.</summary>
        /// <param name="cells">Клетки с артефактами.</param>
        /// <param name="attempts">Номера бросков, параллельно клеткам.</param>
        /// <exception cref="ArgumentNullException">Список не задан.</exception>
        /// <exception cref="ArgumentException">Длины списков различаются.</exception>
        public void PlaceRadar(IReadOnlyList<int> cells, IReadOnlyList<int> attempts)
        {
            if (cells == null || attempts == null)
            {
                throw new ArgumentNullException(nameof(cells));
            }

            if (cells.Count != attempts.Count)
            {
                throw new ArgumentException(
                    CoreText.Format(CoreErrors.SaveMissingField, "artifactAttempts"),
                    nameof(attempts));
            }

            _artifactCells.Clear();
            _artifactAttempts.Clear();
            for (var index = 0; index < cells.Count; index++)
            {
                _artifactCells.Add(cells[index]);
                _artifactAttempts.Add(attempts[index]);
            }

            RadarPlaced = true;
        }

        /// <summary>Проверяет, есть ли в клетке артефакт, и убирает его из скрытой раскладки.</summary>
        /// <param name="cell">Номер клетки.</param>
        /// <param name="attempt">Номер броска, которым получен артефакт.</param>
        /// <returns><c>true</c>, если артефакт был в клетке.</returns>
        public bool TryTakeArtifact(int cell, out int attempt)
        {
            var index = _artifactCells.IndexOf(cell);
            if (index < 0)
            {
                attempt = 0;
                return false;
            }

            attempt = _artifactAttempts[index];
            _artifactCells.RemoveAt(index);
            _artifactAttempts.RemoveAt(index);
            return true;
        }

        /// <summary>Отмечает клетку отсканированной.</summary>
        /// <param name="cell">Номер клетки.</param>
        public void MarkScanned(int cell)
        {
            if (!_scannedCells.Contains(cell))
            {
                _scannedCells.Add(cell);
            }
        }

        /// <summary>Отмечает клетку обысканной.</summary>
        /// <param name="cell">Номер клетки.</param>
        public void MarkSearched(int cell)
        {
            if (!_searchedCells.Contains(cell))
            {
                _searchedCells.Add(cell);
            }
        }

        /// <summary>Была ли клетка уже отсканирована.</summary>
        /// <param name="cell">Номер клетки.</param>
        /// <returns><c>true</c>, если скан уже был.</returns>
        public bool IsScanned(int cell) => _scannedCells.Contains(cell);

        /// <summary>Была ли клетка уже обыскана.</summary>
        /// <param name="cell">Номер клетки.</param>
        /// <returns><c>true</c>, если обыск уже был.</returns>
        public bool IsSearched(int cell) => _searchedCells.Contains(cell);

        /// <summary>Восстанавливает раскладку радара из сейва.</summary>
        /// <param name="placed">Раскладка уже выполнялась.</param>
        /// <param name="artifacts">Клетки с артефактами.</param>
        /// <param name="attempts">Номера бросков, параллельно клеткам.</param>
        /// <param name="scanned">Отсканированные клетки.</param>
        /// <param name="searched">Обысканные клетки.</param>
        /// <exception cref="ArgumentException">Длины списков артефактов различаются.</exception>
        public void RestoreRadar(
            bool placed,
            IReadOnlyList<int>? artifacts,
            IReadOnlyList<int>? attempts,
            IReadOnlyList<int>? scanned,
            IReadOnlyList<int>? searched)
        {
            var cells = artifacts ?? Array.Empty<int>();
            var rolls = attempts ?? Array.Empty<int>();
            if (cells.Count != rolls.Count)
            {
                throw new ArgumentException(
                    CoreText.Format(CoreErrors.SaveMissingField, "artifactAttempts"),
                    nameof(attempts));
            }

            RadarPlaced = placed;
            _artifactCells.Clear();
            _artifactAttempts.Clear();
            for (var index = 0; index < cells.Count; index++)
            {
                _artifactCells.Add(cells[index]);
                _artifactAttempts.Add(rolls[index]);
            }

            _scannedCells.Clear();
            if (scanned != null)
            {
                _scannedCells.AddRange(scanned);
            }

            _searchedCells.Clear();
            if (searched != null)
            {
                _searchedCells.AddRange(searched);
            }
        }

        /// <summary>Найденные артефакты, которые ещё не забрали.</summary>
        public IReadOnlyList<ItemInstance> Loot => _loot;

        /// <summary>Есть ли в аномалии незабранные артефакты.</summary>
        public bool HasLoot => _loot.Count > 0;

        /// <summary>Можно ли ещё искать: попытки не кончились.</summary>
        /// <param name="attempts">Общее число попыток аномалии из данных.</param>
        /// <returns><c>true</c>, если поиск доступен.</returns>
        public bool CanSearch(int attempts) => AttemptsUsed < attempts;

        /// <summary>Сколько попыток осталось.</summary>
        /// <param name="attempts">Общее число попыток аномалии из данных.</param>
        /// <returns>Остаток, не меньше нуля.</returns>
        public int RemainingAttempts(int attempts) => Math.Max(0, attempts - AttemptsUsed);

        /// <summary>Отмечает потраченную попытку.</summary>
        public void RegisterAttempt() => AttemptsUsed++;

        /// <summary>Восстанавливает лут и счётчик попыток из сейва.</summary>
        /// <param name="items">Оставшиеся артефакты; <c>null</c> — пусто.</param>
        /// <param name="attemptsUsed">Сколько попыток уже потрачено.</param>
        /// <exception cref="ArgumentOutOfRangeException">Счётчик попыток отрицателен.</exception>
        public void Restore(IReadOnlyList<ItemInstance>? items, int attemptsUsed)
        {
            if (attemptsUsed < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(attemptsUsed));
            }

            AttemptsUsed = attemptsUsed;
            _loot.Clear();
            if (items == null)
            {
                return;
            }

            foreach (var item in items)
            {
                if (item != null)
                {
                    _loot.Add(item);
                }
            }
        }

        /// <summary>Добавляет найденные артефакты в объект.</summary>
        /// <param name="items">Найденные артефакты; <c>null</c> — ничего не добавлять.</param>
        public void AddLoot(IReadOnlyList<ItemInstance>? items)
        {
            if (items == null)
            {
                return;
            }

            foreach (var item in items)
            {
                if (item != null)
                {
                    _loot.Add(item);
                }
            }
        }

        /// <summary>Забирает артефакт по id экземпляра.</summary>
        /// <param name="instanceId">Id экземпляра предмета.</param>
        /// <returns><c>true</c>, если предмет найден и убран из аномалии.</returns>
        public bool Take(string instanceId)
        {
            for (var index = 0; index < _loot.Count; index++)
            {
                if (!string.Equals(_loot[index].InstanceId, instanceId, StringComparison.Ordinal))
                {
                    continue;
                }

                _loot.RemoveAt(index);
                return true;
            }

            return false;
        }
    }
}
