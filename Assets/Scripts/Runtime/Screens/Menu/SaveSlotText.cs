#nullable enable
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Runtime.Ui.Core;

namespace AnomalyPath.Runtime.Screens.Menu
{
    /// <summary>Подписи сохранений для списков загрузки и сохранения: уровень, деньги и день мира.</summary>
    internal static class SaveSlotText
    {
        /// <summary>Деньги в виде «1000 руб».</summary>
        /// <param name="texts">Каталог текстов.</param>
        /// <param name="money">Сумма.</param>
        /// <returns>Подпись.</returns>
        public static string Money(IGameTexts texts, int money) =>
            UiText.Format(texts, UiTextKey.Common_Money, money);
    }
}
