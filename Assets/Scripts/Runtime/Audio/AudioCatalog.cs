#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;

namespace AnomalyPath.Runtime.Audio
{
    /// <summary>
    /// Каталог звуков: пары «id из данных → клип». Заполняется редакторным инструментом
    /// <c>Tools/AnomalyPath/Собрать каталог звуков</c> из <c>Assets/Audio</c>, id — имя файла без расширения.
    /// </summary>
    [CreateAssetMenu(menuName = "AnomalyPath/Audio Catalog", fileName = "AudioCatalog")]
    public sealed class AudioCatalog : ScriptableObject, IAudioCatalog
    {
        /// <summary>Запись каталога: id и клип.</summary>
        [Serializable]
        public sealed class Entry
        {
            [SerializeField] private string id = string.Empty;
            [SerializeField] private AudioClip? value;

            /// <summary>Создаёт пустую запись.</summary>
            public Entry()
            {
            }

            /// <summary>Создаёт запись каталога.</summary>
            /// <param name="id">Id из данных.</param>
            /// <param name="value">Клип.</param>
            public Entry(string id, AudioClip value)
            {
                this.id = id;
                this.value = value;
            }

            /// <summary>Id из данных.</summary>
            public string Id => id;

            /// <summary>Клип или <c>null</c>.</summary>
            public AudioClip? Value => value;
        }

        [SerializeField] private List<Entry> entries = new List<Entry>();

        private Dictionary<string, AudioClip>? _map;

        /// <summary>Записи каталога.</summary>
        public IReadOnlyList<Entry> Entries => entries;

        /// <inheritdoc />
        public bool TryGet(string id, out AudioClip? clip)
        {
            clip = null;
            if (string.IsNullOrEmpty(id))
            {
                return false;
            }

            _map ??= BuildMap();
            return _map.TryGetValue(id, out clip);
        }

        /// <summary>Заменяет все записи каталога.</summary>
        /// <param name="items">Новые записи; записи без клипа пропускаются.</param>
        public void SetEntries(IEnumerable<Entry> items)
        {
            entries.Clear();
            foreach (var item in items)
            {
                if (!string.IsNullOrEmpty(item.Id) && item.Value != null)
                {
                    entries.Add(new Entry(item.Id, item.Value!));
                }
            }

            _map = null;
        }

        /// <summary>Очищает каталог.</summary>
        public void Clear()
        {
            entries.Clear();
            _map = null;
        }

        private Dictionary<string, AudioClip> BuildMap()
        {
            var map = new Dictionary<string, AudioClip>(StringComparer.Ordinal);
            foreach (var entry in entries)
            {
                if (!string.IsNullOrEmpty(entry.Id) && entry.Value != null)
                {
                    map[entry.Id] = entry.Value!;
                }
            }

            return map;
        }
    }
}
