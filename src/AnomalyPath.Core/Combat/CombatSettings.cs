#nullable enable
using AnomalyPath.Core.Abstractions;

namespace AnomalyPath.Core.Combat
{
    /// <summary>
    /// Настройки боя из данных, которые нужны интерфейсу: цена перезарядки и смены оружия.
    /// Числа читаются из <see cref="CombatSettingsTemplate"/> и в коде не дублируются.
    /// </summary>
    public sealed class CombatSettings : ICombatSettings
    {
        private readonly CombatSettingsTemplate _template;

        /// <summary>Создаёт настройки поверх шаблона из данных.</summary>
        /// <param name="template">Шаблон <c>combat.json</c>.</param>
        public CombatSettings(CombatSettingsTemplate template)
        {
            _template = template;
        }

        /// <inheritdoc />
        public int ReloadApCost => _template.ReloadApCost;

        /// <inheritdoc />
        public int WeaponSwapApCost => _template.WeaponSwapApCost;
    }
}
