#nullable enable
using System.Collections.Generic;
using AnomalyPath.Core.Combat;
using AnomalyPath.Core.Inventory;
using AnomalyPath.Core.World;
using EquipmentState = AnomalyPath.Core.Inventory.Equipment;

namespace AnomalyPath.Core.Abstractions
{
    /// <summary>Бой: старт, действия и смена хода.</summary>
    public interface ICombatService
    {
        /// <summary>Начинает бой.</summary>
        /// <param name="playerActor">Актёр игрока.</param>
        /// <param name="playerEquipment">Экипировка игрока.</param>
        /// <param name="enemy">Противник.</param>
        /// <param name="scenario">Сценарий боя; <c>null</c> — обычный бой, игрок ходит первым.</param>
        /// <param name="playerModifiers">Бонусы талантов игрока; <c>null</c> — без бонусов.</param>
        /// <param name="playerHealthBonus">Прибавка к максимуму здоровья за уровни игрока; 0 — без неё.</param>
        /// <param name="playerInjuries">Активные травмы игрока; <c>null</c> — без травм.</param>
        /// <param name="playerArtifacts">Бонусы надетых артефактов; <c>null</c> — без бонусов.</param>
        /// <param name="phase">Фаза суток на момент начала боя; по умолчанию день.</param>
        /// <param name="nightAccuracyPenalty">
        /// Влияние ночи на точность, 0..1: доля меткости, вычитаемая ночью; по умолчанию 0.
        /// </param>
        /// <param name="context">Контекст боя вне локации; <c>null</c> — обычный бой на локации.</param>
        /// <param name="playerApCostBonus">
        /// Прибавка к цене любого действия игрока в ОД (голод); 0 — без прибавки.
        /// </param>
        /// <param name="playerTurnDamage">
        /// Урон здоровью игрока в начале каждого его хода (жажда); 0 — без урона.
        /// </param>
        /// <param name="enemyWeaponSeed">
        /// Seed выбора оружия из пула <c>attack.weaponIds</c>; 0 — первый вариант пула.
        /// </param>
        /// <returns>Состояние боя.</returns>
        CombatState Start(
            ActorTemplate playerActor,
            EquipmentState playerEquipment,
            ActorTemplate enemy,
            CombatScenario? scenario = null,
            AnomalyPath.Core.Talents.TalentModifiers? playerModifiers = null,
            int playerHealthBonus = 0,
            IReadOnlyList<AnomalyPath.Core.Player.InjuryState>? playerInjuries = null,
            AnomalyPath.Core.Artifacts.ArtifactModifiers? playerArtifacts = null,
            TimePhase phase = TimePhase.Day,
            double nightAccuracyPenalty = 0,
            CombatEncounterContext? context = null,
            int playerApCostBonus = 0,
            int playerTurnDamage = 0,
            int enemyWeaponSeed = 0);

        /// <summary>Список действий текущего участника: скиллы владельца.</summary>
        /// <param name="state">Состояние боя.</param>
        /// <param name="weapon">Оружие текущего участника; <c>null</c> — скиллы самого актёра (мутант без оружия).</param>
        /// <returns>Действия в порядке объявления скиллов.</returns>
        IReadOnlyList<CombatAction> GetActions(CombatState state, ItemInstance? weapon);

        /// <summary>Применяет скилл оружия или скилл актёра без оружия текущим участником.</summary>
        /// <param name="state">Состояние боя.</param>
        /// <param name="weapon">Оружие; <c>null</c> — скилл без оружия (у мутанта).</param>
        /// <param name="skillId">Идентификатор скилла из данных.</param>
        /// <param name="result">Выстрелы, урон и смерть цели.</param>
        /// <returns><c>true</c>, если скилл выполнен.</returns>
        bool TryUseSkill(CombatState state, ItemInstance? weapon, string skillId, out CombatSkillResult result);

        /// <summary>Атакует текущим ходящим участником дефолтным скиллом оружия.</summary>
        /// <param name="state">Состояние боя.</param>
        /// <param name="weapon">Оружие атакующего.</param>
        /// <param name="result">Попадание, урон и смерть цели.</param>
        /// <returns><c>true</c>, если атака выполнена.</returns>
        bool TryAttack(CombatState state, ItemInstance weapon, out CombatAttackResult result);

        /// <summary>Противник атакует своим дефолтным скиллом: оружием или без оружия.</summary>
        /// <param name="state">Состояние боя.</param>
        /// <param name="result">Попадание, урон и смерть цели.</param>
        /// <returns><c>true</c>, если атака выполнена; <c>false</c>, если она невозможна.</returns>
        bool TryEnemyAttack(CombatState state, out CombatAttackResult result);

