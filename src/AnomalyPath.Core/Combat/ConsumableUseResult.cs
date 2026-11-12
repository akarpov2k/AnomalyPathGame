#nullable enable
using System;
using System.Collections.Generic;

namespace AnomalyPath.Core.Combat
{
    /// <summary>
    /// Итог применения расходника: что вылечено, сколько ОД, еды и воды восполнено
    /// и остались ли применения. Неудачное применение возвращает <see cref="Failed"/>
    /// и не меняет состояние.
    /// </summary>
    public sealed class ConsumableUseResult
    {
        private static readonly IReadOnlyList<string> NoInjuries = Array.Empty<string>();

        /// <summary>Создаёт итог применения.</summary>
        /// <param name="succeeded">Применение состоялось.</param>
        /// <param name="healthRestored">Сколько здоровья восстановлено.</param>
        /// <param name="apRestored">Сколько очков действия восстановлено.</param>
        /// <param name="foodRestored">Сколько еды восполнено.</param>
        /// <param name="waterRestored">Сколько воды восполнено.</param>
        /// <param name="injuriesHealed">Id снятых травм.</param>
        /// <param name="chargesLeft">Сколько применений осталось у предмета.</param>
        /// <param name="itemConsumed">Предмет израсходован полностью.</param>
        /// <exception cref="ArgumentNullException">Список травм не передан.</exception>
        public ConsumableUseResult(
            bool succeeded,
            int healthRestored,
            int apRestored,
            int foodRestored,
            int waterRestored,
            IReadOnlyList<string> injuriesHealed,
            int chargesLeft,
            bool itemConsumed)
        {
            Succeeded = succeeded;
            HealthRestored = healthRestored;
            ApRestored = apRestored;
            FoodRestored = foodRestored;
            WaterRestored = waterRestored;
            InjuriesHealed = injuriesHealed ?? throw new ArgumentNullException(nameof(injuriesHealed));
            ChargesLeft = chargesLeft;
            ItemConsumed = itemConsumed;
        }

        /// <summary>Неудачное применение: ничего не изменилось.</summary>
        public static ConsumableUseResult Failed { get; } =
            new ConsumableUseResult(false, 0, 0, 0, 0, NoInjuries, 0, false);

        /// <summary><c>true</c>, если применение состоялось.</summary>
        public bool Succeeded { get; }

        /// <summary>Сколько здоровья восстановлено.</summary>
        public int HealthRestored { get; }

        /// <summary>Сколько очков действия восстановлено.</summary>
        public int ApRestored { get; }

        /// <summary>Сколько еды восполнено.</summary>
        public int FoodRestored { get; }

        /// <summary>Сколько воды восполнено.</summary>
        public int WaterRestored { get; }

        /// <summary>Id травм, которые снял расходник.</summary>
        public IReadOnlyList<string> InjuriesHealed { get; }

        /// <summary>Сколько применений осталось у предмета.</summary>
        public int ChargesLeft { get; }

        /// <summary><c>true</c>, если предмет израсходован полностью и убран.</summary>
        public bool ItemConsumed { get; }
    }
}
