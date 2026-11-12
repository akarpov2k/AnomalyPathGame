#nullable enable
using System.Collections.Generic;
using AnomalyPath.Runtime.Screens.Menu;
using AnomalyPath.Runtime.Ui.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace AnomalyPath.Tests.PlayMode
{
    /// <summary>Часть класса <see cref="MainMenuPlayModeTests"/>: служебные помощники.</summary>
    public sealed partial class MainMenuPlayModeTests
    {
        private static List<SaveSlotRow> ActiveRows(string panelName)
        {
            var rows = new List<SaveSlotRow>();
            var panel = GameObject.Find(panelName);
            if (panel == null)
            {
                return rows;
            }

            foreach (var row in panel.GetComponentsInChildren<SaveSlotRow>(true))
            {
                if (row.gameObject.activeSelf)
                {
                    rows.Add(row);
                }
            }

            return rows;
        }

        private static int CountSaveTiles() => ActiveRows("SaveGame").Count;

        private static SaveSlotRow? FindSaveRow(string slot) => RowBySlot("SaveGame", slot);

        private static SaveSlotRow? FindLoadRow(string slot) => RowBySlot("LoadGame", slot);

        private static SaveSlotRow? RowBySlot(string panelName, string slot)
        {
            foreach (var row in ActiveRows(panelName))
            {
                if (row.Slot == slot)
                {
                    return row;
                }
            }

            return null;
        }

        private static float ViewportHeightOf(string panelName)
        {
            var panel = GameObject.Find(panelName);
            if (panel == null)
            {
                return 0f;
            }

            foreach (var area in panel.GetComponentsInChildren<ScrollArea>(true))
            {
                if (area.gameObject.activeInHierarchy)
                {
                    return area.ViewportHeight;
                }
            }

            return 0f;
        }

        private static SaveSlotRow? FindNewSlotRow()
        {
            foreach (var row in ActiveRows("SaveGame"))
            {
                if (row.NameField != null && row.NameField.gameObject.activeSelf)
                {
                    return row;
                }
            }

            return null;
        }

        private static string TitleOf(SaveSlotRow row) => ChildText(row, "Title");

        private static string DetailsOf(SaveSlotRow row) => ChildText(row, "Details");

        private static string DayOf(SaveSlotRow row) => ChildText(row, "Day");

        private static string ChildText(SaveSlotRow row, string name)
        {
            var child = row.transform.Find(name);
            if (child == null)
            {
                return string.Empty;
            }

            var label = child.GetComponent<Text>();
            return label != null ? label.text : string.Empty;
        }

        private static void ClickButton(Button? button)
        {
            Assert.IsNotNull(button, "Кнопка не найдена");
            button!.onClick.Invoke();
        }

        private static void ClickIn(string panelName, string objectName)
        {
            var panel = GameObject.Find(panelName);
            Assert.IsNotNull(panel, $"Панель «{panelName}» не найдена");
            foreach (var button in panel!.GetComponentsInChildren<Button>(true))
            {
                if (button.gameObject.activeInHierarchy && button.gameObject.name == objectName)
                {
                    button.onClick.Invoke();
                    return;
                }
            }

            Assert.Fail($"На панели «{panelName}» нет активной кнопки «{objectName}»");
        }

        private static string ConfirmMessage(GameObject panel)
        {
            var longest = string.Empty;
            foreach (var label in panel.GetComponentsInChildren<Text>(true))
            {
                if (label.gameObject.activeInHierarchy && label.text.Length > longest.Length)
                {
                    longest = label.text;
                }
            }

            return longest;
        }
    }
}
