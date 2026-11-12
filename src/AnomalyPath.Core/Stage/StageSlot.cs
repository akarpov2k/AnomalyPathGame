#nullable enable
namespace AnomalyPath.Core.Stage
{
    /// <summary>
    /// Слот сцены локации: место, на которое встаёт фигура. Мест три — левый и правый
    /// объекты и противник по центру; слот определяет, какую точку земли берёт экран.
    /// </summary>
    public enum StageSlot
    {
        /// <summary>Левый слот: первый объект пула.</summary>
        Left,

        /// <summary>Правый слот: второй объект пула или тайник.</summary>
        Right,

        /// <summary>Центр сцены: противник.</summary>
        Center
    }
}
