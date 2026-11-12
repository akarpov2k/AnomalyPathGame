#nullable enable
namespace AnomalyPath.Runtime.Screens.Combat
{
    /// <summary>
    /// Часть класса <see cref="CombatScreen"/>: полупрозрачный журнал действий слева от кнопки.
    /// Журнал закрывается кликом в любом месте и клавишей Esc.
    /// </summary>
    public sealed partial class CombatScreen
    {
        /// <summary>Открыт ли журнал действий.</summary>
        private bool IsJournalOpen => journalPanel != null && journalPanel.activeSelf;

        /// <summary>Открывает журнал действий; повторное нажатие закрывает его.</summary>
        private void OpenJournal()
        {
            if (journalPanel == null)
            {
                return;
            }

            if (IsJournalOpen)
            {
                CloseJournal();
                return;
            }

            CloseMenus();
            RefreshJournal();
            journalPanel.SetActive(true);
            if (journalCatcher != null)
            {
                journalCatcher.SetActive(true);
            }
        }

        /// <summary>Закрывает журнал действий.</summary>
        private void CloseJournal()
        {
            if (journalPanel != null)
            {
                journalPanel.SetActive(false);
            }

            if (journalCatcher != null)
            {
                journalCatcher.SetActive(false);
            }
        }

        /// <summary>Переносит строки журнала боя в окно журнала.</summary>
        private void RefreshJournal()
        {
            if (journalText != null)
            {
                journalText.text = string.Join("\n", _lines);
            }
        }
    }
}
