#nullable enable
using System.Collections;
using UnityEngine;

namespace AnomalyPath.Runtime.Audio
{
    /// <summary>
    /// Слой звука игры: зацикленный эмбиент локации и музыка с плавным переходом.
    /// Клипы передаёт <c>GameScreen</c> по id из каталога; громкости и длительность перехода — из правил.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AudioDirector : MonoBehaviour
    {
        [Tooltip("Источник эмбиента: зацикленный, без пространственного затухания")]
        [SerializeField]
        private AudioSource? ambienceSource = null;

        [Tooltip("Источник музыки: зацикленный, без пространственного затухания")]
        [SerializeField]
        private AudioSource? musicSource = null;

        private AudioSource _ambience = null!;
        private AudioSource _music = null!;
        private AudioClip? _ambienceClip;
        private AudioClip? _musicClip;
        private float _ambienceVolume = 1f;
        private float _musicVolume = 1f;
        private float _fadeSeconds;
        private Coroutine? _ambienceFade;
        private Coroutine? _musicFade;

        /// <summary>Текущий клип эмбиента или <c>null</c>, если эмбиент молчит.</summary>
        public AudioClip? AmbienceClip => _ambienceClip;

        /// <summary>Текущий клип музыки или <c>null</c>, если музыка молчит.</summary>
        public AudioClip? MusicClip => _musicClip;

        /// <summary>Громкость слоя эмбиента, 0..1.</summary>
        public float AmbienceVolume => _ambienceVolume;

        /// <summary>Громкость слоя музыки, 0..1.</summary>
        public float MusicVolume => _musicVolume;

        private void Awake() => Resolve();

        /// <summary>Задаёт громкости слоёв и длительность плавного перехода.</summary>
        /// <param name="ambienceVolume">Громкость эмбиента, 0..1; значение вне границ обрезается.</param>
        /// <param name="musicVolume">Громкость музыки, 0..1; значение вне границ обрезается.</param>
        /// <param name="fadeSeconds">Длительность перехода, секунды; отрицательное значение считается нулём.</param>
        public void Configure(float ambienceVolume, float musicVolume, float fadeSeconds)
        {
            _ambienceVolume = Mathf.Clamp01(ambienceVolume);
            _musicVolume = Mathf.Clamp01(musicVolume);
            _fadeSeconds = Mathf.Max(0f, fadeSeconds);
            ApplyVolume(_ambience, ref _ambienceFade, _ambienceClip, _ambienceVolume);
            ApplyVolume(_music, ref _musicFade, _musicClip, _musicVolume);
        }

        /// <summary>Включает зацикленный эмбиент или выключает его значением <c>null</c>.</summary>
        /// <param name="clip">Клип эмбиента или <c>null</c>, чтобы остановить слой.</param>
        public void SetAmbience(AudioClip? clip) =>
            Switch(_ambience, ref _ambienceFade, ref _ambienceClip, clip, _ambienceVolume);

        /// <summary>Включает зацикленную музыку или выключает её значением <c>null</c>.</summary>
        /// <param name="clip">Клип музыки или <c>null</c>, чтобы остановить слой.</param>
        public void SetMusic(AudioClip? clip) =>
            Switch(_music, ref _musicFade, ref _musicClip, clip, _musicVolume);

        private void Resolve()
        {
            _ambience = ambienceSource != null ? ambienceSource : CreateSource("Ambience");
            _music = musicSource != null ? musicSource : CreateSource("Music");
            _ambienceClip = _ambience.clip;
            _musicClip = _music.clip;

            if (FindAnyObjectByType<AudioListener>() == null)
            {
                gameObject.AddComponent<AudioListener>();
            }
        }

        private AudioSource CreateSource(string name)
        {
            var host = new GameObject(name);
            host.transform.SetParent(transform, false);
            var source = host.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = true;
            source.spatialBlend = 0f;
            source.volume = 0f;
            return source;
        }

        private void Switch(
            AudioSource source,
            ref Coroutine? fade,
            ref AudioClip? current,
            AudioClip? clip,
            float volume)
        {
            if (current == clip)
            {
                if (clip != null && !source.isPlaying)
                {
                    source.Play();
                }

                return;
            }

            if (fade != null)
            {
                StopCoroutine(fade);
                fade = null;
            }

            current = clip;
            if (clip == null)
            {
                fade = StartCoroutine(FadeOut(source));
                return;
            }

            source.clip = clip;
            source.loop = true;
            source.volume = _fadeSeconds > 0f ? 0f : volume;
            source.Play();
            if (_fadeSeconds > 0f)
            {
                fade = StartCoroutine(FadeIn(source, volume));
            }
        }

        private void ApplyVolume(AudioSource source, ref Coroutine? fade, AudioClip? clip, float volume)
        {
            if (clip == null)
            {
                return;
            }

            if (fade != null)
            {
                StopCoroutine(fade);
                fade = null;
            }

            source.volume = volume;
        }

        private IEnumerator FadeIn(AudioSource source, float target)
        {
            var time = 0f;
            while (time < _fadeSeconds)
            {
                time += Time.unscaledDeltaTime;
                source.volume = Mathf.Lerp(0f, target, time / _fadeSeconds);
                yield return null;
            }

            source.volume = target;
        }

        private IEnumerator FadeOut(AudioSource source)
        {
            var start = source.volume;
            var time = 0f;
            while (time < _fadeSeconds)
            {
                time += Time.unscaledDeltaTime;
                source.volume = Mathf.Lerp(start, 0f, time / _fadeSeconds);
                yield return null;
            }

            source.Stop();
            source.clip = null;
            source.volume = 0f;
        }
    }
}
