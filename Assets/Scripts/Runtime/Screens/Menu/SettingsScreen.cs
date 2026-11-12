#nullable enable
using System.Globalization;
using AnomalyPath.Runtime.Ui.Core;
using UnityEngine;
using UnityEngine.UI;

namespace AnomalyPath.Runtime.Screens.Menu
{
    /// <summary>
    /// Экран настроек по прототипу: разрешение окна, громкость, переход к настройке клавиш
    /// и чувствительность мыши. Изменения применяются и запоминаются сразу; разметка,
    /// ползунки, список разрешений и кнопки приходят из префаба.
    /// </summary>
    public sealed class SettingsScreen : PrefabScreenBase<IScreenHost>
    {
        [Tooltip("Выбор разрешения окна")]
        [SerializeField]
        private ResolutionDropdown resolution = null!;

        [Tooltip("Ползунок громкости звука")]
        [SerializeField]
        private Slider volume = null!;

        [Tooltip("Текущая громкость справа от ползунка")]
        [SerializeField]
        private Text volumeValue = null!;

        [Tooltip("Ползунок чувствительности мыши")]
        [SerializeField]
        private Slider sensitivity = null!;

        [Tooltip("Текущая чувствительность справа от ползунка")]
        [SerializeField]
        private Text sensitivityValue = null!;

        [Tooltip("Кнопка «Настройка клавиш»")]
        [SerializeField]
        private Button keys = null!;

        [Tooltip("Кнопка «Назад»")]
        [SerializeField]
        private Button back = null!;

        private bool _suppress;

        /// <inheritdoc />
        protected override void OnBound()
        {
            volume.onValueChanged.AddListener(OnVolumeChanged);
            sensitivity.onValueChanged.AddListener(OnSensitivityChanged);
            keys.onClick.AddListener(Host.ShowKeyBindings);
            back.onClick.AddListener(Host.ShowMainMenu);
        }

        /// <summary>Показывает настройки и заполняет их текущими значениями.</summary>
        public void Show()
        {
            Host.Router.Show(Panel);
            Refresh();
        }

        /// <summary>Esc закрывает выпадающий список, иначе возвращает в стартовое меню.</summary>
        /// <returns><c>true</c>, если Esc обработан.</returns>
        public override bool HandleEscape()
        {
            if (resolution.IsOpen)
            {
                resolution.Close();
                return true;
            }

            Host.ShowMainMenu();
            return true;
        }

        private void Refresh()
        {
            _suppress = true;
            var settings = Host.Settings;
            resolution.Bind(
                settings.ResolutionWidth,
                settings.ResolutionHeight,
                settings.Resolutions,
                OnResolutionPicked);
            volume.SetValueWithoutNotify(settings.Volume);
            volumeValue.text = settings.Volume.ToString(CultureInfo.InvariantCulture);
            sensitivity.SetValueWithoutNotify(settings.MouseSensitivity);
            sensitivityValue.text = settings.MouseSensitivity.ToString(CultureInfo.InvariantCulture);
            _suppress = false;
        }

        private void OnVolumeChanged(float value)
        {
            if (_suppress)
            {
                return;
            }

            var rounded = Mathf.RoundToInt(value);
            Host.Settings.SetVolume(rounded);
            volumeValue.text = rounded.ToString(CultureInfo.InvariantCulture);
        }

        private void OnSensitivityChanged(float value)
        {
            if (_suppress)
            {
                return;
            }

            var rounded = Mathf.RoundToInt(value);
            Host.Settings.SetMouseSensitivity(rounded);
            sensitivityValue.text = rounded.ToString(CultureInfo.InvariantCulture);
        }

        private void OnResolutionPicked(int width, int height) => Host.Settings.SetResolution(width, height);
    }
}
