#nullable enable
namespace AnomalyPath.Runtime.Settings
{
    /// <summary>Хранилище машинных настроек игры: чисел и строк по ключу.</summary>
    public interface IGameSettingsStorage
    {
        /// <summary>Читает целое значение.</summary>
        /// <param name="key">Ключ настройки.</param>
        /// <param name="fallback">Значение, если настройки нет.</param>
        /// <returns>Сохранённое или запасное значение.</returns>
        int GetInt(string key, int fallback);

        /// <summary>Записывает целое значение.</summary>
        /// <param name="key">Ключ настройки.</param>
        /// <param name="value">Значение.</param>
        void SetInt(string key, int value);

        /// <summary>Читает строковое значение.</summary>
        /// <param name="key">Ключ настройки.</param>
        /// <param name="fallback">Значение, если настройки нет.</param>
        /// <returns>Сохранённое или запасное значение.</returns>
        string GetString(string key, string fallback);

        /// <summary>Записывает строковое значение.</summary>
        /// <param name="key">Ключ настройки.</param>
        /// <param name="value">Значение.</param>
        void SetString(string key, string value);

        /// <summary>Сохраняет накопленные значения на диск.</summary>
        void Save();
    }
}
