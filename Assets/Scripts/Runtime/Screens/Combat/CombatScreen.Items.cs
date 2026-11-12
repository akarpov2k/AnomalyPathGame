#nullable enable
using AnomalyPath.Core.Combat;
using AnomalyPath.Core.Templates;
using AnomalyPath.Runtime.Ui.Core;
using UnityEngine.UI;

namespace AnomalyPath.Runtime.Screens.Combat
{
    /// <summary>
    /// Часть класса <see cref="CombatScreen"/>: быстрые слоты с назначенными предметами.
    /// Слоты строятся из неактивного образца префаба по числу слотов экипировки.
    /// </summary>
    public sealed partial class CombatScreen
    {
        /// <summary>
        /// Показывает быстрые слоты: медицину можно применить в свой ход за ОД,
        /// оружие в слоте меняет активное оружие тем же действием.
        /// </summary>
        /// <param name="combat">Состояние боя; <c>null</c> — боя нет.</param>
        private void RefreshQuickSlots(CombatState? combat)
        {
            if (quickSlots == null || quickSlotTemplate == null)
            {
                return;
            }

            var equipment = combat?.Player.Equipment;
            var count = equipment?.QuickSlotCount ?? 0;
            if (_quickSlots.Count != count)
            {
                RebuildQuickSlots(count);
            }

            var playerTurn = IsPlayerTurn(combat);
            for (var index = 0; index < _quickSlots.Count; index++)
            {
                var button = _quickSlots[index];
                var item = equipment?.QuickSlots[index];
                var template = item == null ? null : Host.Items.Get(item.ItemId);
                SetSlotArt(button, template);
                var label = button.transform.Find("Label");
                var text = label == null ? null : label.GetComponent<Text>();
                if (text != null)
                {
                    text.text = item == null
                        ? UiText.Get(Host.Texts, UiTextKey.Combat_Quick_Empty)
                        : Host.Texts.Get(template!.NameKey);
                }

                button.interactable = item != null && playerTurn &&
                                      (template?.Weapon != null ||
                                       (template?.Consumable != null && Host.Sessions.CanUseMedicine(Host.Session!, item)));
                var tooltip = item == null ? string.Empty : SlotTooltip(template);
                var hover = button.GetComponent<HoverInfo>() ?? button.gameObject.AddComponent<HoverInfo>();
                hover.Entered = () =>
                {
                    if (tooltip.Length > 0)
                    {
                        ShowTooltip(tooltip);
                    }
                };
                hover.Left = HideTooltip;
            }
        }

        /// <summary>Создаёт быстрые слоты по образцу префаба.</summary>
        /// <param name="count">Сколько слотов в экипировке.</param>
        private void RebuildQuickSlots(int count)
        {
            foreach (var button in _quickSlots)
            {
                if (button != null)
                {
                    Destroy(button.gameObject);
                }
            }

            _quickSlots.Clear();
            if (quickSlots == null || quickSlotTemplate == null)
            {
                return;
            }

            quickSlotTemplate.gameObject.SetActive(false);
            for (var index = 0; index < count; index++)
            {
                var slotIndex = index;
                var button = Instantiate(quickSlotTemplate, quickSlots);
                button.gameObject.name = "CombatQuick" + index;
                button.gameObject.SetActive(true);
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => UseQuickSlot(slotIndex));
                _quickSlots.Add(button);
            }
        }

        /// <summary>Ставит в слот иконку предмета; без предмета иконка скрыта.</summary>
        /// <param name="button">Кнопка слота.</param>
        /// <param name="template">Шаблон предмета; <c>null</c> — слот пуст.</param>
        private void SetSlotArt(Button button, ItemTemplate? template)
        {
            var iconTransform = button.transform.Find("Icon");
            var icon = iconTransform == null ? null : iconTransform.GetComponent<Image>();
            if (icon == null)
            {
                return;
            }

            var found = template != null &&
                        Host.Sprites.TryGet(template.Icon, out var sprite) &&
                        sprite != null;
            icon.enabled = found;
            if (found && template != null && Host.Sprites.TryGet(template.Icon, out var resolved) &&
                resolved != null)
            {
                icon.sprite = resolved;
            }
        }

        /// <summary>Подсказка предмета в быстром слоте: название, цена и описание.</summary>
        /// <param name="template">Шаблон предмета.</param>
        /// <returns>Текст подсказки.</returns>
        private string SlotTooltip(ItemTemplate template)
        {
            var lines = new System.Collections.Generic.List<string>
            {
                Host.Texts.Get(template.NameKey)
            };
            if (template.Consumable != null)
            {
                lines.Add(UiText.Format(
                    Host.Texts,
                    UiTextKey.Combat_Skill_Button_No_Ammo,
                    Host.Texts.Get(template.NameKey),
                    template.Consumable.UseTime));
            }

            if (!string.IsNullOrEmpty(template.DescriptionKey))
            {
                lines.Add(Host.Texts.Get(template.DescriptionKey!));
            }

            return string.Join("\n", lines);
        }

        /// <summary>
        /// Нажатие быстрого слота: медицина применяется за ОД, оружие меняет активное оружие.
        /// </summary>
        /// <param name="index">Номер слота.</param>
        private void UseQuickSlot(int index)
        {
            CloseMenus();
            HideTooltip();
            var session = Host.Session;
            var combat = session?.Combat;
            var equipment = combat?.Player.Equipment;
            if (session == null || combat == null || equipment == null || index >= equipment.QuickSlotCount)
            {
                return;
            }

            var item = equipment.QuickSlots[index];
            if (item == null)
            {
                return;
            }

            var template = Host.Items.Get(item.ItemId);
            if (template.Consumable != null)
            {
                if (Host.Sessions.TryUseMedicine(session, item, out var result))
                {
                    AddLine(UiText.Format(Host.Texts, UiTextKey.Combat_Log_Medicine, result.HealthRestored));
                }

                Refresh();
                return;
            }

            if (template.Weapon != null)
            {
                if (Host.Sessions.TryUseQuickWeapon(session, index))
                {
                    AddLine(UiText.Get(Host.Texts, UiTextKey.Combat_Log_Swap));
                }

                Refresh();
            }
        }
    }
}