        /// <summary>
        /// Побег: передаёт ход противнику, тот делает стандартную атаку, если может,
        /// и при выживании игрока закрывает бой без победы.
        /// </summary>
        /// <param name="state">Состояние боя.</param>
        /// <param name="result">Исход побега, атака противника и здоровье игрока.</param>
        /// <returns><c>true</c>, если побег выполнен; <c>false</c>, если он невозможен.</returns>
        bool TryFlee(CombatState state, out FleeResult result);

        /// <summary>Перезаряжает оружие из инвентаря, тратя ОД.</summary>
        /// <param name="state">Состояние боя.</param>
        /// <param name="weapon">Оружие.</param>
        /// <param name="inventory">Инвентарь с патронами.</param>
        /// <returns><c>true</c>, если перезарядка выполнена.</returns>
        bool TryReload(CombatState state, ItemInstance weapon, GridContainer inventory);

        /// <summary>Проверяет, можно ли применить медицину прямо сейчас.</summary>
        /// <param name="state">Состояние боя.</param>
        /// <param name="item">Медицина из слота быстрого доступа.</param>
        /// <returns>
        /// <c>true</c>, если сейчас ход игрока, предмет — боевая медицина с остатком применений,
        /// лежит в быстром слоте и хватает ОД.
        /// </returns>
        bool CanUseMedicine(CombatState state, ItemInstance item);

        /// <summary>
        /// Применяет медицину в бою: тратит ОД, лечит здоровье, снимает травмы, восстанавливает ОД
        /// и накладывает временные модификаторы. Последнее применение убирает предмет из слота.
        /// </summary>
        /// <param name="state">Состояние боя.</param>
        /// <param name="item">Медицина из слота быстрого доступа.</param>
        /// <returns>Итог применения; при неудаче состояние не меняется.</returns>
        ConsumableUseResult TryUseMedicine(CombatState state, ItemInstance item);

        /// <summary>
        /// Противник применяет свою аптечку: лечит за ОД и тратит заряд.
        /// ИИ выбирает это действие по порогу здоровья (<see cref="CombatSettingsTemplate.MedkitHpThreshold"/>).
        /// </summary>
        /// <param name="state">Состояние боя.</param>
        /// <returns>Итог применения; при неудаче состояние не меняется.</returns>
        ConsumableUseResult TryEnemyUseMedkit(CombatState state);

        /// <summary>Оружие игрока в активном слоте боя.</summary>
        /// <param name="state">Состояние боя.</param>
        /// <returns>Экземпляр оружия или <c>null</c>, если активный слот пуст.</returns>
        ItemInstance? PlayerWeapon(CombatState state);

        /// <summary>Меняет активное оружие игрока между основным и запасным слотом за ОД.</summary>
        /// <param name="state">Состояние боя.</param>
        /// <returns><c>true</c>, если оружие сменено.</returns>
        bool TrySwapWeapon(CombatState state);

        /// <summary>
        /// Достаёт оружие из быстрого слота: оно встаёт в активный слот, прежнее активное оружие —
        /// в этот быстрый слот. Стоит столько же ОД, сколько обычная смена оружия.
        /// </summary>
        /// <param name="state">Состояние боя.</param>
        /// <param name="quickIndex">Индекс быстрого слота.</param>
        /// <returns><c>true</c>, если оружие сменено.</returns>
        bool TryUseQuickWeapon(CombatState state, int quickIndex);

        /// <summary>Можно ли перезарядить оружие противника из его бесконечного запаса.</summary>
        /// <param name="state">Состояние боя.</param>
        /// <returns><c>true</c>, если магазин не полон и хватает ОД.</returns>
        bool CanEnemyReload(CombatState state);

        /// <summary>Перезаряжает оружие противника из бесконечного запаса за ОД.</summary>
        /// <param name="state">Состояние боя.</param>
        /// <returns><c>true</c>, если магазин заполнен.</returns>
        bool TryEnemyReload(CombatState state);

        /// <summary>Считает шанс попадания текущего участника по противнику.</summary>
        /// <param name="state">Состояние боя.</param>
        /// <param name="weapon">Оружие атакующего.</param>
        /// <returns>Шанс попадания 0..1.</returns>
        double HitChance(CombatState state, ItemInstance weapon);

        /// <summary>
        /// Предпросмотр скилла текущего участника без бросков: доступность, цена, шанс,
        /// ожидаемый урон и вероятность добить цель.
        /// </summary>
        /// <param name="state">Состояние боя.</param>
        /// <param name="weapon">Оружие; <c>null</c> — скилл без оружия.</param>
        /// <param name="skillId">Идентификатор скилла из данных.</param>
        /// <returns>Предпросмотр; при недоступности — причина и цена.</returns>
        CombatSkillPreview PreviewSkill(CombatState state, ItemInstance? weapon, string skillId);

        /// <summary>Завершает ход и передаёт его противнику.</summary>
        /// <param name="state">Состояние боя.</param>
        void EndTurn(CombatState state);
    }
}
