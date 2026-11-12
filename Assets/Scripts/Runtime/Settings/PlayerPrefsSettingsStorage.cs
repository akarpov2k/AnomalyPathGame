#nullable enable
using UnityEngine;

namespace AnomalyPath.Runtime.Settings
{
    /// <summary>Машинное хранилище настроек в <see cref="PlayerPrefs"/>.</summary>
    public sealed class PlayerPrefsSettingsStorage : IGameSettingsStorage
    {
        /// <inheritdoc />
        public int GetInt(string key, int fallback) => PlayerPrefs.GetInt(key, fallback);

        /// <inheritdoc />
        public void SetInt(string key, int value) => PlayerPrefs.SetInt(key, value);

        /// <inheritdoc />
        public string GetString(string key, string fallback) => PlayerPrefs.GetString(key, fallback);

        /// <inheritdoc />
        public void SetString(string key, string value) => PlayerPrefs.SetString(key, value);

        /// <inheritdoc />
        public void Save() => PlayerPrefs.Save();
    }
}
