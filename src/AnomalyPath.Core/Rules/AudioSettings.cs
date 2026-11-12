#nullable enable
using AnomalyPath.Core.Data;
using AnomalyPath.Core.Json;
using AnomalyPath.Core.Localization;

namespace AnomalyPath.Core.Rules
{
    /// <summary>
    /// Правила звука из данных: музыка меню и боя, громкости эмбиента и музыки
    /// и длительность плавного перехода между треками.
    /// </summary>
    public sealed class AudioSettings
    {
        /// <summary>Создаёт правила звука.</summary>
        /// <param name="menuMusicId">Id музыки меню в каталоге звуков.</param>
        /// <param name="combatMusicId">Id музыки боя в каталоге звуков.</param>
        /// <param name="ambienceVolume">Громкость слоя эмбиента, от 0 до 1.</param>
        /// <param name="musicVolume">Громкость слоя музыки, от 0 до 1.</param>
        /// <param name="fadeSeconds">Длительность плавного перехода между треками, секунды, не меньше нуля.</param>
        /// <exception cref="System.ArgumentException">Строковое поле пусто.</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">Громкость вне 0..1 или переход отрицательный.</exception>
        public AudioSettings(
            string menuMusicId,
            string combatMusicId,
            double ambienceVolume,
            double musicVolume,
            double fadeSeconds)
        {
            if (string.IsNullOrWhiteSpace(menuMusicId))
            {
                throw new System.ArgumentException(
                    CoreText.Format(CoreErrors.RequiredField, nameof(menuMusicId)),
                    nameof(menuMusicId));
            }

            if (string.IsNullOrWhiteSpace(combatMusicId))
            {
                throw new System.ArgumentException(
                    CoreText.Format(CoreErrors.RequiredField, nameof(combatMusicId)),
                    nameof(combatMusicId));
            }

            if (ambienceVolume < 0 || ambienceVolume > 1)
            {
                throw new System.ArgumentOutOfRangeException(nameof(ambienceVolume));
            }

            if (musicVolume < 0 || musicVolume > 1)
            {
                throw new System.ArgumentOutOfRangeException(nameof(musicVolume));
            }

            if (fadeSeconds < 0)
            {
                throw new System.ArgumentOutOfRangeException(nameof(fadeSeconds));
            }

            MenuMusicId = menuMusicId;
            CombatMusicId = combatMusicId;
            AmbienceVolume = ambienceVolume;
            MusicVolume = musicVolume;
            FadeSeconds = fadeSeconds;
        }

        /// <summary>Id музыки меню в каталоге звуков.</summary>
        public string MenuMusicId { get; }

        /// <summary>Id музыки боя в каталоге звуков.</summary>
        public string CombatMusicId { get; }

        /// <summary>Громкость слоя эмбиента, от 0 до 1.</summary>
        public double AmbienceVolume { get; }

        /// <summary>Громкость слоя музыки, от 0 до 1.</summary>
        public double MusicVolume { get; }

        /// <summary>Длительность плавного перехода между треками, секунды.</summary>
        public double FadeSeconds { get; }

        /// <summary>Читает правила звука из блока <c>audio</c> записи правил.</summary>
        /// <param name="value">Объект <c>audio</c>.</param>
        /// <param name="path">Путь для сообщения об ошибке.</param>
        /// <returns>Правила звука.</returns>
        /// <exception cref="DataException">Поля отсутствуют или значения вне границ.</exception>
        public static AudioSettings FromJson(JsonValue value, string path)
        {
            var menuMusicId = DataMap.RequireString(value, "menuMusic", path);
            var combatMusicId = DataMap.RequireString(value, "combatMusic", path);

            var ambienceVolume = DataMap.RequireDouble(value, "ambienceVolume", path);
            if (ambienceVolume < 0 || ambienceVolume > 1)
            {
                throw new DataException(path, CoreErrors.RulesChance, "audio.ambienceVolume");
            }

            var musicVolume = DataMap.RequireDouble(value, "musicVolume", path);
            if (musicVolume < 0 || musicVolume > 1)
            {
                throw new DataException(path, CoreErrors.RulesChance, "audio.musicVolume");
            }

            var fadeSeconds = DataMap.RequireDouble(value, "fadeSeconds", path);
            if (fadeSeconds < 0)
            {
                throw new DataException(path, CoreErrors.NonNegative, "audio.fadeSeconds");
            }

            return new AudioSettings(menuMusicId, combatMusicId, ambienceVolume, musicVolume, fadeSeconds);
        }
    }
}
