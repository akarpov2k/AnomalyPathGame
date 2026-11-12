#nullable enable
using AnomalyPath.Core.Data;
using AnomalyPath.Core.Json;
using AnomalyPath.Core.Localization;

namespace AnomalyPath.Core.Combat
{
    /// <summary>Настройки боя из единственной записи вида <c>combat</c>.</summary>
    public sealed class CombatSettingsTemplate
    {
        /// <summary>Создаёт настройки боя.</summary>
        /// <param name="damageFormulaId">Формула урона.</param>
        /// <param name="xpFormulaId">Формула опыта за победу.</param>
        /// <param name="clampMin">Нижняя граница результата по умолчанию.</param>
        /// <param name="clampMax">Верхняя граница результата по умолчанию.</param>
        /// <param name="apRestore">Режим восстановления ОД.</param>
        /// <param name="apPerTurn">Сколько ОД восстанавливается при частичном режиме.</param>
        /// <param name="reloadApCost">Цена перезарядки в ОД.</param>
        /// <param name="weaponSwapApCost">Цена смены оружия в бою, в ОД.</param>
        /// <param name="aiPlanDepth">Сколько действий вперёд смотрит ИИ противника.</param>
        /// <param name="medkitHpThreshold">Доля здоровья противника, ниже которой ИИ применяет аптечку.</param>
        public CombatSettingsTemplate(
            string damageFormulaId,
            string xpFormulaId,
            double? clampMin,
            double? clampMax,
            ApRestoreKind apRestore,
            int apPerTurn,
            int reloadApCost,
            int weaponSwapApCost = 1,
            int aiPlanDepth = 5,
            double medkitHpThreshold = 0.3)
        {
            DamageFormulaId = damageFormulaId;
            XpFormulaId = xpFormulaId;
            ClampMin = clampMin;
            ClampMax = clampMax;
            ApRestore = apRestore;
            ApPerTurn = apPerTurn;
            ReloadApCost = reloadApCost;
            WeaponSwapApCost = weaponSwapApCost;
            AiPlanDepth = aiPlanDepth;
            MedkitHpThreshold = medkitHpThreshold;
        }

        /// <summary>Доля здоровья противника, ниже которой ИИ применяет аптечку.</summary>
        public double MedkitHpThreshold { get; }

        /// <summary>Цена перезарядки в ОД.</summary>
        public int ReloadApCost { get; }

        /// <summary>Цена смены оружия в бою действием за ОД.</summary>
        public int WeaponSwapApCost { get; }

        /// <summary>Сколько действий вперёд смотрит ИИ противника.</summary>
        public int AiPlanDepth { get; }

        /// <summary>Формула урона.</summary>
        public string DamageFormulaId { get; }

        /// <summary>Формула опыта за победу.</summary>
        public string XpFormulaId { get; }

        /// <summary>Нижняя граница результата по умолчанию.</summary>
        public double? ClampMin { get; }

        /// <summary>Верхняя граница результата по умолчанию.</summary>
        public double? ClampMax { get; }

        /// <summary>Режим восстановления ОД.</summary>
        public ApRestoreKind ApRestore { get; }

        /// <summary>Сколько ОД восстанавливается при частичном режиме.</summary>
        public int ApPerTurn { get; }

        /// <summary>Читает настройки боя из записи.</summary>
        /// <param name="value">Загруженная запись.</param>
        /// <param name="sourcePath">Путь для сообщения об ошибке.</param>
        /// <returns>Настройки.</returns>
        /// <exception cref="DataException">Поля заданы неверно.</exception>
        public static CombatSettingsTemplate FromJson(JsonValue value, string sourcePath)
        {
            var damageFormulaId = DataMap.RequireString(value, "damageFormulaId", sourcePath);
            var xpFormulaId = DataMap.RequireString(value, "xpFormulaId", sourcePath);

            var apRestoreCode = DataMap.RequireString(value, "apRestore", sourcePath);
            if (!SchemaEnum.TryParse(apRestoreCode, out ApRestoreKind apRestore))
            {
                throw new DataException(sourcePath, CoreErrors.UnknownApRestore, apRestoreCode);
            }

            return new CombatSettingsTemplate(
                damageFormulaId,
                xpFormulaId,
                DataMap.OptionalDouble(value, "clampMin"),
                DataMap.OptionalDouble(value, "clampMax"),
                apRestore,
                DataMap.OptionalInt(value, "apPerTurn", 0),
                DataMap.RequireInt(value, "reloadApCost", sourcePath),
                ReadWeaponSwapApCost(value, sourcePath),
                ReadAiPlanDepth(value, sourcePath),
                ReadMedkitHpThreshold(value, sourcePath));
        }

        private static int ReadWeaponSwapApCost(JsonValue value, string sourcePath)
        {
            var cost = DataMap.OptionalInt(value, "weaponSwapApCost", 1);
            if (cost < 0)
            {
                throw new DataException(sourcePath, CoreErrors.NonNegative, "weaponSwapApCost");
            }

            return cost;
        }

        private static int ReadAiPlanDepth(JsonValue value, string sourcePath)
        {
            var depth = DataMap.OptionalInt(value, "aiPlanDepth", 5);
            if (depth < 1)
            {
                throw new DataException(sourcePath, CoreErrors.MinimumOne, "aiPlanDepth");
            }

            return depth;
        }

        private static double ReadMedkitHpThreshold(JsonValue value, string sourcePath)
        {
            var threshold = DataMap.OptionalDouble(value, "medkitHpThreshold") ?? 0.3;
            if (double.IsNaN(threshold) || threshold < 0 || threshold > 1)
            {
                throw new DataException(sourcePath, CoreErrors.MedkitHpThresholdRange, threshold);
            }

            return threshold;
        }
    }
}
