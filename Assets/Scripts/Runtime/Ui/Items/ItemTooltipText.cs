#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using AnomalyPath.Core.Artifacts;
using AnomalyPath.Core.Inventory;
using AnomalyPath.Core.Templates;
using AnomalyPath.Runtime.Ui.Core;

namespace AnomalyPath.Runtime.Ui.Items
{
    /// <summary>
    /// Текст подсказки предмета: название, описание из каталога строк и характеристики
    /// из <see cref="ItemFacts"/>. Числа собираются здесь, чтобы подсказка не зависела от вёрстки.
    /// </summary>
    public static class ItemTooltipText
    {
        /// <summary>Название предмета с количеством, если это стопка.</summary>
        /// <param name="host">Хост интерфейса.</param>
        /// <param name="item">Экземпляр предмета.</param>
        /// <param name="template">Шаблон предмета.</param>
        /// <returns>Название для заголовка подсказки.</returns>
        public static string Title(IUiHost host, ItemInstance item, ItemTemplate template) =>
            Title(host, template, item.Stack);

        /// <summary>Название предмета по шаблону и количеству — для товара, у которого нет экземпляра.</summary>
        /// <param name="host">Хост интерфейса.</param>
        /// <param name="template">Шаблон предмета.</param>
        /// <param name="stack">Количество в стопке или остаток товара у торговца.</param>
        /// <returns>Название для заголовка подсказки.</returns>
        public static string Title(IUiHost host, ItemTemplate template, int stack)
        {
            var name = host.Texts.Get(template.NameKey);
            return stack > 1
                ? name + " ×" + stack.ToString(CultureInfo.InvariantCulture)
                : name;
        }

        /// <summary>Описание предмета из каталога строк; пусто, если описания в данных нет.</summary>
        /// <param name="host">Хост интерфейса.</param>
        /// <param name="template">Шаблон предмета.</param>
        /// <returns>Текст описания или пустая строка.</returns>
        public static string Description(IUiHost host, ItemTemplate template)
        {
            var key = template.DescriptionKey;
            if (string.IsNullOrWhiteSpace(key) || !host.Texts.Contains(key!))
            {
                return string.Empty;
            }

            return host.Texts.Get(key!);
        }

        /// <summary>Характеристики предмета строками, по одной на строку.</summary>
        /// <param name="host">Хост интерфейса.</param>
        /// <param name="item">Экземпляр предмета; <c>null</c> — только данные шаблона (товар торговца).</param>
        /// <param name="template">Шаблон предмета.</param>
        /// <returns>Строки характеристик; пусто, если показывать нечего.</returns>
        public static string Facts(IUiHost host, ItemInstance? item, ItemTemplate template)
        {
            ArtifactModifiers? modifiers = item != null && template.Category == ItemCategory.Artifact
                ? host.Artifacts.GetModifiers(item)
                : null;
            var weaponMultiplier = WeaponStats.Multiplier(item, template, host.Rarities);
            var facts = ItemFacts.Collect(template, item, host.DamageTypes.All, modifiers, weaponMultiplier);
            var lines = new List<string>(facts.Count);
            foreach (var fact in facts)
            {
                lines.Add(Line(host, fact));
            }

            return string.Join("\n", lines);
        }

        private static string Line(IUiHost host, ItemFact fact) => fact.Kind switch
        {
            ItemFactKind.Weight => UiText.Format(host.Texts, UiTextKey.Item_Weight, Number(fact.Value)),
            ItemFactKind.Damage => UiText.Format(host.Texts, UiTextKey.Item_Damage, Number(fact.Value)),
            ItemFactKind.Magazine => UiText.Format(host.Texts, UiTextKey.Item_Magazine, Whole(fact.Value)),
            ItemFactKind.Caliber => UiText.Format(host.Texts, UiTextKey.Item_Caliber, fact.Code ?? string.Empty),
            ItemFactKind.Penetration => UiText.Format(host.Texts, UiTextKey.Item_Penetration, Number(fact.Value)),
            ItemFactKind.Durability => UiText.Format(
                host.Texts,
                UiTextKey.Item_Durability,
                Whole(fact.Value),
                Whole(fact.Max ?? 0)),
            ItemFactKind.Protection => UiText.Format(
                host.Texts,
                UiTextKey.Item_Protection,
                DamageType(host, fact.Code),
                Whole(fact.Value)),
            ItemFactKind.ExtraWeight => UiText.Format(host.Texts, UiTextKey.Item_Extra_Weight, Number(fact.Value)),
            ItemFactKind.ArtifactSlots => UiText.Format(host.Texts, UiTextKey.Item_Artifact_Slots, Whole(fact.Value)),
            ItemFactKind.HealthModifier => UiText.Format(host.Texts, UiTextKey.Item_Health, Whole(fact.Value)),
            ItemFactKind.AccuracyModifier => UiText.Format(host.Texts, UiTextKey.Item_Accuracy, Signed(fact.Value)),
            ItemFactKind.StaminaModifier => UiText.Format(host.Texts, UiTextKey.Item_Stamina, Whole(fact.Value)),
            ItemFactKind.ProtectionModifier => UiText.Format(
                host.Texts,
                UiTextKey.Item_Protection,
                DamageType(host, fact.Code),
                Signed(fact.Value)),
            ItemFactKind.ActionApCostModifier => UiText.Format(host.Texts, UiTextKey.Item_Action_Ap, Signed(fact.Value)),
            ItemFactKind.Heal => UiText.Format(host.Texts, UiTextKey.Item_Heal, Whole(fact.Value)),
            ItemFactKind.ApRestore => UiText.Format(host.Texts, UiTextKey.Item_Ap_Restore, Whole(fact.Value)),
            ItemFactKind.Food => UiText.Format(host.Texts, UiTextKey.Item_Food, Whole(fact.Value)),
            ItemFactKind.Water => UiText.Format(host.Texts, UiTextKey.Item_Water, Whole(fact.Value)),
            ItemFactKind.Charges => UiText.Format(host.Texts, UiTextKey.Item_Charges, Whole(fact.Value)),
            ItemFactKind.HealsInjuries => UiText.Format(host.Texts, UiTextKey.Item_Heals_Injuries, Whole(fact.Value)),
            _ => throw new ArgumentOutOfRangeException(nameof(fact), fact.Kind, "unknown item fact")
        };

        private static string DamageType(IUiHost host, string? damageTypeId)
        {
            if (string.IsNullOrWhiteSpace(damageTypeId))
            {
                return string.Empty;
            }

            return host.DamageTypes.TryGet(damageTypeId!, out var damageType)
                ? host.Texts.Get(damageType.NameKey)
                : damageTypeId!;
        }

        private static string Number(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);

        private static string Whole(double value) =>
            ((long)Math.Round(value, MidpointRounding.AwayFromZero)).ToString(CultureInfo.InvariantCulture);

        private static string Signed(double value) =>
            (value > 0 ? "+" : string.Empty) + value.ToString("0.###", CultureInfo.InvariantCulture);
    }
}
