#nullable enable
namespace AnomalyPath.Runtime.Screens.Menu
{
    /// <summary>
    /// Размеры экрана настроек и экрана настройки клавиш для сборщика префабов.
    /// Это стартовые значения разметки: в игре геометрия берётся из префаба, а не отсюда.
    /// </summary>
    public static class SettingsMetrics
    {
        /// <summary>Высота строки настройки.</summary>
        public const float RowHeight = 112f;

        /// <summary>Зазор между строками настроек.</summary>
        public const float RowGap = 24f;

        /// <summary>Боковой отступ списка настроек.</summary>
        public const float ListInset = 24f;

        /// <summary>Отступ списка настроек сверху.</summary>
        public const float ListTop = 24f;

        /// <summary>Отступ списка настроек снизу: над кнопкой «Назад».</summary>
        public const float ListBottom = 120f;

        /// <summary>Кегль подписи настройки.</summary>
        public const int LabelSize = 30;

        /// <summary>Кегль текущего значения настройки.</summary>
        public const int ValueSize = 26;

        /// <summary>Ширина подписи текущего значения.</summary>
        public const float ValueWidth = 90f;

        /// <summary>Ширина элемента управления строки: ползунка или выпадающего списка.</summary>
        public const float ControlWidth = 380f;

        /// <summary>Высота ползунка.</summary>
        public const float SliderHeight = 40f;

        /// <summary>Высота дорожки ползунка.</summary>
        public const float SliderTrackHeight = 14f;

        /// <summary>Ширина ползунка.</summary>
        public const float SliderHandleWidth = 28f;

        /// <summary>Высота кнопки выбора разрешения.</summary>
        public const float DropdownHeight = 64f;

        /// <summary>Высота строки выпадающего списка разрешений.</summary>
        public const float DropdownRowHeight = 52f;

        /// <summary>Зазор между строками выпадающего списка.</summary>
        public const float DropdownRowGap = 4f;

        /// <summary>Отступ строк выпадающего списка от его краёв.</summary>
        public const float DropdownInset = 4f;

        /// <summary>Высота кнопки «Настройка клавиш».</summary>
        public const float KeysButtonHeight = 96f;

        /// <summary>Ширина кнопки «Настройка клавиш»: кадр кнопки держит пропорцию 2:1.</summary>
        public const float KeysButtonWidth = 192f;

        /// <summary>Кегль подписи кнопки «Настройка клавиш».</summary>
        public const int KeysButtonFontSize = 20;

        /// <summary>Отступ заголовка раздела клавиш сверху.</summary>
        public const float KeysTitleTop = 28f;

        /// <summary>Высота заголовка раздела клавиш.</summary>
        public const float KeysTitleHeight = 48f;

        /// <summary>Кегль заголовка раздела клавиш.</summary>
        public const int KeysTitleSize = 32;

        /// <summary>Отступ списка клавиш сверху: под заголовком раздела.</summary>
        public const float KeysListTop = 96f;

        /// <summary>Отступ списка клавиш снизу: над нижней полосой кнопок.</summary>
        public const float KeysListBottom = 120f;

        /// <summary>Высота строки привязки клавиши.</summary>
        public const float KeyRowHeight = 88f;

        /// <summary>Зазор между строками привязок.</summary>
        public const float KeyRowGap = 12f;

        /// <summary>Ширина колонки названия действия в строке привязки.</summary>
        public const float KeyNameWidth = 320f;

        /// <summary>Ширина кнопки с текущей клавишей.</summary>
        public const float KeyButtonWidth = 240f;

        /// <summary>Высота кнопки с текущей клавишей.</summary>
        public const float KeyButtonHeight = 60f;

        /// <summary>Кегль названия действия в строке привязки.</summary>
        public const int KeyRowLabelSize = 28;

        /// <summary>Кегль клавиши в строке привязки.</summary>
        public const int KeyRowKeySize = 24;

        /// <summary>Ширина отметки несохранённого изменения.</summary>
        public const float StarWidth = 28f;

        /// <summary>Кегль отметки несохранённого изменения.</summary>
        public const int StarSize = 30;

        /// <summary>Ширина кнопки «Сохранить».</summary>
        public const float SaveWidth = 160f;

        /// <summary>Высота кнопки «Сохранить»: кадр кнопки держит пропорцию 2:1.</summary>
        public const float SaveHeight = 80f;

        /// <summary>Отступ нижней полосы кнопок от нижнего края.</summary>
        public const float BottomInset = 18f;

        /// <summary>Зазор между «Сохранить» и «Назад».</summary>
        public const float BottomGap = 16f;

        /// <summary>Ширина полосы прокрутки списка.</summary>
        public const float ScrollbarWidth = 12f;

        /// <summary>Ширина центрированной колонки экрана настроек.</summary>
        public const float ColumnWidth = 1000f;
    }
}
