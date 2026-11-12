#nullable enable
namespace AnomalyPath.Core.Player
{
    /// <summary>Часть класса <see cref="PlayerState"/>: счётчик смертей за прохождение.</summary>
    public sealed partial class PlayerState
    {
        /// <summary>
        /// Сколько раз игрок умирал за прохождение. Счётчик входит в сейв:
        /// это накопленное состояние, а не расчёт из текущих данных.
        /// </summary>
        public int Deaths { get; private set; }

        /// <summary>Учитывает смерть игрока: счётчик увеличивается на единицу.</summary>
        public void AddDeath()
        {
            if (Deaths < int.MaxValue)
            {
                Deaths++;
            }
        }
    }
}
