#nullable enable
using System.Drawing;

namespace AnomalyPath.Setup
{
    /// <summary>
    /// Палитра, шрифты и общие метрики установщика AnomalyPath: цвета повторяют
    /// постер (зелёное свечение аномалии, тёмный металл), шрифт — системный Segoe UI.
    /// </summary>
    internal static class SetupTheme
    {
        /// <summary>Основной акцент — цвет аномалии на постере.</summary>
        public static readonly Color Accent = Color.FromArgb(126, 216, 118);

        /// <summary>Тёмный вариант акцента для заливки кнопки.</summary>
        public static readonly Color AccentDark = Color.FromArgb(44, 92, 46);

        /// <summary>Цвет текста ошибки.</summary>
        public static readonly Color Danger = Color.FromArgb(232, 122, 108);

        /// <summary>Обычный текст.</summary>
        public static readonly Color Text = Color.FromArgb(238, 242, 234);

        /// <summary>Приглушённый текст: подписи, пути, имена файлов.</summary>
        public static readonly Color TextDim = Color.FromArgb(170, 182, 168);

        /// <summary>Фон составных кнопок.</summary>
        public static readonly Color Panel = Color.FromArgb(28, 32, 29);

        /// <summary>Фон составной кнопки под курсором.</summary>
        public static readonly Color PanelHover = Color.FromArgb(46, 52, 47);

        /// <summary>Рамка составных кнопок и поля ввода.</summary>
        public static readonly Color Border = Color.FromArgb(86, 100, 86);

        /// <summary>Фон полосы прогресса.</summary>
        public static readonly Color Track = Color.FromArgb(38, 44, 39);

        /// <summary>Семейство шрифта интерфейса.</summary>
        public const string FontFamily = "Segoe UI";

        /// <summary>Создаёт шрифт интерфейса заданного размера в пунктах.</summary>
        /// <param name="points">Размер в пунктах.</param>
        /// <param name="style">Начертание.</param>
        /// <returns>Новый шрифт; владелец — вызывающий код.</returns>
        public static Font Font(float points, FontStyle style = FontStyle.Regular)
        {
            return new Font(FontFamily, points, style, GraphicsUnit.Point);
        }
    }
}
