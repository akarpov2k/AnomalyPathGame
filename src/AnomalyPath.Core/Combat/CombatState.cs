#nullable enable
using AnomalyPath.Core.Equipment;
using AnomalyPath.Core.Inventory;
using AnomalyPath.Core.World;

namespace AnomalyPath.Core.Combat
{
    /// <summary>Состояние боя: участники, чей ход и номер раунда.</summary>
    public sealed class CombatState
    {
        internal CombatState(
            Combatant player,
            Combatant enemy,
            CombatScenario scenario,
            ItemInstance? enemyWeapon = null,
            int enemyMedkitCharges = 0,
            TimePhase phase = TimePhase.Day,
            double nightAccuracyPenalty = 0,
            CombatEncounterContext? context = null,
            int playerApCostBonus = 0,
            int playerTurnDamage = 0)
        {
            if (playerApCostBonus < 0)
            {
                throw new System.ArgumentOutOfRangeException(nameof(playerApCostBonus));
            }

            if (playerTurnDamage < 0)
            {
                throw new System.ArgumentOutOfRangeException(nameof(playerTurnDamage));
            }

            Player = player;
            Enemy = enemy;
            Scenario = scenario;
            EnemyWeapon = enemyWeapon;
            EnemyMedkitCharges = enemyMedkitCharges;
            Phase = phase;
            NightAccuracyPenalty = nightAccuracyPenalty;
            Context = context;
            PlayerApCostBonus = playerApCostBonus;
            PlayerTurnDamage = playerTurnDamage;
            Current = scenario.First == CombatStartSide.Player ? player : enemy;
        }

        /// <summary>Игрок.</summary>
        public Combatant Player { get; }

        /// <summary>Противник.</summary>
        public Combatant Enemy { get; }

        /// <summary>Оружие противника; <c>null</c> у естественной атаки.</summary>
        public ItemInstance? EnemyWeapon { get; }

        /// <summary>
        /// Остаток применений аптечки противника: ИИ тратит их, когда здоровье ниже
        /// порога <see cref="CombatSettingsTemplate.MedkitHpThreshold"/>.
        /// </summary>
        public int EnemyMedkitCharges { get; internal set; }

        /// <summary>Активный слот оружия игрока в бою: основное или запасное.</summary>
        public EquipmentSlot PlayerWeaponSlot { get; internal set; } = EquipmentSlot.Main;

        /// <summary>Сценарий боя: обычный, засада или сюжетный.</summary>
        public CombatScenario Scenario { get; }

        /// <summary>
        /// Фаза суток на момент начала боя. Бой не двигает часы мира, поэтому штраф ночи
        /// фиксируется на весь бой.
        /// </summary>
        public TimePhase Phase { get; }

        /// <summary>
        /// Влияние ночи на точность, выбранное при старте игры: доля базовой меткости,
        /// вычитаемая ночью у игрока и у противника, 0..1.
        /// </summary>
        public double NightAccuracyPenalty { get; }

        /// <summary>Контекст боя вне локации; <c>null</c> — обычный бой на локации.</summary>
        public CombatEncounterContext? Context { get; }

        /// <summary>
        /// Прибавка к цене любого действия игрока в ОД, зафиксированная на бой:
        /// голод даёт +1 ко всем тратам ОД, враг её не получает.
        /// </summary>
        public int PlayerApCostBonus { get; }

        /// <summary>Урон здоровью игрока в начале каждого его хода; 0 — жажды нет.</summary>
        public int PlayerTurnDamage { get; }

        /// <summary>Кто ходит сейчас.</summary>
        public Combatant Current { get; internal set; }

        /// <summary>Номер раунда, начиная с 1.</summary>
        public int Round { get; internal set; } = 1;

        /// <summary>Противник текущего участника.</summary>
        public Combatant Opponent => ReferenceEquals(Current, Player) ? Enemy : Player;

        /// <summary><c>true</c>, если бой завершён смертью одного из участников.</summary>
        public bool IsFinished => Player.IsDead || Enemy.IsDead;

        /// <summary>Копия состояния боя для расчёта плана ИИ: бой не меняется.</summary>
        /// <returns>Независимое состояние с копиями участников и оружия противника.</returns>
        internal CombatState Clone()
        {
            var player = Player.Clone();
            var enemy = Enemy.Clone();
            var state = new CombatState(
                player,
                enemy,
                Scenario,
                EnemyWeapon?.Clone(),
                phase: Phase,
                nightAccuracyPenalty: NightAccuracyPenalty,
                context: Context,
                playerApCostBonus: PlayerApCostBonus,
                playerTurnDamage: PlayerTurnDamage);
            state.Current = ReferenceEquals(Current, Player) ? state.Player : state.Enemy;
            state.Round = Round;
            state.PlayerWeaponSlot = PlayerWeaponSlot;
            state.EnemyMedkitCharges = EnemyMedkitCharges;
            return state;
        }
    }
}
