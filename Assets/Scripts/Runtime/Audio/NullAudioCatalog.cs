#nullable enable
using UnityEngine;

namespace AnomalyPath.Runtime.Audio
{
    /// <summary>Пустой каталог звуков: отвечает «нет» на любой id, когда каталог не подключён.</summary>
    public sealed class NullAudioCatalog : IAudioCatalog
    {
        /// <summary>Единственный экземпляр пустого каталога.</summary>
        public static NullAudioCatalog Instance { get; } = new NullAudioCatalog();

        private NullAudioCatalog()
        {
        }

        /// <inheritdoc />
        public bool TryGet(string id, out AudioClip? clip)
        {
            clip = null;
            return false;
        }
    }
}
