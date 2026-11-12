#nullable enable
using System;
using System.Collections.Generic;
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Core.Json;
using AnomalyPath.Core.Localization;

namespace AnomalyPath.Core.Data
{
    /// <summary>
    /// Пакет данных в одном JSON-документе: «путь файла → его текст». Нужен сборкам, где папки
    /// данных нет: на Android содержимое <c>StreamingAssets</c> лежит внутри APK, и обычное
    /// чтение файлов по пути недоступно.
    /// </summary>
    public static class DataPack
    {
        /// <summary>Имя пакета без расширения — по нему пакет ищется в ресурсах сборки.</summary>
        public const string ResourceName = "AnomalyPathData";

        /// <summary>Поле версии формата пакета.</summary>
        public const string VersionField = "version";

        /// <summary>Поле списка файлов «путь → текст».</summary>
        public const string FilesField = "files";

        /// <summary>Версия формата пакета, которую понимает загрузчик.</summary>
        public const int FormatVersion = 1;

        /// <summary>Упаковывает все файлы источника в текст пакета.</summary>
        /// <param name="source">Источник файлов данных.</param>
        /// <returns>Текст пакета; порядок ключей детерминирован.</returns>
        /// <exception cref="ArgumentNullException">Источник не задан.</exception>
        public static string Serialize(IDataSource source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            var files = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var file in source.EnumerateFiles())
            {
                if (file.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                files[Normalize(file)] = source.ReadAllText(file);
            }

            return Serialize(files);
        }

        /// <summary>Упаковывает подготовленный набор файлов в текст пакета.</summary>
        /// <param name="files">Файлы «путь → текст».</param>
        /// <returns>Текст пакета; порядок ключей детерминирован.</returns>
        /// <exception cref="ArgumentNullException">Набор не задан.</exception>
        public static string Serialize(IReadOnlyDictionary<string, string> files)
        {
            if (files == null)
            {
                throw new ArgumentNullException(nameof(files));
            }

            var packed = JsonValue.NewObject();
            packed.Members[VersionField] = JsonValue.FromNumber(FormatVersion);

            var entries = JsonValue.NewObject();
            foreach (var pair in files)
            {
                entries.Members[Normalize(pair.Key)] = JsonValue.FromString(pair.Value ?? string.Empty);
            }

            packed.Members[FilesField] = entries;
            return JsonWriter.Write(packed, indented: false);
        }

        /// <summary>Разбирает текст пакета в набор файлов.</summary>
        /// <param name="text">Текст пакета.</param>
        /// <returns>Файлы «путь → текст».</returns>
        /// <exception cref="DataException">Пакет пуст, повреждён или с неподдерживаемой версией.</exception>
        public static IReadOnlyDictionary<string, string> Parse(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                throw new DataException(ResourceName, CoreErrors.DataPackInvalid);
            }

            var root = ParseRoot(text);
            if (!root.IsObject)
            {
                throw new DataException(ResourceName, CoreErrors.DataPackInvalid);
            }

            if (!root.TryGet(VersionField, out var version) || version.Kind != JsonKind.Number)
            {
                throw new DataException(ResourceName, CoreErrors.DataPackInvalid);
            }

            if ((int)version.Number != FormatVersion)
            {
                throw new DataException(ResourceName, CoreErrors.DataPackVersion, version.Number);
            }

            if (!root.TryGet(FilesField, out var entries) || !entries.IsObject)
            {
                throw new DataException(ResourceName, CoreErrors.DataPackInvalid);
            }

            var files = new Dictionary<string, string>(entries.Members.Count, StringComparer.Ordinal);
            foreach (var pair in entries.Members)
            {
                if (pair.Value.Kind != JsonKind.String)
                {
                    throw new DataException(ResourceName, CoreErrors.DataPackInvalid);
                }

                files[Normalize(pair.Key)] = pair.Value.Text;
            }

            return files;
        }

        /// <summary>Создаёт источник данных из текста пакета.</summary>
        /// <param name="text">Текст пакета.</param>
        /// <returns>Источник, читающий файлы из памяти.</returns>
        /// <exception cref="DataException">Пакет пуст, повреждён или с неподдерживаемой версией.</exception>
        public static IDataSource ToSource(string text) => new MemoryDataSource(Parse(text));

        private static JsonValue ParseRoot(string text)
        {
            try
            {
                return JsonParser.Parse(text, ResourceName);
            }
            catch (DataException)
            {
                throw new DataException(ResourceName, CoreErrors.DataPackInvalid);
            }
        }

        private static string Normalize(string path) => (path ?? string.Empty).Replace('\\', '/').TrimStart('/');
    }
}
