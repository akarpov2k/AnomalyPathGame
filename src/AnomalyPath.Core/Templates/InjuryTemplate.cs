#nullable enable
using AnomalyPath.Core.Combat;
using AnomalyPath.Core.Data;
using AnomalyPath.Core.Json;
using AnomalyPath.Core.Localization;

namespace AnomalyPath.Core.Templates
{
    /// <summary>
    /// Травма как сущность данных: шанс срабатывания, модификаторы бойца и вид длительности.
    /// Скилл ссылается на травмы по id, а при попадании бой выбирает одну из них.
    /// </summary>
    public sealed class InjuryTemplate
    {
        /// <summary>Создаёт травму.</summary>
        /// <param name="id">Id вида <c>injury_broken_leg</c>.</param>
        /// <param name="nameKey">Ключ названия в каталоге строк.</param>
        /// <param name="icon">Id иконки в каталоге спрайтов.</param>
        /// <param name="chance">Шанс срабатывания при попадании, больше нуля и не больше единицы.</param>
        /// <param name="modifiers">Модификаторы, которые травма даёт бойцу.</param>
        /// <param name="durationKind">Вид длительности.</param>
        /// <param name="durationAmount">Длительность в часах; для «до лечения» — 0.</param>
        /// <exception cref="System.ArgumentNullException">Обязательное значение не передано.</exception>
        public InjuryTemplate(
            string id,
            string nameKey,
            string icon,
            double chance,
            CombatModifiers modifiers,
            InjuryDurationKind durationKind,
            int durationAmount)
        {
            Id = id ?? throw new System.ArgumentNullException(nameof(id));
            NameKey = nameKey ?? throw new System.ArgumentNullException(nameof(nameKey));
            Icon = icon ?? throw new System.ArgumentNullException(nameof(icon));
            Chance = chance;
            Modifiers = modifiers ?? throw new System.ArgumentNullException(nameof(modifiers));
            DurationKind = durationKind;
            DurationAmount = durationAmount;
        }

        /// <summary>Id травмы в данных.</summary>
        public string Id { get; }

        /// <summary>Ключ названия в каталоге строк.</summary>
        public string NameKey { get; }

        /// <summary>Id иконки травмы в каталоге спрайтов.</summary>
        public string Icon { get; }

        /// <summary>Шанс срабатывания при попадании.</summary>
        public double Chance { get; }

        /// <summary>Модификаторы бойца, пока травма активна.</summary>
        public CombatModifiers Modifiers { get; }

        /// <summary>Вид длительности: часы мира или до лечения.</summary>
        public InjuryDurationKind DurationKind { get; }

        /// <summary>Длительность в часах; для «до лечения» — 0.</summary>
        public int DurationAmount { get; }

        /// <summary><c>true</c>, если травма истекает сама (часы).</summary>
        public bool IsTimed => InjuryDurationCodec.IsTimed(DurationKind);

        /// <summary>Читает травму из записи вида <c>injuries</c>.</summary>
        /// <param name="value">Загруженная запись.</param>
        /// <param name="sourcePath">Путь для сообщения об ошибке.</param>
        /// <returns>Травма.</returns>
        /// <exception cref="DataException">Поля заданы неверно.</exception>
        public static InjuryTemplate FromJson(JsonValue value, string sourcePath)
        {
            var id = DataMap.RequireString(value, "id", sourcePath);
            var nameKey = DataMap.RequireString(value, "nameKey", sourcePath);
            var icon = DataMap.RequireString(value, "icon", sourcePath);
            var chance = DataMap.RequireDouble(value, "chance", sourcePath);
            if (chance <= 0 || chance > 1)
            {
                throw new DataException(sourcePath, CoreErrors.InjuryChanceRange, chance);
            }

            var accuracyBonus = DataMap.OptionalDouble(value, "accuracyBonus") ?? 0;
            if (accuracyBonus < -1 || accuracyBonus > 1)
            {
                throw new DataException(sourcePath, CoreErrors.InjuryAccuracyRange, accuracyBonus);
            }

            var actionApCostBonus = DataMap.OptionalInt(value, "actionApCostBonus", 0);
            if (!value.TryGet("duration", out var duration) || !duration.IsObject)
            {
                throw new DataException(sourcePath, CoreErrors.InjuryDurationMissing, id);
            }

            var kindCode = DataMap.RequireString(duration, "kind", sourcePath);
            if (!InjuryDurationCodec.TryParse(kindCode, out var durationKind))
            {
                throw new DataException(sourcePath, CoreErrors.UnknownInjuryDuration, kindCode);
            }

            var durationAmount = DataMap.OptionalInt(duration, "amount", 0);
            if (durationKind == InjuryDurationKind.UntilHealed)
            {
                if (durationAmount != 0)
                {
                    throw new DataException(sourcePath, CoreErrors.InjuryDurationAmount, id);
                }
            }
            else
            {
                if (durationAmount < 1)
                {
                    throw new DataException(sourcePath, CoreErrors.InjuryDurationAmount, id);
                }
            }

            return new InjuryTemplate(
                id,
                nameKey,
                icon,
                chance,
                new CombatModifiers(accuracyBonus, actionApCostBonus),
                durationKind,
                durationAmount);
        }
    }
}
