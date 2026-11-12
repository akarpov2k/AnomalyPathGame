#nullable enable
namespace AnomalyPath.Runtime.Screens.Game.Hub
{
    /// <summary>
    /// Часть класса <see cref="HubScreen"/>: действия торговца, меню, инвентаря и выхода.
    /// </summary>
    public sealed partial class HubScreen
    {
        private void OpenMenu() => Host.ShowGameMenu();

        private void OpenInventory() => Host.ShowInventory();

        /// <summary>Дверь комнаты торговца ведёт в большой хаб, а не на маршрут.</summary>
        private void LeaveHub()
        {
            _dialog?.Hide();
            Host.ShowGame(false);
        }

        /// <summary>Клик по фигуре торговца открывает меню диалога.</summary>
        private void OpenMerchant()
        {
            if (_traderId != null)
            {
                _dialog?.ShowMenu(_traderId);
            }
        }
    }
}
