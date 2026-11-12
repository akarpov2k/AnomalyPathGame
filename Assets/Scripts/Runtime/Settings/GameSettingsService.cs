#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AnomalyPath.Runtime.Settings
{
    /// <summary>
    /// Машинные настройки игры: разрешение окна, громкость, чувствительность мыши и привязки
    /// горячих клавиш. Хранятся в хранилище <see cref="IGameSettingsStorage"/>.
    /// В игре это <see cref="PlayerPrefs"/>; в сейв и в данные контента настройки не попадают.
    /// Экран настроек меняет значения сразу, экран клавиш держит рабочую копию
    /// и сохраняет её кнопкой «Сохранить».
    /// </summary>
    public sealed class GameSettingsService
    {
        /// <summary>Минимальная громкость звука.</summary>
        public const int MinVolume = 0;

        /// <summary>Максимальная громкость звука.</summary>
        public const int MaxVolume = 100;

        /// <summary>Громкость по умолчанию.</summary>
        public const int DefaultVolume = 100;

        /// <summary>Минимальная чувствительность мыши.</summary>
        public const int MinMouseSensitivity = 0;

        /// <summary>Максимальная чувствительность мыши.</summary>
        public const int MaxMouseSensitivity = 100;

        /// <summary>Чувствительность мыши по умолчанию.</summary>
        public const int DefaultMouseSensitivity = 50;

        /// <summary>Ширина окна по умолчанию, если настройки ещё нет.</summary>
        public const int DefaultResolutionWidth = 1920;

        /// <summary>Высота окна по умолчанию, если настройки ещё нет.</summary>
        public const int DefaultResolutionHeight = 1080;

        private const string VolumeKey = "settings.volume";
        private const string MouseSensitivityKey = "settings.mouseSensitivity";
        private const string ResolutionWidthKey = "settings.resolution.width";
        private const string ResolutionHeightKey = "settings.resolution.height";
        private const string BindingPrefix = "settings.key.";

        private static readonly Vector2Int[] FallbackResolutions =
        {
            new Vector2Int(1280, 720),
            new Vector2Int(1600, 900),
            new Vector2Int(1920, 1080),
            new Vector2Int(2560, 1440),
            new Vector2Int(3840, 2160)
        };

        private readonly IGameSettingsStorage _storage;
        private readonly Dictionary<HotkeyAction, Key> _bindings = new Dictionary<HotkeyAction, Key>();
        private readonly Dictionary<HotkeyAction, Key> _savedBindings = new Dictionary<HotkeyAction, Key>();
        private readonly List<Vector2Int> _resolutions = new List<Vector2Int>();

        /// <summary>Создаёт настройки с хранилищем и значениями по умолчанию.</summary>
        /// <param name="storage">Хранилище машинных настроек.</param>
        /// <exception cref="ArgumentNullException">Хранилище не задано.</exception>
        public GameSettingsService(IGameSettingsStorage storage)
        {
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
            Volume = DefaultVolume;
            MouseSensitivity = DefaultMouseSensitivity;
            ResolutionWidth = DefaultResolutionWidth;
            ResolutionHeight = DefaultResolutionHeight;
            foreach (var action in HotkeyDefaults.All)
            {
                var key = HotkeyDefaults.For(action);
                _bindings[action] = key;
                _savedBindings[action] = key;
            }

            AddResolution(ResolutionWidth, ResolutionHeight);
        }

        /// <summary>Громкость звука от 0 до 100.</summary>
        public int Volume { get; private set; }

        /// <summary>Чувствительность мыши от 0 до 100.</summary>
        public int MouseSensitivity { get; private set; }

        /// <summary>Ширина окна.</summary>
        public int ResolutionWidth { get; private set; }

        /// <summary>Высота окна.</summary>
        public int ResolutionHeight { get; private set; }

        /// <summary>Доступные разрешения окна: текущее и режимы монитора без повторов.</summary>
        public IReadOnlyList<Vector2Int> Resolutions => _resolutions;

        /// <summary><c>true</c>, если рабочая копия привязок отличается от сохранённой.</summary>
        public bool HasUnsavedBindings
        {
            get
            {
                foreach (var pair in _bindings)
                {
                    if (_savedBindings[pair.Key] != pair.Value)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>Читает настройки из хранилища; пустые значения заменяет умолчаниями.</summary>
        public void Load()
        {
            Volume = Clamp(_storage.GetInt(VolumeKey, DefaultVolume), MinVolume, MaxVolume);
            MouseSensitivity = Clamp(
                _storage.GetInt(MouseSensitivityKey, DefaultMouseSensitivity),
                MinMouseSensitivity,
                MaxMouseSensitivity);

            var width = _storage.GetInt(ResolutionWidthKey, 0);
            var height = _storage.GetInt(ResolutionHeightKey, 0);
            if (width <= 0 || height <= 0)
            {
                width = Screen.width > 0 ? Screen.width : DefaultResolutionWidth;
                height = Screen.height > 0 ? Screen.height : DefaultResolutionHeight;
            }

            ResolutionWidth = width;
            ResolutionHeight = height;
            BuildResolutions();

            foreach (var action in HotkeyDefaults.All)
            {
                var fallback = HotkeyDefaults.For(action);
                var text = _storage.GetString(BindingKey(action), fallback.ToString());
                var key = ParseKey(text, fallback);
                _bindings[action] = key;
                _savedBindings[action] = key;
            }
        }

        /// <summary>Применяет громкость и разрешение окна к игре.</summary>
        public void Apply()
        {
            AudioListener.volume = Volume / (float)MaxVolume;
            ApplyResolution();
        }

        /// <summary>Меняет громкость и запоминает её.</summary>
        /// <param name="volume">Громкость от 0 до 100.</param>
        public void SetVolume(int volume)
        {
            var value = Clamp(volume, MinVolume, MaxVolume);
            if (value == Volume)
            {
                return;
            }

            Volume = value;
            _storage.SetInt(VolumeKey, value);
            AudioListener.volume = value / (float)MaxVolume;
        }

        /// <summary>Меняет чувствительность мыши и запоминает её.</summary>
        /// <param name="sensitivity">Чувствительность от 0 до 100.</param>
        public void SetMouseSensitivity(int sensitivity)
        {
            var value = Clamp(sensitivity, MinMouseSensitivity, MaxMouseSensitivity);
            if (value == MouseSensitivity)
            {
                return;
            }

            MouseSensitivity = value;
            _storage.SetInt(MouseSensitivityKey, value);
        }

        /// <summary>Меняет разрешение окна, запоминает его и применяет сразу.</summary>
        /// <param name="width">Ширина больше нуля.</param>
        /// <param name="height">Высота больше нуля.</param>
        public void SetResolution(int width, int height)
        {
            if (width <= 0 || height <= 0 || (width == ResolutionWidth && height == ResolutionHeight))
            {
                return;
            }

            ResolutionWidth = width;
            ResolutionHeight = height;
            _storage.SetInt(ResolutionWidthKey, width);
            _storage.SetInt(ResolutionHeightKey, height);
            _storage.Save();
            ApplyResolution();
        }

        /// <summary>Возвращает рабочую привязку действия.</summary>
        /// <param name="action">Действие.</param>
        /// <returns>Клавиша рабочей копии.</returns>
        public Key GetBinding(HotkeyAction action) => _bindings[action];

        /// <summary>Возвращает сохранённую привязку действия, по которой работают горячие клавиши.</summary>
        /// <param name="action">Действие.</param>
        /// <returns>Сохранённая клавиша.</returns>
        public Key GetSavedBinding(HotkeyAction action) => _savedBindings[action];

        /// <summary>
        /// Назначает клавишу в рабочей копии. Если клавиша занята другим действием,
        /// привязки меняются местами, поэтому дублей не остаётся.
        /// </summary>
        /// <param name="action">Действие.</param>
        /// <param name="key">Новая клавиша; <c>Key.None</c> оставляет действие без клавиши.</param>
        public void SetBinding(HotkeyAction action, Key key)
        {
            var previous = _bindings[action];
            if (previous == key)
            {
                return;
            }

            if (key != Key.None)
            {
                foreach (var other in HotkeyDefaults.All)
                {
                    if (other != action && _bindings[other] == key)
                    {
                        _bindings[other] = previous;
                        break;
                    }
                }
            }

            _bindings[action] = key;
        }

        /// <summary>Записывает рабочую копию привязок в хранилище — горячие клавиши начинают работать.</summary>
        public void SaveBindings()
        {
            foreach (var pair in _bindings)
            {
                _savedBindings[pair.Key] = pair.Value;
                _storage.SetString(BindingKey(pair.Key), pair.Value.ToString());
            }

            _storage.Save();
        }

        /// <summary>Отбрасывает несохранённые изменения привязок.</summary>
        public void RevertBindings()
        {
            foreach (var pair in _savedBindings)
            {
                _bindings[pair.Key] = pair.Value;
            }
        }

        private static int Clamp(int value, int min, int max) => value < min ? min : value > max ? max : value;

        private static Key ParseKey(string text, Key fallback) =>
            Enum.TryParse<Key>(text, ignoreCase: false, out var key) ? key : fallback;

        private static string BindingKey(HotkeyAction action) => BindingPrefix + action;

        private void BuildResolutions()
        {
            _resolutions.Clear();
            AddResolution(ResolutionWidth, ResolutionHeight);
            foreach (var resolution in Screen.resolutions)
            {
                AddResolution(resolution.width, resolution.height);
            }

            if (_resolutions.Count == 1)
            {
                foreach (var fallback in FallbackResolutions)
                {
                    AddResolution(fallback.x, fallback.y);
                }
            }

            _resolutions.Sort((left, right) =>
            {
                var width = right.x.CompareTo(left.x);
                return width != 0 ? width : right.y.CompareTo(left.y);
            });
        }

        private void AddResolution(int width, int height)
        {
            if (width <= 0 || height <= 0)
            {
                return;
            }

            var value = new Vector2Int(width, height);
            if (!_resolutions.Contains(value))
            {
                _resolutions.Add(value);
            }
        }

        private void ApplyResolution()
        {
#if !UNITY_EDITOR
            if (ResolutionWidth <= 0 || ResolutionHeight <= 0)
            {
                return;
            }

            if (Screen.width == ResolutionWidth &&
                Screen.height == ResolutionHeight &&
                Screen.fullScreenMode == FullScreenMode.Windowed)
            {
                return;
            }

            Screen.SetResolution(ResolutionWidth, ResolutionHeight, FullScreenMode.Windowed);
#endif
        }
    }
}
