#nullable enable
namespace AnomalyPath.Core.Localization
{
    /// <summary>Часть класса <see cref="CoreErrors"/>: ошибки формул и боя.</summary>
    public static partial class CoreErrors
    {
        /// <summary>Пустое выражение формулы.</summary>
        public const string FormulaEmpty = "error.formula_empty";

        /// <summary>Синтаксическая ошибка в формуле.</summary>
        public const string FormulaSyntaxError = "error.formula_syntax";

        /// <summary>Формула использует неизвестное имя.</summary>
        public const string FormulaUnknownVariable = "error.formula_unknown_variable";

        /// <summary>Формула использует неизвестную функцию.</summary>
        public const string FormulaUnknownFunction = "error.formula_unknown_function";

        /// <summary>Формула должна задавать ровно одно представление.</summary>
        public const string FormulaRepresentation = "error.formula_representation";

        /// <summary>Неизвестный тип результата формулы.</summary>
        public const string FormulaResult = "error.formula_result";

        /// <summary>Ссылка на формулу не найдена.</summary>
        public const string FormulaNotFound = "error.formula_not_found";

        /// <summary>Запись настроек боя должна быть ровно одна.</summary>
        public const string CombatSettingsSingle = "error.combat_settings_single";

        /// <summary>У актёра нет боевого блока, а он обязателен.</summary>
        public const string ActorWithoutCombat = "error.actor_without_combat";

        /// <summary>Неизвестный тип актёра.</summary>
        public const string UnknownActorType = "error.unknown_actor_type";

        /// <summary>Неизвестный вид урона в поле kind.</summary>
        public const string UnknownDamageTypeKind = "error.unknown_damage_type_kind";

        /// <summary>Неизвестный режим восстановления ОД.</summary>
        public const string UnknownApRestore = "error.unknown_ap_restore";

        /// <summary>Блок атаки указан не у врага.</summary>
        public const string AttackOnNonEnemy = "error.attack_on_non_enemy";

        /// <summary>У врага нет блока атаки.</summary>
        public const string ActorWithoutAttack = "error.actor_without_attack";

        /// <summary>В атаке смешаны оружие и естественное оружие.</summary>
        public const string AttackMixedFields = "error.attack_mixed_fields";

        /// <summary>Скилл не найден в репозитории.</summary>
        public const string SkillNotFound = "error.skill_not_found";

        /// <summary>Неизвестный тип скилла.</summary>
        public const string UnknownSkillKind = "error.unknown_skill_kind";

        /// <summary>У скилла нет эффектов.</summary>
        public const string SkillNoEffects = "error.skill_no_effects";

        /// <summary>Неизвестный тип эффекта скилла.</summary>
        public const string SkillUnknownEffect = "error.skill_unknown_effect";

        /// <summary>Неизвестная цель эффекта статуса.</summary>
        public const string UnknownSkillTarget = "error.unknown_skill_target";

        /// <summary>У статуса скилла нет ни одного модификатора.</summary>
        public const string SkillStatusNoModifier = "error.skill_status_no_modifier";

        /// <summary>У атакующего скилла нет эффекта урона.</summary>
        public const string SkillWithoutDamage = "error.skill_without_damage";

        /// <summary>Дефолтный скилл оружия не входит в его список скиллов.</summary>
        public const string WeaponDefaultSkillMissing = "error.weapon_default_skill_missing";

        /// <summary>У мутанта задано оружие в блоке атаки.</summary>
        public const string MutantWithWeapon = "error.mutant_with_weapon";

        /// <summary>У врага-человека нет оружия в блоке атаки.</summary>
        public const string EnemyWithoutWeapon = "error.enemy_without_weapon";

        /// <summary>Скиллы заданы у актёра, который не мутант.</summary>
        public const string ActorSkillsNotMutant = "error.actor_skills_not_mutant";

        /// <summary>У мутанта нет скиллов.</summary>
        public const string ActorWithoutSkills = "error.actor_without_skills";

