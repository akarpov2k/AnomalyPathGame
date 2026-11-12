#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace AnomalyPath.Setup
{
    /// <summary>
    /// Данные поставки, которые установщик кладёт в реестр и показывает в окне:
    /// имя, версия, издатель, имя игры и размер полезной нагрузки.
    /// </summary>
    internal sealed class InstallInfo
    {
        private InstallInfo(string app, string displayName, string version, string publisher, string exeName, int fileCount, long bytes)
        {
            App = app;
            DisplayName = displayName;
            Version = version;
            Publisher = publisher;
            ExeName = exeName;
            FileCount = fileCount;
            Bytes = bytes;
        }

        /// <summary>Короткое имя приложения (ключ реестра, имя ярлыка).</summary>
        public string App { get; }

        /// <summary>Полное название для «Программ и компонентов».</summary>
        public string DisplayName { get; }

        /// <summary>Версия игры.</summary>
        public string Version { get; }

        /// <summary>Издатель.</summary>
        public string Publisher { get; }

        /// <summary>Имя исполняемого файла игры.</summary>
        public string ExeName { get; }

        /// <summary>Число файлов в поставке.</summary>
        public int FileCount { get; }

        /// <summary>Размер поставки в байтах.</summary>
        public long Bytes { get; }

        /// <summary>Разбирает ресурс <c>setup.info</c> вида <c>ключ=значение</c>.</summary>
        /// <param name="stream">Поток ресурса в UTF-8.</param>
        /// <returns>Данные поставки.</returns>
        /// <exception cref="InvalidDataException">В файле нет обязательных полей.</exception>
        public static InstallInfo Read(Stream stream)
        {
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            using (var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true))
            {
                while (reader.ReadLine() is { } line)
                {
                    int split = line.IndexOf('=');
                    if (split <= 0)
                    {
                        continue;
                    }

                    values[line.Substring(0, split).Trim()] = line.Substring(split + 1).Trim();
                }
            }

            string Get(string key) =>
                values.TryGetValue(key, out var value) && value.Length > 0
                    ? value
                    : throw new InvalidDataException("setup.info: нет поля " + key);

            int fileCount = int.Parse(values.TryGetValue("files", out var files) ? files : "0", CultureInfo.InvariantCulture);
            long bytes = long.Parse(values.TryGetValue("bytes", out var size) ? size : "0", CultureInfo.InvariantCulture);
            return new InstallInfo(
                Get("app"),
                Get("displayName"),
                Get("version"),
                Get("publisher"),
                Get("exe"),
                fileCount,
                bytes);
        }
    }
}
