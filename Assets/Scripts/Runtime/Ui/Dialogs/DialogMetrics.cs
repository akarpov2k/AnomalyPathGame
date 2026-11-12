#nullable enable
namespace AnomalyPath.Runtime.Ui.Dialogs
{
    /// <summary>
    /// Размеры окна диалога по прототипу `.prototypes/диалог.penpot`:
    /// меню из полос 711×64 в окне 904 и окно разговора с репликой сверху
    /// и прокручиваемым списком фраз снизу.
    /// </summary>
    public static class DialogMetrics
    {
        /// <summary>Ширина окна диалога в единицах канваса.</summary>
        public const float WindowWidth = 904f;

        /// <summary>Высота окна разговора: реплика собеседника и ровно три видимые фразы.</summary>
        public const float TalkHeight = 442f;

        /// <summary>Боковой и верхний отступ окна.</summary>
        public const float WindowPadding = 36f;

        /// <summary>Ширина полосы меню.</summary>
        public const float MenuButtonWidth = 711f;

        /// <summary>Высота полосы меню.</summary>
        public const float MenuButtonHeight = 64f;

        /// <summary>Зазор между полосами меню.</summary>
        public const float MenuButtonGap = 14f;

        /// <summary>Верх области ответа собеседника.</summary>
        public const float ReplyTop = 24f;

        /// <summary>Высота области ответа собеседника.</summary>
        public const float ReplyHeight = 150f;

        /// <summary>Внутренний отступ области ответа.</summary>
        public const float ReplyPadding = 16f;

        /// <summary>Высота строки с именем собеседника.</summary>
        public const float SpeakerHeight = 30f;

        /// <summary>Верх текста ответа внутри области.</summary>
        public const float ReplyTextTop = 18f;

        /// <summary>Высота текста ответа.</summary>
        public const float ReplyTextHeight = 96f;

        /// <summary>Верх области фраз.</summary>
        public const float PhrasesTop = 190f;

        /// <summary>Высота видимой области фраз: ровно три кнопки с отступами и зазорами.</summary>
        public const float PhrasesHeight = 228f;

        /// <summary>Высота кнопки-фразы.</summary>
        public const float PhraseHeight = 64f;

        /// <summary>Зазор между кнопками-фразами.</summary>
        public const float PhraseGap = 10f;

        /// <summary>Отступ кнопок-фраз внутри прокрутки.</summary>
        public const float PhrasePadding = 8f;

        /// <summary>Сколько фраз видно одновременно; остальные — прокруткой.</summary>
        public const int VisiblePhrases = 3;

        /// <summary>Ширина полосы прокрутки фраз.</summary>
        public const float ScrollbarWidth = 12f;

        /// <summary>Кегль полос меню.</summary>
        public const int MenuFontSize = 26;

        /// <summary>Кегль имени собеседника.</summary>
        public const int SpeakerFontSize = 24;

        /// <summary>Кегль ответа собеседника.</summary>
        public const int ReplyFontSize = 20;

        /// <summary>Кегль фразы игрока.</summary>
        public const int PhraseFontSize = 22;

        /// <summary>
        /// Высота меню для показанных полос: отступы сверху и снизу, полосы и зазоры между
        /// ними. У меню с одной полосой окно ниже, чем у меню торговца с тремя.
        /// </summary>
        /// <param name="buttonCount">Сколько полос показано.</param>
        /// <returns>Высота окна меню в единицах канваса.</returns>
        public static float MenuHeightFor(int buttonCount)
        {
            var count = buttonCount < 0 ? 0 : buttonCount;
            var gaps = count > 1 ? count - 1 : 0;
            return (2f * WindowPadding) +
                   (count * MenuButtonHeight) +
                   (gaps * MenuButtonGap);
        }
    }
}
