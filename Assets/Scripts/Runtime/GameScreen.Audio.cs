#nullable enable
using AnomalyPath.Runtime.Audio;
using UnityEngine;

namespace AnomalyPath.Runtime
{
    /// <summary>Часть класса <see cref="GameScreen"/>: слои эмбиента и музыки по экранам.</summary>
    public sealed partial class GameScreen
    {
        private void EnsureAudio()
        {
            _audio = audioDirector != null ? audioDirector : GetComponent<AudioDirector>();
            if (_audio == null)
            {
                _audio = gameObject.AddComponent<AudioDirector>();
            }

            var audio = Rules.Require().Audio;
            if (audio != null)
            {
                _audio.Configure(
                    (float)audio.AmbienceVolume,
                    (float)audio.MusicVolume,
                    (float)audio.FadeSeconds);
            }
        }

        /// <summary>Эмбиент текущей локации; музыка выключается.</summary>
        private void PlayLocationAudio()
        {
            var location = Session?.State.World.CurrentLocation;
            var ambienceId = location != null ? Locations.Get(location.TemplateId).Ambience : null;
            _audio.SetAmbience(ResolveClip(ambienceId));
            _audio.SetMusic(null);
        }

        /// <summary>Музыка меню; эмбиент локации выключается.</summary>
        private void PlayMenuAudio()
        {
            _audio.SetAmbience(null);
            _audio.SetMusic(ResolveClip(Rules.Require().Audio?.MenuMusicId));
        }

        /// <summary>Музыка боя; эмбиент локации продолжает играть.</summary>
        private void PlayCombatAudio() =>
            _audio.SetMusic(ResolveClip(Rules.Require().Audio?.CombatMusicId));

        private AudioClip? ResolveClip(string? id) =>
            id != null && AudioClips.TryGet(id, out var clip) ? clip : null;
    }
}
