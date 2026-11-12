#nullable enable
using NUnit.Framework;
using UnityEngine;

namespace AnomalyPath.Tests.PlayMode
{
    /// <summary>
    /// Тишина на весь PlayMode-прогон. Сцена Main создаёт <c>GameSettingsService</c> и применяет
    /// громкость из настроек (<c>AudioListener.volume</c>), поэтому обнуления громкости в начале
    /// набора мало: при загрузке сцены звук возвращается. Слушатель ставится на паузу — игровой код
    /// её не трогает, поэтому звук молчит и после загрузки сцены, и при смене экранов.
    /// </summary>
    [SetUpFixture]
    public sealed class TestAudioMute
    {
        private static float _volume = 1f;
        private static bool _paused;

        /// <summary>Выключает звук перед первым тестом прогона.</summary>
        [OneTimeSetUp]
        public void Mute()
        {
            _volume = AudioListener.volume;
            _paused = AudioListener.pause;
            AudioListener.volume = 0f;
            AudioListener.pause = true;
        }

        /// <summary>Возвращает прежнюю громкость и паузу звука после последнего теста прогона.</summary>
        [OneTimeTearDown]
        public void Restore()
        {
            AudioListener.pause = _paused;
            AudioListener.volume = _volume;
        }
    }
}