        /// <summary>У мутанта нет дефолтного скилла.</summary>
        public const string ActorWithoutDefaultSkill = "error.actor_without_default_skill";

        /// <summary>Дефолтный скилл не входит в список скиллов владельца.</summary>
        public const string DefaultSkillNotListed = "error.default_skill_not_listed";

        /// <summary>Дефолтный скилл задан у актёра, который не мутант.</summary>
        public const string DefaultSkillNotMutant = "error.default_skill_not_mutant";

        /// <summary>Скилл мутанта тратит патроны, которых у мутанта нет.</summary>
        public const string MutantSkillAmmo = "error.mutant_skill_ammo";

        /// <summary>Дефолтный скилл не является атакующим.</summary>
        public const string DefaultSkillNotAttack = "error.default_skill_not_attack";

        /// <summary>Ссылка на травму не найдена.</summary>
        public const string InjuryNotFound = "error.injury_not_found";

        /// <summary>Неизвестный вид длительности травмы.</summary>
        public const string UnknownInjuryDuration = "error.unknown_injury_duration";

        /// <summary>Шанс травмы вне диапазона (0, 1].</summary>
        public const string InjuryChanceRange = "error.injury_chance_range";

        /// <summary>Поправка к меткости травмы вне диапазона [-1, 1].</summary>
        public const string InjuryAccuracyRange = "error.injury_accuracy_range";

        /// <summary>У травмы нет блока длительности.</summary>
        public const string InjuryDurationMissing = "error.injury_duration_missing";

        /// <summary>Длительность травмы не соответствует виду: нужны шаги или часы.</summary>
        public const string InjuryDurationAmount = "error.injury_duration_amount";

        /// <summary>Травма указана в скилле дважды.</summary>
        public const string DuplicateInjuryId = "error.duplicate_injury_id";

        /// <summary>Травма без модификаторов: непонятно, что она делает.</summary>
        public const string InjuryWithoutEffect = "error.injury_without_effect";

        /// <summary>У расходника нет блока <c>consumable</c>.</summary>
        public const string ConsumableWithoutBlock = "error.consumable_without_block";

        /// <summary>Блок <c>consumable</c> стоит у предмета другой категории.</summary>
        public const string ConsumableBlockOnOther = "error.consumable_block_on_other";

        /// <summary>У расходника нет ни эффектов, ни травм, которые он лечит.</summary>
        public const string ConsumableNoEffects = "error.consumable_no_effects";

        /// <summary>Неизвестный тип эффекта расходника.</summary>
        public const string UnknownConsumableEffect = "error.unknown_consumable_effect";

        /// <summary>Неизвестная характеристика эффекта-модификатора.</summary>
        public const string UnknownConsumableStat = "error.unknown_consumable_stat";

        /// <summary>Величина модификатора равна нулю: эффект ничего не делает.</summary>
        public const string ConsumableStatValue = "error.consumable_stat_value";

        /// <summary>У модификатора защиты не указан тип урона.</summary>
        public const string ConsumableProtectionType = "error.consumable_protection_type";

        /// <summary>Лечение или восстановление ОД с неположительным количеством.</summary>
        public const string ConsumableEffectAmount = "error.consumable_effect_amount";

        /// <summary>Медицина «только вне боя» разрешена в слоте быстрого доступа.</summary>
        public const string QuickSlotOutOfCombat = "error.quick_slot_out_of_combat";

        /// <summary>Аптечка врага ссылается на предмет, который не является боевым расходником.</summary>
        public const string ActorMedkitItem = "error.actor_medkit_item";

        /// <summary>У аптечки врага не задано число применений.</summary>
        public const string ActorMedkitCharges = "error.actor_medkit_charges";

        /// <summary>Аптечка задана актёру, который не противник.</summary>
        public const string ActorMedkitNotEnemy = "error.actor_medkit_not_enemy";

        /// <summary>Порог здоровья для аптечки ИИ вне диапазона [0, 1].</summary>
        public const string MedkitHpThresholdRange = "error.medkit_hp_threshold_range";
    }
}
