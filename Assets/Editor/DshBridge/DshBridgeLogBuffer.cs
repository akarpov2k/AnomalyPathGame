#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;

namespace AnomalyPath.Editor.DshBridge
{
    /// <summary>
    /// Кольцевой буфер сообщений консоли редактора: мост отдаёт их по команде
    /// <c>log</c>, чтобы ошибки компиляции и исключения были видны агенту.
    /// </summary>
    internal static class DshBridgeLogBuffer
    {
        /// <summary>Сколько сообщений хранится; старые вытесняются.</summary>
        private const int Capacity = 400;

        private static readonly ConcurrentQueue<Entry> Queue = new ConcurrentQueue<Entry>();
        private static int installed;

        /// <summary>Подписывается на консоль редактора один раз на домен.</summary>
        internal static void Install()
        {
            if (System.Threading.Interlocked.Exchange(ref installed, 1) == 1)
            {
                return;
            }

            Application.logMessageReceivedThreaded += OnLog;
        }

        /// <summary>
        /// Возвращает последние сообщения.
        /// </summary>
        /// <param name="limit">Сколько записей вернуть (1..<see cref="Capacity"/>).</param>
        /// <param name="level">Фильтр по типу: <c>error</c>, <c>warning</c>, <c>log</c> или <c>all</c>.</param>
        /// <returns>Список записей от старых к новым.</returns>
        internal static IReadOnlyList<Entry> Tail(int limit, string level)
        {
            var filter = ParseLevel(level);
            var result = new List<Entry>();
            var all = new List<Entry>(Queue);
            for (var i = 0; i < all.Count; i++)
            {
                var entry = all[i];
                if (filter is not null && entry.Type != filter)
                {
                    continue;
                }

                result.Add(entry);
            }

            if (limit >= 0 && result.Count > limit)
            {
                result.RemoveRange(0, result.Count - limit);
            }

            return result;
        }

        private static LogType? ParseLevel(string level)
        {
            switch ((level ?? "all").ToLowerInvariant())
            {
                case "error":
                case "errors":
                    return LogType.Error;
                case "exception":
                case "exceptions":
                    return LogType.Exception;
                case "warning":
                case "warnings":
                    return LogType.Warning;
                case "log":
                case "logs":
                    return LogType.Log;
                default:
                    return null;
            }
        }

        private static void OnLog(string condition, string stackTrace, LogType type)
        {
            Queue.Enqueue(new Entry
            {
                Time = DateTime.Now.ToString("HH:mm:ss"),
                Type = type,
                Message = condition,
                Stack = stackTrace,
            });

            while (Queue.Count > Capacity && Queue.TryDequeue(out _))
            {
                // Вытесняем самые старые записи.
            }
        }

        /// <summary>Одно сообщение консоли.</summary>
        internal sealed class Entry
        {
            /// <summary>Время сообщения (локальное, <c>HH:mm:ss</c>).</summary>
            public string Time { get; set; } = string.Empty;

            /// <summary>Тип сообщения Unity.</summary>
            public LogType Type { get; set; }

            /// <summary>Текст сообщения.</summary>
            public string Message { get; set; } = string.Empty;

            /// <summary>Стек вызова.</summary>
            public string Stack { get; set; } = string.Empty;
        }
    }
}
