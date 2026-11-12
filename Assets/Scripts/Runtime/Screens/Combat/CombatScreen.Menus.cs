#nullable enable
using System;
using System.Collections.Generic;
using AnomalyPath.Core.Combat;
using AnomalyPath.Core.Equipment;
using AnomalyPath.Runtime.Ui.Core;
using UnityEngine;
using UnityEngine.UI;

namespace AnomalyPath.Runtime.Screens.Combat
{
    /// <summary>
    /// Часть класса <see cref="CombatScreen"/>: выпадашки «Переход» и «Скиллы»,
    /// их строки, подсказки при наведении и действия из строк.
    /// </summary>
    public sealed partial class CombatScreen
    {
        /// <summary>Открыта ли выпадашка действий.</summary>
        private bool IsMenuOpen =>
            (moveMenu != null && moveMenu.activeSelf) || (skillMenu != null && skillMenu.activeSelf);

        /// <summary>Открывает выпадашку скиллов: скиллы оружия, перезарядка и смена оружия.</summary>
        private void OpenSkillMenu()
        {
            var open = skillMenu != null && skillMenu.activeSelf;
            CloseMenus();
            if (!open && skillMenu != null)
            {
                skillMenu.SetActive(true);
                if (menuCatcher != null)
                {
                    menuCatcher.SetActive(true);
                }
            }
        }

        /// <summary>Закрывает обе выпадашки и плашку закрытия.</summary>
        private void CloseMenus()
        {
            if (moveMenu != null)
            {
                moveMenu.SetActive(false);
            }

            if (skillMenu != null)
            {
                skillMenu.SetActive(false);
            }

            if (menuCatcher != null)
            {
                menuCatcher.SetActive(false);
            }

            HideTooltip();
        }

        /// <summary>
        /// Перестраивает строки выпадашки по действиям текущего хода: скиллы оружия,
        /// перезарядка и смена оружия. В чужой ход строки пустые.
        /// </summary>
        /// <param name="combat">Состояние боя; <c>null</c> — боя нет.</param>
        private void RefreshSkills(CombatState? combat)
        {
            var session = Host.Session;
            if (!IsPlayerTurn(combat) || session == null)
            {
                ClearEntries(skillEntries, skillEntryTemplate);
                return;
            }

            var actions = Host.Sessions.GetCombatActions(session);
            BuildSkillEntries(actions, combat!);
        }

        /// <summary>Строки выпадашки скиллов и служебные пункты перезарядки и смены оружия.</summary>
        /// <param name="actions">Действия текущего хода.</param>
        /// <param name="combat">Состояние боя.</param>
        private void BuildSkillEntries(IReadOnlyList<CombatAction> actions, CombatState combat)
        {
            ClearEntries(skillEntries, skillEntryTemplate);
            foreach (var action in actions)
            {
                if (action.Kind != CombatActionKind.Skill || action.SkillId == null)
                {
                    continue;
                }

                var skillId = action.SkillId;
                var label = SkillLabel(action);
                var tooltip = TooltipText(
                    SkillName(skillId),
                    action.ApCost,
                    action.AmmoCost,
                    SkillDescription(skillId));
                var chance = HitChanceLine(combat, skillId);
                if (chance.Length > 0)
                {
                    tooltip += "\n" + chance;
                }

                var damage = DamageLine(combat, skillId);
                if (damage.Length > 0)
                {
                    tooltip += "\n" + damage;
                }

                AddEntry(
                    skillEntries,
                    skillEntryTemplate,
                    "CombatSkill_" + skillId,
                    label,
                    tooltip,
                    action.IsAvailable,
                    () => UseSkill(skillId));
            }

            var settings = Host.CombatSettings;
            var reloadName = UiText.Get(Host.Texts, UiTextKey.Combat_Reload);
            var reloadLabel = CostLabel(reloadName, settings.ReloadApCost);
            AddEntry(
                skillEntries,
                skillEntryTemplate,
                "CombatReload",
                reloadLabel,
                reloadLabel,
                PlayerWeapon(combat) != null,
                Reload);

            var swapName = UiText.Get(Host.Texts, UiTextKey.Combat_Swap);
            var swapLabel = CostLabel(swapName, settings.WeaponSwapApCost);
            AddEntry(
                skillEntries,
                skillEntryTemplate,
                "CombatSwap",
                swapLabel,
                swapLabel,
                combat.Player.Equipment?.Get(EquipmentSlot.Secondary) != null,
                Swap);
        }

        /// <summary>Удаляет строки выпадашки, оставляя неактивный образец.</summary>
        /// <param name="container">Контейнер строк.</param>
        /// <param name="template">Образец строки.</param>
        private void ClearEntries(Transform? container, Button? template)
        {
            if (container == null || template == null)
            {
                return;
            }

            for (var index = container.childCount - 1; index >= 0; index--)
            {
                var child = container.GetChild(index).gameObject;
                if (child != template.gameObject)
                {
                    Destroy(child);
                }
            }

            template.gameObject.SetActive(false);
        }

