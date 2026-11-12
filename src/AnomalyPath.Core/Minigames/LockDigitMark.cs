#nullable enable
namespace AnomalyPath.Core.Minigames
{
    /// <summary>Подсказка по одной цифре введённого кода: на месте, есть в коде или отсутствует.</summary>
    public enum LockDigitMark
    {
        /// <summary>Цифры нет в коде; подсветки нет.</summary>
        None = 0,

        /// <summary>Цифра есть в коде, но стоит на другом месте; жёлтая подсветка.</summary>
        WrongPlace = 1,

        /// <summary>Цифра стоит на своём месте; зелёная подсветка.</summary>
        CorrectPlace = 2,
    }
}
