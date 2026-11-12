#nullable enable
using System;
using System.IO;
using System.Reflection;

namespace AnomalyPath.Setup
{
    /// <summary>Доступ к ресурсам, встроенным в исполняемый файл установщика.</summary>
    internal static class EmbeddedResources
    {
        /// <summary>Имя ресурса с архивом игры.</summary>
        public const string Payload = "payload.zip";

        /// <summary>Имя ресурса с постером.</summary>
        public const string Poster = "poster.png";

        /// <summary>Имя ресурса с программой удаления.</summary>
        public const string Uninstaller = "uninstall.exe";

        /// <summary>Имя ресурса с данными поставки.</summary>
        public const string Info = "setup.info";

        /// <summary>Открывает поток встроенного ресурса.</summary>
        /// <param name="name">Имя ресурса.</param>
        /// <returns>Поток ресурса.</returns>
        /// <exception cref="InvalidOperationException">Ресурс не найден.</exception>
        public static Stream Open(string name)
        {
            return Assembly.GetExecutingAssembly().GetManifestResourceStream(name)
                   ?? throw new InvalidOperationException("встроенный ресурс не найден: " + name);
        }

        /// <summary>Читает ресурс целиком в память.</summary>
        /// <param name="name">Имя ресурса.</param>
        /// <returns>Содержимое ресурса.</returns>
        public static byte[] ReadAll(string name)
        {
            using var stream = Open(name);
            using var buffer = new MemoryStream();
            stream.CopyTo(buffer);
            return buffer.ToArray();
        }
    }
}