        /// <summary>Добавляет строку в выпадашку по образцу и вешает на неё действия.</summary>
        /// <param name="container">Контейнер строк.</param>
        /// <param name="template">Образец строки.</param>
        /// <param name="name">Имя объекта строки.</param>
        /// <param name="label">Видимый текст строки.</param>
        /// <param name="tooltip">Текст подсказки при наведении.</param>
        /// <param name="interactable">Доступна ли строка.</param>
        /// <param name="action">Что выполнить по нажатию.</param>
        /// <param name="onEnter">Что выполнить при наведении; <c>null</c> — ничего.</param>
        /// <param name="onExit">Что выполнить, когда курсор ушёл; <c>null</c> — ничего.</param>
        private void AddEntry(
            Transform? container,
            Button? template,
            string name,
            string label,
            string tooltip,
            bool interactable,
            Action action,
            Action? onEnter = null,
            Action? onExit = null)
        {
            if (container == null || template == null)
            {
                return;
            }

            var entry = Instantiate(template, container);
            entry.gameObject.name = name;
            entry.gameObject.SetActive(true);
            entry.interactable = interactable;
            var text = entry.GetComponentInChildren<Text>();
            if (text != null)
            {
                text.text = label;
            }

            entry.onClick.RemoveAllListeners();
            entry.onClick.AddListener(() => action());
            var hover = entry.GetComponent<HoverInfo>() ?? entry.gameObject.AddComponent<HoverInfo>();
            hover.Entered = () =>
            {
                ShowTooltip(tooltip);
                onEnter?.Invoke();
            };
            hover.Left = () =>
            {
                HideTooltip();
                onExit?.Invoke();
            };
        }

        /// <summary>Показывает подсказку при наведении на действие.</summary>
        /// <param name="text">Текст подсказки.</param>
        private void ShowTooltip(string text)
        {
            if (tooltip == null || tooltipText == null)
            {
                return;
            }

            tooltipText.text = text;
            tooltip.SetActive(true);
        }

        /// <summary>Прячет подсказку при наведении.</summary>
        private void HideTooltip()
        {
            if (tooltip != null)
            {
                tooltip.SetActive(false);
            }
        }

        /// <summary>Подпись с ценой действия: название и ОД.</summary>
        /// <param name="name">Название действия.</param>
        /// <param name="apCost">Цена в очках действия.</param>
        /// <returns>Готовая подпись.</returns>
        private string CostLabel(string name, int apCost) =>
            UiText.Format(Host.Texts, UiTextKey.Combat_Skill_Button_No_Ammo, name, apCost);

        /// <summary>Подсказка действия: название, цена, патроны и описание.</summary>
        /// <param name="name">Название.</param>
        /// <param name="apCost">Цена в ОД.</param>
        /// <param name="ammoCost">Цена в патронах.</param>
        /// <param name="description">Описание из данных; пусто — без него.</param>
        /// <returns>Текст подсказки.</returns>
        private string TooltipText(string name, int apCost, int ammoCost, string description)
        {
            var text = ammoCost > 0
                ? UiText.Format(Host.Texts, UiTextKey.Combat_Skill_Button, name, apCost, ammoCost)
                : UiText.Format(Host.Texts, UiTextKey.Combat_Skill_Button_No_Ammo, name, apCost);
            return string.IsNullOrEmpty(description) ? text : text + "\n" + description;
        }

        /// <summary>
        /// Строка шанса попадания для подсказки скилла. Шанс берётся из предпросмотра
        /// боевых правил — того же расчёта, что и настоящий выстрел.
        /// </summary>
        /// <param name="combat">Состояние боя.</param>
        /// <param name="skillId">Идентификатор скилла.</param>
        /// <returns>Готовая строка или пустая, если скилл недоступен.</returns>
        private string HitChanceLine(CombatState combat, string skillId)
        {
            var preview = Host.CombatEngine.PreviewSkill(combat, PlayerWeapon(combat), skillId);
            if (!preview.Available)
            {
                return string.Empty;
            }

            return UiText.Format(
                Host.Texts,
                UiTextKey.Combat_Hit_Chance,
                (int)Math.Round(preview.HitChance * 100));
        }

        /// <summary>Строка ожидаемого урона скилла для подсказки; у статусов без урона пустая.</summary>
        /// <param name="combat">Состояние боя.</param>
        /// <param name="skillId">Идентификатор скилла.</param>
        /// <returns>Готовая строка или пустая.</returns>
        private string DamageLine(CombatState combat, string skillId)
        {
            var preview = Host.CombatEngine.PreviewSkill(combat, PlayerWeapon(combat), skillId);
            if (!preview.Available || preview.ExpectedDamage <= 0)
            {
                return string.Empty;
            }

            return UiText.Format(
                Host.Texts,
                UiTextKey.Combat_Damage_Expected,
                (int)Math.Round(preview.ExpectedDamage));
        }

