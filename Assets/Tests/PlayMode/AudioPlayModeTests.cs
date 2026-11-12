#nullable enable
using System.Collections;
using AnomalyPath.Runtime.Audio;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AnomalyPath.Tests.PlayMode
{
    /// <summary>Звук: каталог клипов, музыка меню и боя и эмбиент текущей локации.</summary>
    public sealed class AudioPlayModeTests : PlayModeTestBase
    {
        /// <summary>Клип находится по id, неизвестный id не находится.</summary>
        [Test]
        public void AudioCatalog_TryGet_ReturnsClipById()
        {
            var clip = AudioClip.Create("test", 16, 1, 44100, false);
            var catalog = ScriptableObject.CreateInstance<AudioCatalog>();
            try
            {
                catalog.SetEntries(new[] { new AudioCatalog.Entry("amb-atp", clip) });

                Assert.IsTrue(catalog.TryGet("amb-atp", out var found), "клип должен находиться по id");
                Assert.AreSame(clip, found, "должен вернуться тот же клип");
                Assert.IsFalse(catalog.TryGet("amb-missing", out _), "неизвестный id не должен находиться");
            }
            finally
            {
                Object.DestroyImmediate(catalog);
                Object.DestroyImmediate(clip);
            }
        }

        /// <summary>Пустой каталог всегда отвечает «нет».</summary>
        [Test]
        public void NullAudioCatalog_AlwaysMisses()
        {
            Assert.IsFalse(NullAudioCatalog.Instance.TryGet("amb-atp", out var clip));
            Assert.IsNull(clip);
        }

        /// <summary>
        /// Прогон тестов не звучит: загрузка сцены с настройками громкости не возвращает звук,
        /// потому что слушатель остаётся на паузе.
        /// </summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Audio_AfterSceneLoad_StaysMuted()
        {
            yield return LoadMain();
            yield return null;

            Assert.IsTrue(AudioListener.pause, "звук PlayMode-прогона должен быть на паузе");
            Assert.IsNotNull(FindScreen().Audio.MusicClip, "в меню должна играть тема меню: проверяется, что звук есть, но молчит");
        }

        /// <summary>В стартовом меню играет тема меню, эмбиента нет.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator MainMenu_PlaysMenuThemeWithoutAmbience()
        {
            yield return LoadMain();

            var game = FindScreen();
            Assert.IsNull(game.Audio.AmbienceClip, "в меню не должно быть эмбиента локации");
            Assert.IsNotNull(game.Audio.MusicClip, "в меню должна играть тема меню");
            Assert.IsTrue(game.AudioClips.TryGet("mus-menu-theme", out var menu), "тема меню не найдена в каталоге");
            Assert.AreSame(menu, game.Audio.MusicClip, "должна играть тема из данных");
        }

        /// <summary>Новая игра включает эмбиент стартовой локации и выключает тему меню.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator NewGame_PlaysLocationAmbienceWithoutMusic()
        {
            yield return StartGame();

            var game = FindScreen();
            var location = game.Session!.State.World.CurrentLocation;
            var template = game.Locations.Get(location.TemplateId);

            Assert.IsNotNull(template.Ambience, "у стартовой локации должен быть эмбиент в данных");
            Assert.IsTrue(game.AudioClips.TryGet(template.Ambience!, out var clip), "эмбиент локации не найден в каталоге");
            Assert.AreSame(clip, game.Audio.AmbienceClip, "должен играть эмбиент текущей локации");
            Assert.IsNull(game.Audio.MusicClip, "на локации музыка не играет");
        }

        /// <summary>Бой включает музыку боя, эмбиент локации продолжает играть.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Combat_PlaysCombatMusicAndKeepsAmbience()
        {
            yield return StartGame();

            var game = FindScreen();
            var ambience = game.Audio.AmbienceClip;
            game.ShowCombat();
            yield return null;

            Assert.IsTrue(game.AudioClips.TryGet("mus-combat-light", out var combat), "музыка боя не найдена в каталоге");
            Assert.AreSame(combat, game.Audio.MusicClip, "должна играть музыка боя");
            Assert.AreSame(ambience, game.Audio.AmbienceClip, "эмбиент локации не должен прерываться");
        }

        /// <summary>Возврат из боя в игру выключает музыку боя и оставляет эмбиент локации.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator GameAfterCombat_StopsCombatMusic()
        {
            yield return StartGame();

            var game = FindScreen();
            var ambience = game.Audio.AmbienceClip;
            game.ShowCombat();
            yield return null;
            game.ShowGame(false);
            yield return null;

            Assert.IsNull(game.Audio.MusicClip, "после боя музыка не играет");
            Assert.AreSame(ambience, game.Audio.AmbienceClip, "эмбиент локации остаётся");
        }

        /// <summary>У каждой локации из данных эмбиент есть в каталоге звуков.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator EveryLocationAmbience_IsInCatalog()
        {
            yield return LoadMain();

            var game = FindScreen();
            foreach (var location in game.Locations.All)
            {
                if (location.Ambience == null)
                {
                    continue;
                }

                Assert.IsTrue(
                    game.AudioClips.TryGet(location.Ambience, out var clip),
                    $"Нет эмбиента {location.Ambience} для {location.Id}");
                Assert.IsNotNull(clip, $"Клип {location.Ambience} не загружен");
            }
        }
    }
}
