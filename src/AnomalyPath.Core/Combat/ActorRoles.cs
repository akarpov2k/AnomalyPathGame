#nullable enable
namespace AnomalyPath.Core.Combat
{
    /// <summary>Коды ролей актёра из схемы данных: <c>roles</c> в файле актёра.</summary>
    public static class ActorRoles
    {
        /// <summary>Роль торговца: обязательна для <c>type = trader</c>.</summary>
        public const string Trader = "trader";

        /// <summary>Роль мутанта: цель квестов на убийство.</summary>
        public const string Mutant = "mutant";

        /// <summary>Роль человека: цель квестов на убийство NPC.</summary>
        public const string Human = "human";
    }
}