        /// <summary>Подпись строки скилла: название, цена и причина недоступности.</summary>
        /// <param name="action">Действие-скилл.</param>
        /// <returns>Готовый текст строки.</returns>
        private string SkillLabel(CombatAction action)
        {
            var name = SkillName(action.SkillId!);
            var text = action.AmmoCost > 0
                ? UiText.Format(Host.Texts, UiTextKey.Combat_Skill_Button, name, action.ApCost, action.AmmoCost)
                : UiText.Format(Host.Texts, UiTextKey.Combat_Skill_Button_No_Ammo, name, action.ApCost);
            var reason = AvailabilityText(action.Availability);
            return reason.Length == 0
                ? text
                : UiText.Format(Host.Texts, UiTextKey.Combat_Skill_Reason, text, reason);
        }

        /// <summary>Название скилла из каталога текстов; без шаблона остаётся id.</summary>
        /// <param name="skillId">Идентификатор скилла.</param>
        /// <returns>Видимое название.</returns>
        private string SkillName(string skillId) =>
            Host.Skills.TryGet(skillId, out var template) && template != null
                ? Host.Texts.Get(template.NameKey)
                : skillId;

        /// <summary>Описание скилла из каталога текстов; пусто — описания нет.</summary>
        /// <param name="skillId">Идентификатор скилла.</param>
        /// <returns>Видимое описание или пустая строка.</returns>
        private string SkillDescription(string skillId) =>
            Host.Skills.TryGet(skillId, out var template) && template != null &&
            !string.IsNullOrEmpty(template.DescriptionKey)
                ? Host.Texts.Get(template.DescriptionKey)
                : string.Empty;

        /// <summary>Причина недоступности скилла для подписи строки.</summary>
        /// <param name="availability">Причина из боевого расчёта.</param>
        /// <returns>Видимый текст; пустая строка, если действие доступно.</returns>
        private string AvailabilityText(CombatActionAvailability availability) => availability switch
        {
            CombatActionAvailability.NoAp => UiText.Get(Host.Texts, UiTextKey.Combat_Skill_No_Ap),
            CombatActionAvailability.NoAmmo => UiText.Get(Host.Texts, UiTextKey.Combat_Skill_No_Ammo),
            CombatActionAvailability.BrokenWeapon => UiText.Get(Host.Texts, UiTextKey.Combat_Skill_Broken_Weapon),
            CombatActionAvailability.NoWeapon => UiText.Get(Host.Texts, UiTextKey.Combat_No_Weapon),
            CombatActionAvailability.MissingSkill => UiText.Get(Host.Texts, UiTextKey.Combat_Skill_Missing),
            CombatActionAvailability.Finished => UiText.Get(Host.Texts, UiTextKey.Combat_Skill_Finished),
            _ => string.Empty
        };

        /// <summary>Применяет скилл и пишет в журнал отдельную строку на каждый выстрел.</summary>
        /// <param name="skillId">Идентификатор скилла.</param>
        private void UseSkill(string skillId)
        {
            CloseMenus();
            HideTooltip();
            var session = Host.Session;
            if (session == null)
            {
                return;
            }

            var name = SkillName(skillId);
            if (!Host.Sessions.TryUseCombatSkill(session, skillId, out var result))
            {
                AddLine(UiText.Format(Host.Texts, UiTextKey.Combat_Skill_Unavailable, name));
                Refresh();
                return;
            }

            foreach (var hit in result.Hits)
            {
                AddLine(hit.Hit
                    ? UiText.Format(Host.Texts, UiTextKey.Combat_Log_Skill_Hit, name, hit.Damage)
                    : UiText.Format(Host.Texts, UiTextKey.Combat_Log_Skill_Miss, name));
            }

            if (result.Hits.Count == 0)
            {
                AddLine(UiText.Format(Host.Texts, UiTextKey.Combat_Log_Skill_Used, name));
            }

            Refresh();
            if (result.Hits.Count > 0 && playerFigure != null)
            {
                var hits = result.Hits;
                var flags = new bool[hits.Count];
                for (var i = 0; i < hits.Count; i++)
                {
                    flags[i] = hits[i].Hit;
                }

                // Числа показываются после каждого выстрела очереди, а не все разом.
                playerFigure.FireVolley(
                    enemyFigure,
                    flags,
                    index => ShowFloating(enemyFigure, hits[index].Hit, hits[index].Damage));
            }
        }

        /// <summary>Перезаряжает активное оружие за ОД.</summary>
        private void Reload()
        {
            CloseMenus();
            HideTooltip();
            var session = Host.Session;
            var weapon = PlayerWeapon(session?.Combat);
            if (session == null || weapon == null)
            {
                return;
            }

            if (Host.Sessions.TryReload(session, weapon))
            {
                AddLine(UiText.Get(Host.Texts, UiTextKey.Combat_Log_Reload));
            }

            Refresh();
        }

        /// <summary>Меняет основное оружие на запасное за ОД.</summary>
        private void Swap()
        {
            CloseMenus();
            HideTooltip();
            var session = Host.Session;
            if (session == null)
            {
                return;
            }

            if (Host.Sessions.TrySwapWeapon(session))
            {
                AddLine(UiText.Get(Host.Texts, UiTextKey.Combat_Log_Swap));
            }

            Refresh();
        }
    }
}
