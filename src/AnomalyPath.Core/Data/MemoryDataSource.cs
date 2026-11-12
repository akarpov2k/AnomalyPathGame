#nullable enable
using System;
using System.Collections.Generic;
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Core.Localization;

namespace AnomalyPath.Core.Data
{
    /// <summary>
    /// Источник данных в памяти: файлы уже прочитаны из пакета. Так работает сборка на Android,
    /// где папку данных заменить нечем.
    /// </summary>
    public sealed class MemoryDataSource : IDataSource
    {
        private readonly Dictionary<string, string> _files;

        /// <summary>Создаёт источник из набора файлов.</summary>
        /// <param name="files">Файлы «путь → текст»; путь с прямыми или обратными слэшами.</param>
        /// <exception cref="ArgumentNullException">Набор не задан.</exception>
        public MemoryDataSource(IReadOnlyDictionary<string, string> files)
        {
            if (files == null)
            {
                throw new ArgumentNullException(nameof(files));
            }

            _files = new Dictionary<string, string>(files.Count, StringComparer.Ordinal);
            foreach (var pair in files)
            {
                _files[Normalize(pair.Key)] = pair.Value ?? string.Empty;
            }
        }

        /// <summary>Сколько файлов в источнике.</summary>
        public int Count => _files.Count;

        /// <inheritdoc />
        public bool Exists(string relativePath) =>
            !string.IsNullOrWhiteSpace(relativePath) && _files.ContainsKey(Normalize(relativePath));

        /// <inheritdoc />
        /// <exception cref="DataException">Путь пуст или файла нет.</exception>
        public string ReadAllText(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                throw new DataException("<empty>", CoreErrors.EmptyPath);
            }

            if (!_files.TryGetValue(Normalize(relativePath), out var text))
            {
                throw new DataException(relativePath, CoreErrors.FileNotFound);
            }

            return text;
        }

        /// <inheritdoc />
        public IReadOnlyList<string> EnumerateFiles()
        {
            var result = new List<string>(_files.Keys);
            result.Sort(StringComparer.Ordinal);
            return result;
        }

        private static string Normalize(string path) => (path ?? string.Empty).Replace('\\', '/').TrimStart('/');
    }
}
