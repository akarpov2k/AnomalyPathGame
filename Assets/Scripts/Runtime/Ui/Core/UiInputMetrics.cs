#nullable enable
namespace AnomalyPath.Runtime.Ui.Core
{
    /// <summary>
    /// Пороги ввода интерфейса. Это не игровые правила, а поведение управления:
    /// на телефоне удержание заменяет правую кнопку мыши.
    /// </summary>
    public static class UiInputMetrics
    {
        /// <summary>Сколько секунд держать палец на предмете, чтобы открылось контекстное меню.</summary>
        public const float LongPressSeconds = 0.45f;
    }
}
